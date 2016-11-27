using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Forms;

namespace RandomGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // hold left side's ListBox data
        private List<int> _srcList;
        // hold right side's ListBox data
        private List<int> _destList;

        private IntervalWorker _thread1;
        private IntervalWorker _thread2;

        // hold dragging data when clicking on ListBoxA
        private string _selectedListBoxValue;

        private static readonly object _lock = new object();

        private void Form1_Load(object sender, EventArgs e)
        {
            _srcList = new List<int>();
            _destList = new List<int>();
            button1.Enabled = false;

            ListBoxB.AllowDrop = true;

            _thread1 = new IntervalWorker(1000);
            _thread2 = new IntervalWorker(1000);

            WireUpDragAndDrop();
            WireUpThreadFunc();

            // check if number can be added to listbox.
            Observable.FromEventPattern<EventArgs>(textBox1, "TextChanged")
               .Where(x => x != null)
               .Select(arg =>
               {
                   var text = (arg.Sender as TextBox).Text;
                   int value = 0;
                   return !string.IsNullOrWhiteSpace(text) &&
                          int.TryParse(text, out value) &&
                          value <= Int32.MaxValue &&
                          !_srcList.Contains(value);
               })
               .Throttle(TimeSpan.FromSeconds(0.2))
               .ObserveOn(SynchronizationContext.Current)
               .Subscribe(x => button1.Enabled = x);

            _thread1.Start();
            _thread2.Start();
        }

        /// <summary>
        /// Handle thread function
        /// </summary>
        private void WireUpThreadFunc()
        {
            /*
             Thread 1 - 
                generates 1-4 integers randomly(1 - 1000000, non-duplicate) every second 
                and adds them to  listbox A.
             */
            _thread1.DoJobEventHandler += () =>
            {
                var rnd = new Random();
                var results = Enumerable.Range(1, rnd.Next(4) + 1)
                    .Select(r => rnd.Next(1000000) + 1)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                lock (_lock)
                {
                    _srcList.AddRange(results);
                    _srcList.Sort();

                    // as required, update UI inside thread
                    UpdateListBoxA();
                }
            };

            /*
             Thread 2 - 
                removes 0-2 integers from listboxA every seconds.
             */
            // I do add extra check when user is dragging one of those value.
            _thread2.DoJobEventHandler += () =>
            {
                if (_srcList.Count == 0)
                    return;

                lock (_lock)
                {
                    var start = (_srcList.Count >= 3) ? 2 : (_srcList.Count - 1);
                    if (!string.IsNullOrWhiteSpace(_selectedListBoxValue))
                    {
                        for (var i = start; i >= 0; i--)
                        {
                            var value = Convert.ToInt32(_selectedListBoxValue);
                            if (_srcList[i] == value)
                                continue;

                            _srcList.RemoveAt(i);
                        }
                    }
                    else
                    {
                        _srcList.RemoveRange(0, start+1);
                    }

                    UpdateListBoxA();
                }
            };
        }

        private void UpdateListBoxA()
        {
            ListBoxA.SafeInvoke(ctl =>
            {
                ctl.Items.Clear();
                ctl.BeginUpdate();
                foreach (var item in _srcList)
                {
                    ctl.Items.Add(item);
                }
                ctl.EndUpdate();
            });
        }

        private void UpdateListBoxB()
        {
            ListBoxB.SafeInvoke(ctl =>
            {
                ctl.Items.Clear();
                ctl.BeginUpdate();
                foreach (var item in _destList)
                {
                    ctl.Items.Add(item);
                }
                ctl.EndUpdate();
            });
        }

        /// <summary>
        /// Handle drag and drop between two ListBox
        /// </summary>
        private void WireUpDragAndDrop()
        {
            var srcMouseDown = Observable.FromEventPattern<MouseEventArgs>(ListBoxA, "MouseDown");
            var srcQueryContinueDrag = Observable.FromEventPattern<QueryContinueDragEventArgs>(ListBoxA,
                "QueryContinueDrag");

            var destDragOver = Observable.FromEventPattern<DragEventArgs>(ListBoxB, "DragOver");
            var destDragDrop = Observable.FromEventPattern<DragEventArgs>(ListBoxB, "DragDrop");

            // when source ListBox mouse down, remember the item and start DoDragDrop()
            srcMouseDown.Subscribe(evt =>
            {
                if (ListBoxA.Items.Count == 0)
                    return;

                _selectedListBoxValue =
                    (ListBoxA.Items[ListBoxA.IndexFromPoint(evt.EventArgs.X, evt.EventArgs.Y)].ToString());
                (evt.Sender as ListBox).DoDragDrop(_selectedListBoxValue, DragDropEffects.Move);
            });

            // handle the dragging process
            // if mouse's left button released in middle of the dragging,
            // cancel drag action, and clear recorded value.
            srcQueryContinueDrag.Subscribe(val =>
            {
                var rc = new Rectangle(ListBoxB.Location, new Size(ListBoxB.Width, ListBoxB.Height));
                if ((Control.MouseButtons == MouseButtons.Left) ||
                    rc.Contains(PointToClient(Control.MousePosition)))
                    return;

                val.EventArgs.Action = DragAction.Cancel;
                _selectedListBoxValue = string.Empty;
            });

            // when destinate ListBox DragOver, set up DragDropEffects
            destDragOver.Subscribe(val =>
            {
                if (val.EventArgs.Data.GetDataPresent(DataFormats.Text))
                    val.EventArgs.Effect = DragDropEffects.Move;
                else
                    val.EventArgs.Effect = DragDropEffects.None;
            });

            // when destinate ListBox DragDrop, get drop data, 
            // and fill in destinate list then update
            destDragDrop.Subscribe(val =>
            {
                if (!val.EventArgs.Data.GetDataPresent(DataFormats.Text))
                    return;

                var str = val.EventArgs.Data.GetData(DataFormats.Text).ToString();
                int value = 0;
                if (int.TryParse(str, out value))
                {
                    _destList.Add(value);
                    _destList.Sort();
                }
                else
                {
                    Trace.WriteLine("Drop value is not a int!");
                    return;
                }

                lock (_lock)
                {
                    _srcList.Remove(value);
                }
                _selectedListBoxValue = string.Empty;

                UpdateListBoxB();
            });
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _thread1.Stop();
            _thread2.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_thread1.Status == IntervalWorker.StatusState.InProgress)
            {
                _thread1.Pause();
                button2.Text = "Resume producer";
            }
            else if (_thread1.Status == IntervalWorker.StatusState.Paused)
            {
                _thread1.Resume();
                button2.Text = "Pause producer";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_thread2.Status == IntervalWorker.StatusState.InProgress)
            {
                _thread2.Pause();
                button3.Text = "Resume consumer";
            }
            else if (_thread2.Status == IntervalWorker.StatusState.Paused)
            {
                _thread2.Resume();
                button3.Text = "Pause consumer";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _srcList.Add(Convert.ToInt32(textBox1.Text));
            }
            textBox1.Text = string.Empty;
        }
    }
}
