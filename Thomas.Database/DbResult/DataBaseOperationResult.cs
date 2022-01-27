using System.Collections.Generic;

namespace Thomas.Database
{
    public class DataBaseOperationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> OutParameters { get; set; }

        public static DataBaseOperationResult SuccessResult()
        {
            return new DataBaseOperationResult
            {
                Success = true
            };
        }

        public static DataBaseOperationResult ErrorResult(string message)
        {
            return new DataBaseOperationResult
            {
                ErrorMessage = message,
                Success = false
            };
        }
    }

    public class DataBaseOperationResult<T> : DataBaseOperationResult
    {
        public DataBaseOperationResult()
        {
            OutParameters = new Dictionary<string, object>();
        }

        public T Result { get; set; }

        public static DataBaseOperationResult<T> SuccessResult(T result)
        {
            return new DataBaseOperationResult<T>
            {
                Success = true,
                Result = result
            };
        }

        public new static DataBaseOperationResult<T> ErrorResult(string message)
        {
            return new DataBaseOperationResult<T>
            {
                ErrorMessage = message,
                Success = false
            };
        }
    }
}