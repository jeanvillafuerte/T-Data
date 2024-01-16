using System;

namespace Thomas.Database.Exceptions
{
    public sealed class UnknownParameterDirectionException : Exception { }

    public sealed class DbSettingsNotFoundException : Exception
    {
        public DbSettingsNotFoundException(string message) : base(message) { }
    }

    public sealed class EmptyDataReaderException : Exception
    {
        public EmptyDataReaderException(string message) : base(message) { }
    }

    public sealed class FieldsNotFoundException : Exception
    {
        public string[] DbColumnsNoMatch { get; set; }

        public FieldsNotFoundException() { }

        public FieldsNotFoundException(string message) : base(message) { }

        public FieldsNotFoundException(string message, Exception innerException) : base(message, innerException) { }

        public FieldsNotFoundException(string message, string[] dbColumnsNoMatch) : base(message)
        {
            DbColumnsNoMatch = dbColumnsNoMatch;
        }
    }

    public sealed class DuplicateSignatureException : Exception { }
}
