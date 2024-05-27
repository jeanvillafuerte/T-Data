using System;
using System.Data;
using System.Data.Common;

namespace Thomas.Database.Core.Provider
{
    internal sealed class CommandMetadata
    {
        public readonly Action<object, DbCommand> LoadParametersDelegate;
        public readonly Action<object, DbCommand, DbDataReader> LoadOutParametersDelegate;
        public readonly CommandBehavior CommandBehavior;
        public readonly CommandType CommandType;

        public CommandMetadata(in Action<object, DbCommand> loadParametersDelegate, in Action<object, DbCommand, DbDataReader> loadOutParametersDelegate, in CommandBehavior commandBehavior, in CommandType commandType)
        {
            CommandType = commandType;
            LoadParametersDelegate = loadParametersDelegate;
            LoadOutParametersDelegate = loadOutParametersDelegate;
            CommandBehavior = commandBehavior;
        }

        public CommandMetadata CloneNoCommandSequencial()
        {
            return new CommandMetadata(LoadParametersDelegate, LoadOutParametersDelegate, CommandBehavior &~ CommandBehavior.SequentialAccess, CommandType);
        }
    }
}
