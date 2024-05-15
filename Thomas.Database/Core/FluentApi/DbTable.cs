using System.Collections.Generic;

namespace Thomas.Database.Core.FluentApi
{
    public class DbTable
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public string DbName { get; set; }

        public DbColumn Key { get; set; }
        public LinkedList<DbColumn> Columns { get; set; }
    }
}
