using System.Collections.Generic;

namespace Thomas.Database.Core.FluentApi
{
    public class DbTable
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        private string _dbName;

        public string DbName
        {
            get
            {

                if (string.IsNullOrEmpty(_dbName))
                    return Name;
                return _dbName;
            }
            set { _dbName = value; }
        }

        public DbColumn Key { get; set; }
        public List<DbColumn> Columns { get; set; }

        public DbTable()
        {
            Columns = new List<DbColumn>();
        }
    }
}
