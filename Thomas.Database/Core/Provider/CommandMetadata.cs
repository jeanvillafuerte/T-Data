using System;
using System.Data;
using System.Data.Common;

namespace Thomas.Database.Core.Provider
{
    internal readonly struct CommandMetadata
    {
        public readonly Action<object, DbCommand> ParserDelegate;
        public readonly bool HasOutputParameters;
        public readonly CommandBehavior CommandBehavior;
        public readonly CommandType CommandType;

        public CommandMetadata(in Action<object, DbCommand> parser, in bool hasOutputParameters, in CommandBehavior commandBehavior, in CommandType commandType)
        {
            CommandType = commandType;
            ParserDelegate = parser;
            HasOutputParameters = hasOutputParameters;
            CommandBehavior = commandBehavior;
        }
    }
}
