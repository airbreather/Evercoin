using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NodaTime;

namespace Evercoin.Util
{
    public sealed class Waiter<T> : DisposableObject
    {
        private int cleanCalls;

        private Instant lastCleanup = Instant.FromDateTimeUtc(DateTime.UtcNow);

        private readonly ManualResetEventSlim completedWaiter = new ManualResetEventSlim(true);

        private readonly ConcurrentDictionary<T, ManualResetEventSlim> waiters;

        private readonly object cleanupSyncLock = new object();

        public Waiter()
            : this(EqualityComparer<T>.Default)
        {
        }

        public Waiter(IEqualityComparer<T> comparer)
        {
            this.waiters = new ConcurrentDictionary<T, ManualResetEventSlim>(comparer);
        }

        public void WaitFor(T value)
        {
            this.WaitFor(value, CancellationToken.None);
        }

        public void WaitFor(T value, CancellationToken token)
        {
            ManualResetEventSlim evt = this.waiters.GetOrAdd(value, _ => new ManualResetEventSlim());

            if (evt.IsSet)
            {
                return;
            }

            evt.Wait(token);
        }

        public void SetEventFor(T value)
        {
            ManualResetEventSlim evt = this.waiters.GetOrAdd(value, _ => new ManualResetEventSlim());
            evt.Set();
            this.Clean();
        }

        protected override void DisposeManagedResources()
        {
            this.completedWaiter.Dispose();
            foreach (ManualResetEventSlim evt in this.waiters.Values)
            {
                evt.Dispose();
            }

            base.DisposeManagedResources();
        }

        private void Clean()
        {
            // Do a full clean every 10,000 calls or every 2 seconds, whichever
            // comes sooner.
            const int CallsBetweenCleanup = 10000;
            Duration cleanUpInterval = Duration.FromSeconds(2);

            Instant now = Instant.FromDateTimeUtc(DateTime.UtcNow);

            if (Interlocked.Increment(ref this.cleanCalls) % CallsBetweenCleanup != 0 ||
                now - this.lastCleanup < cleanUpInterval)
            {
                return;
            }

            // Free up memory by replacing all references to signaled events
            // with references to just a single signaled event, which allows
            // the garbage collector to dump the redundant ones.
            Parallel.ForEach(this.waiters, kvp =>
            {
                T key = kvp.Key;
                ManualResetEventSlim waiter = kvp.Value;

                if (!waiter.IsSet ||
                    ReferenceEquals(this.completedWaiter, waiter))
                {
                    return;
                }

                using (waiter)
                {
                    this.waiters[key] = this.completedWaiter;
                }
            });

            lock (cleanupSyncLock)
            {
                this.cleanCalls = 0;
                this.lastCleanup = Instant.FromDateTimeUtc(DateTime.UtcNow);
            }
        }
    }
}
