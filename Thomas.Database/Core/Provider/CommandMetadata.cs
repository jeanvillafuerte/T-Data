using System;
using System.Data;
using System.Data.Common;

namespace Thomas.Database.Core.Provider
{
    internal sealed class CommandMetaData
    {
        public readonly Func<object, string, string, DbCommand, DbCommand> LoadParametersDelegate;
        public readonly Func<object[], string, string, DbCommand, DbCommand> LoadParametersDelegate2;

        public readonly Action<object, DbCommand, DbDataReader> LoadOutParametersDelegate;
        public readonly CommandBehavior CommandBehavior;
        public readonly CommandType CommandType;

        //for postgresql store procedure calls
        public readonly string TransformedScript;

        public CommandMetaData(in Func<object, string, string, DbCommand, DbCommand> loadParametersDelegate, in Func<object[], string, string, DbCommand, DbCommand> loadParametersDelegate2, in Action<object, DbCommand, DbDataReader> loadOutParametersDelegate, in CommandBehavior commandBehavior, in CommandType commandType, in string script)
        {
            CommandType = commandType;
            LoadParametersDelegate = loadParametersDelegate;
            LoadParametersDelegate2 = loadParametersDelegate2;
            LoadOutParametersDelegate = loadOutParametersDelegate;
            CommandBehavior = commandBehavior;
            TransformedScript = script;
        }

        public CommandMetaData CloneNoCommandSequential()
        {
            return new CommandMetaData(in LoadParametersDelegate, in LoadParametersDelegate2, in LoadOutParametersDelegate, CommandBehavior &~ CommandBehavior.SequentialAccess, in CommandType, in TransformedScript);
        }
    }
}
