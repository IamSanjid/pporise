using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPOBot.Utils
{
    public class ThreadSafeRandom : Random
    {
        private object _syncRoot;
        public object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        protected override double Sample()
        {
            lock (SyncRoot)
            {
                return base.Sample();
            }
        }
        public override int Next()
        {
            lock (SyncRoot)
            {
                return base.Next();
            }
        }
        public override int Next(int maxVal)
        {
            lock (SyncRoot)
            {
                return base.Next(maxVal);
            }
        }
        public override int Next(int minVal, int maxVal)
        {
            lock (SyncRoot)
            {
                return base.Next(minVal, maxVal);
            }
        }
        public override void NextBytes(byte[] buffer)
        {
            lock (SyncRoot)
            {
                base.NextBytes(buffer);
            }
        }
        public override double NextDouble()
        {
            lock (SyncRoot)
            {
                return base.NextDouble();
            }
        }
    }

}
