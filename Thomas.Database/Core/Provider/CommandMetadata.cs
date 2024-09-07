using System;
using System.Data;
using System.Data.Common;

namespace Thomas.Database.Core.Provider
{
    internal sealed class CommandMetaData
    {
        public readonly Func<object, string, string, DbCommand, DbCommand> LoadParametersDelegate;
        public readonly Action<object, DbCommand, DbDataReader> LoadOutParametersDelegate;
        public readonly CommandBehavior CommandBehavior;
        public readonly CommandType CommandType;
        public readonly string TransformedScript;

        public CommandMetaData(in Func<object, string, string, DbCommand, DbCommand> loadParametersDelegate, in Action<object, DbCommand, DbDataReader> loadOutParametersDelegate, in CommandBehavior commandBehavior, in CommandType commandType, in string script)
        {
            CommandType = commandType;
            LoadParametersDelegate = loadParametersDelegate;
            LoadOutParametersDelegate = loadOutParametersDelegate;
            CommandBehavior = commandBehavior;
            TransformedScript = script;
        }

        public CommandMetaData CloneNoCommandSequential()
        {
            return new CommandMetaData(LoadParametersDelegate, LoadOutParametersDelegate, CommandBehavior &~ CommandBehavior.SequentialAccess, CommandType, TransformedScript);
        }
    }
}
