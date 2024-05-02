using System.Threading;

namespace Thomas.Database.Core
{
    internal static class InternalCounters
    {
        private static int _typeCounter;
        private static int _commandHandlerCounter;

        internal static int GetNextTypeCounter()
        {
            return Interlocked.Increment(ref _typeCounter);
        }

        internal static int GetNextCommandHandlerCounter()
        {
            return Interlocked.Increment(ref _commandHandlerCounter);
        }
    }
}
