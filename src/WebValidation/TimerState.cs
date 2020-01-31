using System;

namespace WebValidation
{
    /// <summary>
    /// Shared state for the Timer Tasks
    /// </summary>
    class TimerState
    {
        public int Index = 0;
        public int MaxIndex = 0;
        public long Count = 0;
        public Random Random = null;
        public object Lock = new object();
        public Test Test;
    }
}
