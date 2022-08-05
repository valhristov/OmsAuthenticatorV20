namespace OmsAuthenticator.Framework
{
    public interface ISystemTime
    {
        DateTimeOffset UtcNow { get; }
    }

    public class SystemTime : ISystemTime
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
