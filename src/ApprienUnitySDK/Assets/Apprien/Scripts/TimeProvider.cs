using System;

namespace Apprien
{
    public interface ITimeProvider
    {
        DateTime GetTimeNow();
    }

    public class TimeProvider : ITimeProvider
    {
        public DateTime GetTimeNow()
        {
            return DateTime.Now;
        }
    }
}
