// Developer: heaplyn
// Date: 2026-09-01
// Summary: Ring0 (Layer0) shared wait scheduler. A SINGLE background thread services a list
//          of pending waiters instead of every background loop owning its own timer/Task.Delay.
//          Deadlines are coalesced to a minimum-timeout granularity so many waiters that come
//          due around the same time wake the CPU ONCE, not N times. Fewer timer objects and
//          fewer thread wakeups => lower CPU and better power behavior (the CPU can stay in
//          low-power states longer between batched wakeups).
//
//          This is the wait primitive behind AdaptiveSleeper.DelayAsync; anything that already
//          calls AdaptiveSleeper.DelayAsync automatically rides this single scheduler thread.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class Ring0WaitScheduler
    {
        private sealed class Waiter
        {
            public long DueTicks;                       // Environment.TickCount64 deadline (ms)
            public TaskCompletionSource<bool> Tcs = null!;
            public CancellationTokenRegistration Reg;
        }

        /// <summary>
        /// Coalescing floor. The scheduler wakes at most about once per this many ms, and every
        /// requested delay is rounded UP to the next multiple of it, so near-simultaneous waiters
        /// share a single wakeup. Larger = fewer wakeups / less CPU, at the cost of coarser timing.
        /// </summary>
        public static int MinTimeoutMs { get; set; } = 250;

        private static readonly List<Waiter> _waiters = new();
        private static readonly object _gate = new();
        private static readonly AutoResetEvent _signal = new(false);
        private static int _started;

        // Diagnostics
        public static int PendingCount { get { lock (_gate) return _waiters.Count; } }

        /// <summary>Idempotent warm-up (safe to call at boot).</summary>
        public static void Start() => EnsureThread();

        /// <summary>
        /// Completes after ~delayMs, serviced by the shared scheduler thread. The deadline is
        /// coalesced to MinTimeoutMs. Honors cancellation.
        /// </summary>
        public static Task WaitAsync(int delayMs, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested) return Task.FromCanceled(ct);
            if (delayMs <= 0) return Task.CompletedTask;

            EnsureThread();

            int gran = Math.Max(1, MinTimeoutMs);
            long due = Environment.TickCount64 + delayMs;
            due = ((due + gran - 1) / gran) * gran;   // round UP to the next coalescing bucket

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var w = new Waiter { DueTicks = due, Tcs = tcs };

            if (ct.CanBeCanceled)
            {
                w.Reg = ct.Register(static state =>
                {
                    var waiter = (Waiter)state!;
                    lock (_gate) { _waiters.Remove(waiter); }
                    waiter.Tcs.TrySetCanceled();
                    _signal.Set();
                }, w);
            }

            lock (_gate) { _waiters.Add(w); }
            _signal.Set();   // wake the scheduler so it re-evaluates the nearest deadline
            return tcs.Task;
        }

        private static void EnsureThread()
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;
            var t = new Thread(Loop)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "Ring0-WaitScheduler"
            };
            t.Start();
        }

        private static void Loop()
        {
            var due = new List<Waiter>();
            while (true)
            {
                int waitMs;
                due.Clear();

                lock (_gate)
                {
                    long now = Environment.TickCount64;

                    // Collect everything that is due (coalesced: one pass fires the whole batch).
                    for (int i = _waiters.Count - 1; i >= 0; i--)
                    {
                        if (_waiters[i].DueTicks <= now)
                        {
                            due.Add(_waiters[i]);
                            _waiters.RemoveAt(i);
                        }
                    }

                    if (_waiters.Count == 0)
                    {
                        waitMs = Timeout.Infinite;   // nothing pending: sleep until a waiter arrives
                    }
                    else
                    {
                        long nearest = long.MaxValue;
                        for (int i = 0; i < _waiters.Count; i++)
                            if (_waiters[i].DueTicks < nearest) nearest = _waiters[i].DueTicks;

                        long delta = nearest - Environment.TickCount64;
                        // Never spin faster than the coalescing floor; clamp to int range.
                        waitMs = (int)Math.Clamp(delta, MinTimeoutMs, int.MaxValue);
                    }
                }

                // Fire OUTSIDE the lock so continuations never run while holding _gate.
                for (int i = 0; i < due.Count; i++)
                {
                    due[i].Reg.Dispose();
                    due[i].Tcs.TrySetResult(true);
                }

                _signal.WaitOne(waitMs);   // wakes on timeout OR when a waiter is added/cancelled
            }
        }
    }
}
