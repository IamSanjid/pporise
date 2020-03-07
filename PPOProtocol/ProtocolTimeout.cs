using System;

namespace PPOProtocol
{
    public class ProtocolTimeout
    {
        public bool IsActive { get; private set; }
        public DateTime _expirationTime;

        public bool Update()
        {
            if (IsActive && DateTime.UtcNow >= _expirationTime)
            {
                IsActive = false;
            }
            return IsActive;
        }

        public void Set(int milliseconds = 1000)
        {
            IsActive = true;
            _expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);
        }

        public void Cancel()
        {
            IsActive = false;
        }
    }
}
