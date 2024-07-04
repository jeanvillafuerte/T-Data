﻿using System.Reflection;

namespace Thomas.Database.Core.FluentApi
{
    public class DbColumn
    {
        public bool AutoGenerated { get; set; }
        public string Name { get; set; }
        public string DbName { get; set; }
        public string FullDbName
        {
            get
            {
                if (string.IsNullOrEmpty(DbName))
                    return Name;

                return $"{DbName} AS {Name}";
            }
        }
        public PropertyInfo Property { get; set; }

        //cast to specific type in database 
        public string DbType { get; set; }
        public bool RequireConversion { get; set; }

        public override bool Equals(object obj)
        {
            return obj is DbColumn column &&
                   column.Name == Name;
        }
    }
}
