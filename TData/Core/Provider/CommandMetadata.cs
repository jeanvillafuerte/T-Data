using System;
using System.Data;
using System.Data.Common;
using static TData.Core.Provider.DatabaseProvider;

namespace TData.Core.Provider
{
    internal sealed class CommandMetaData
    {
        public readonly ConfigureCommandDelegate LoadParametersDelegate;
        public readonly ConfigureCommandDelegate2 LoadParametersDelegate2;

        public readonly Action<object, DbCommand, DbDataReader> LoadOutParametersDelegate;
        public readonly CommandBehavior CommandBehavior;
        public readonly CommandType CommandType;

        //for postgresql store procedure calls
        public readonly string TransformedScript;

        public CommandMetaData(in ConfigureCommandDelegate loadParametersDelegate, in ConfigureCommandDelegate2 loadParametersDelegate2, in Action<object, DbCommand, DbDataReader> loadOutParametersDelegate, in CommandBehavior commandBehavior, in CommandType commandType, in string script, in bool shouldIncludeSequentialBehavior)
        {
            CommandType = commandType;
            LoadParametersDelegate = loadParametersDelegate;
            LoadParametersDelegate2 = loadParametersDelegate2;
            LoadOutParametersDelegate = loadOutParametersDelegate;
            CommandBehavior = shouldIncludeSequentialBehavior ? commandBehavior & CommandBehavior.SequentialAccess : commandBehavior;
            TransformedScript = script;
        }
    }
}
