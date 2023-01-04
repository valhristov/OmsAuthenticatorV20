using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Tests.Helpers
{
    public class SystemTimeMock : ISystemTime
    {
        private DateTimeOffset _currentTime;

        public DateTimeOffset UtcNow => _currentTime;

        public void Wait(TimeSpan time) =>
            _currentTime += time;
    }
}
