using System;
using System.Threading;

namespace RandomGenerator
{
    public class IntervalWorker
    {
        private EventWaitHandle _shutdownEvent = new ManualResetEvent(false);
        private EventWaitHandle _pauseEvent = new ManualResetEvent(true);

        private Thread _thread;
        private readonly int _sleepTime;
        private const int MIN_SLEEPTIME = 15;

        public enum StatusState
        {
            Unstarted,
            InProgress,
            Paused,
            Completed
        };

        public StatusState Status { get; private set; }

        public event Action DoJobEventHandler;

        public IntervalWorker(int sleepTime)
        {
            Status = StatusState.Unstarted;

            _sleepTime = sleepTime;
            if (_sleepTime < MIN_SLEEPTIME)
                _sleepTime = MIN_SLEEPTIME;
        }

        public void Job()
        {
            while (true)
            {
                _pauseEvent.WaitOne(Timeout.Infinite);

                if (DoJobEventHandler != null)
                    DoJobEventHandler();

                if (_shutdownEvent.WaitOne(0))
                    break;

                Thread.Sleep(_sleepTime);
            }
        }

        public void Start()
        {
            Status = StatusState.InProgress;

            _thread = new Thread(Job);
            _thread.Start();
        }

        public void Pause()
        {
            _pauseEvent.Reset();
            Status = StatusState.Paused;
        }

        public void Resume()
        {
            _pauseEvent.Set();
            Status = StatusState.InProgress;
        }

        public void Stop()
        {
            // Signal the shutdown event
            _shutdownEvent.Set();

            // Make sure to resume any paused threads
            _pauseEvent.Set();

            // Wait for the thread to exit
            //_thread.Join();
            Status = StatusState.Completed;
        }
    }
}