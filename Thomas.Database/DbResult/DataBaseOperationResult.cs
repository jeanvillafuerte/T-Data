namespace Thomas.Database
{
    public class DataBaseOperationResult
    {
        public bool Success { get; set; }
        public int RowsAffected { get; set; }
        public string ErrorMessage { get; set; }

        public static DataBaseOperationResult SuccessResult()
        {
            return new DataBaseOperationResult
            {
                Success = true
            };
        }

        public static T ErrorResult<T>(string message) where T: DataBaseOperationResult, new()
        {
            return new T()
            {
                ErrorMessage = message,
                Success = false
            };
        }
    }

    public class DataBaseOperationAsyncResult : DataBaseOperationResult
    {
        public bool Cancelled { get; set; }

        public static T OperationCancelled<T>() where T : DataBaseOperationAsyncResult, new()
        {
            return new T()
            {
                Success = false,
                Cancelled = true
            };
        }
    }

    public class DataBaseOperationResult<T> : DataBaseOperationResult
    {
        public T Result { get; set; }

        public static DataBaseOperationResult<T> SuccessResult(T result)
        {
            return new DataBaseOperationResult<T>
            {
                Success = true,
                Result = result
            };
        }

    }

    public class DataBaseOperationAsyncResult<T> : DataBaseOperationAsyncResult
    {
        public T Result { get; set; }

        public static DataBaseOperationAsyncResult<T> SuccessResult(T result)
        {
            return new DataBaseOperationAsyncResult<T>
            {
                Success = true,
                Result = result
            };
        }

    }
}