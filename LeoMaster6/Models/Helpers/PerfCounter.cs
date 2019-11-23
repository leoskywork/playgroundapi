using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace LeoMaster6.Models.Helpers
{
    public class PerfCounter
    {
        public const int MaxCount = 1000;
        public string Source { get; set; }
        public DateTime InitAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Life
        {
            get { return DateTime.UtcNow - InitAt; }
        }

        public DateTime? Checkpoint { get; set; }
        public List<DateTime> HistoryCheckpoints { get; set; } = new List<DateTime>();
        private bool _ending = false;


        public PerfCounter(string source)
        {
            this.Source = source;
        }

        public void Check(string stage = null)
        {
            var last = this.Checkpoint;
            this.Checkpoint = DateTime.UtcNow;
            var thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var msg = $"[{thread}] {Source}{(stage != null ? "_" + stage : "")}, since last: {(last.HasValue ? (Checkpoint.Value - last.Value).ToString() : "")}";

            System.Threading.ThreadPool.QueueUserWorkItem((_) =>
            {
                if (!this._ending)
                {
                    if (this.HistoryCheckpoints.Count > MaxCount)
                    {
                        this.HistoryCheckpoints.RemoveRange(0, 100);
                    }

                    if (Checkpoint.HasValue) this.HistoryCheckpoints.Add(Checkpoint.Value);
                }

                Debug.WriteLine("perf----->" + msg);
            });
        }

        public void End(string stage = "perf end", bool showHistory = false)
        {
            this.Check(stage);
            this._ending = true;

            if (showHistory)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    //TODO
                    Debug.WriteLine($"perf-----> history count {this.HistoryCheckpoints.Count}");
                    this.HistoryCheckpoints = null;
                });
            }
        }

        public static PerfCounter New(string source)
        {
            return new PerfCounter(source);
        }

        public static PerfCounter NewThenCheck(string source)
        {
            var perf = New(source);
            perf.Check("init");
            return perf;
        }
    }
}