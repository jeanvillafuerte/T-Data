namespace Thomas.Database
{
    public class DbOpResult
    {
        public bool Success { get; set; }
        public int RowsAffected { get; set; }
        public string ErrorMessage { get; set; }

        public static DbOpResult SuccessResult()
        {
            return new DbOpResult
            {
                Success = true
            };
        }

        public static T ErrorResult<T>(in string message) where T : DbOpResult, new()
        {
            return new T()
            {
                ErrorMessage = message,
                Success = false
            };
        }
    }

    public class DbOpAsyncResult : DbOpResult
    {
        public bool Cancelled { get; set; }

        public static T OperationCancelled<T>() where T : DbOpAsyncResult, new()
        {
            return new T()
            {
                Success = false,
                Cancelled = true
            };
        }
    }

    public class DbOpResult<T> : DbOpResult
    {
        public T Result { get; set; }

        public static DbOpResult<T> SuccessResult(T result)
        {
            return new DbOpResult<T>
            {
                Success = true,
                Result = result
            };
        }

    }

    public class DbOpAsyncResult<T> : DbOpAsyncResult
    {
        public T Result { get; set; }

        public static DbOpAsyncResult<T> SuccessResult(T result)
        {
            return new DbOpAsyncResult<T>
            {
                Success = true,
                Result = result
            };
        }

    }
}