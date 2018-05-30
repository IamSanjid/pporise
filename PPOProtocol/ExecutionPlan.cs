using System;

namespace PPOProtocol
{
    public class ExecutionPlan : IDisposable
    {
        private System.Timers.Timer _planTimer;
        private Action planAction;
        bool isRepeatedPlan;

        private ExecutionPlan(int millisecondsDelay, Action planAction, bool isRepeatedPlan)
        {
            _planTimer = new System.Timers.Timer(millisecondsDelay);
            _planTimer.Elapsed += GenericTimerCallback;
            _planTimer.Enabled = true;

            this.planAction = planAction;
            this.isRepeatedPlan = isRepeatedPlan;
        }

        public static ExecutionPlan Delay(int millisecondsDelay, Action planAction)
        {
            return new ExecutionPlan(millisecondsDelay, planAction, false);
        }

        public static ExecutionPlan Repeat(int millisecondsInterval, Action planAction)
        {
            return new ExecutionPlan(millisecondsInterval, planAction, true);
        }

        private void GenericTimerCallback(object sender, System.Timers.ElapsedEventArgs e)
        {
            planAction();
            if (!isRepeatedPlan)
            {
                Abort();
            }
        }

        public void Abort()
        {
            try
            {
                _planTimer.Enabled = false;
                _planTimer.Elapsed -= GenericTimerCallback;
            }
            catch (Exception)
            {
                //
            }
        }

        public void Dispose()
        {
            if (_planTimer != null)
            {
                Abort();
                _planTimer.Dispose();
                _planTimer = null;
            }
            else
            {
                throw new ObjectDisposedException(typeof(ExecutionPlan).Name);
            }
        }
    }
}
