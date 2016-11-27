using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YahooMovieScrape.Model;
using YahooMovieScrape.Service;

namespace YahooMovieScrape
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private const string URL_THIS_WEEK = "https://tw.movies.yahoo.com/movie_thisweek.html";
        private const string URL_IN_THEATERS = "https://tw.movies.yahoo.com/movie_intheaters.html";

        private void Form1_Load(object sender, EventArgs e)
        {
            IDisposable iCanBeDisposed = Observable.Interval(TimeSpan.FromSeconds(1))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(count =>
                {
                    ParseMovie(URL_THIS_WEEK/*, YahooMovieParser.Parse*/)
                        .Subscribe(movieInfos =>
                        {
                            listBox1.Items.Clear();
                            listBox1.Items.Add("本週新片");
                            listBox1.Items.Add("Count: " + count);

                            foreach (var item in movieInfos)
                            {
                                listBox1.Items.Add(item.ToString());
                            }
                        });

                    ParseMovie(URL_IN_THEATERS/*, YahooMovieParser.Parse*/)
                        .Subscribe(movieInfos =>
                        {
                            listBox2.Items.Clear();
                            listBox2.Items.Add("上映中");
                            listBox2.Items.Add("Count: " + count);

                            foreach (var item in movieInfos)
                            {
                                listBox2.Items.Add(item.ToString());
                            }
                        });
                });
        }

        private IObservable<List<MovieInfo>> ParseMovie(string url/*, Func<string, List<MovieInfo>> act*/)
        {
            var wc = new WebClient() { Encoding = Encoding.UTF8 };

            IObservable<List<MovieInfo>> observable = Observable
                .FromEventPattern<DownloadStringCompletedEventArgs>(wc, "DownloadStringCompleted")
                .Select(item =>
                {
                    var data = item.EventArgs.Result;
                    //return act(data);
                    return YahooMovieParser.Parse(data);
                });

            wc.DownloadStringAsync(new Uri(url));

            return observable;
        }
    }
}
