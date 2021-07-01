using System;

namespace Thomas.Database
{
    public class NoMatchPropertiesException : Exception
    {
        public string[] DbColumnsNoMatch { get; set; }

        public NoMatchPropertiesException() : base()
        {
        }

        public NoMatchPropertiesException(string message) : base(message)
        {
        }

        public NoMatchPropertiesException(string message, string[] dbColumnsNoMatch) : base(message)
        {
            DbColumnsNoMatch = dbColumnsNoMatch;
        }

        public NoMatchPropertiesException(string message, Exception innerException) : base(message, innerException)
        {
        }



    }
}
