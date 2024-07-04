namespace Thomas.Database
{
    public class DbOpResult
    {
        public bool Success { get; set; }
        public int RowsAffected { get; set; }
        public string ErrorMessage { get; set; }

        public static implicit operator DbOpResult(int rowsAffected)
        {
            return new DbOpResult
            {
                Success = true,
                RowsAffected = rowsAffected
            };
        }

        public static implicit operator DbOpResult(string errorMessage)
        {
            return new DbOpResult
            {
                ErrorMessage = errorMessage,
                Success = false
            };
        }
    }

    public class DbOpAsyncResult : DbOpResult
    {
        public bool Canceled { get; set; }

        public static T OperationCanceled<T>() where T : DbOpAsyncResult, new()
        {
            return new T()
            {
                Success = false,
                Canceled = true
            };
        }

        public static implicit operator DbOpAsyncResult(int rowsAffected)
        {
            return new DbOpAsyncResult
            {
                Success = true,
                RowsAffected = rowsAffected
            };
        }

        public static implicit operator DbOpAsyncResult(string errorMessage)
        {
            return new DbOpAsyncResult
            {
                ErrorMessage = errorMessage,
                Success = false
            };
        }
    }

    public class DbOpResult<T> : DbOpResult
    {
        public T Result { get; set; }

        public static implicit operator DbOpResult<T>(T result)
        {
            return new DbOpResult<T>
            {
                Success = true,
                Result = result
            };
        }

        public static implicit operator T(DbOpResult<T> result) => result.Result;

        public static implicit operator DbOpResult<T>(string errorMessage)
        {
            return new DbOpResult<T>
            {
                ErrorMessage = errorMessage,
                Success = false
            };
        }

    }

    public class DbOpAsyncResult<T> : DbOpAsyncResult
    {
        public T Result { get; set; }

        public static implicit operator DbOpAsyncResult<T>(T result)
        {
            return new DbOpAsyncResult<T>
            {
                Success = true,
                Result = result
            };
        }

        public static implicit operator DbOpAsyncResult<T>(string errorMessage)
        {
            return new DbOpAsyncResult<T>
            {
                ErrorMessage = errorMessage,
                Success = false
            };
        }
    }
}