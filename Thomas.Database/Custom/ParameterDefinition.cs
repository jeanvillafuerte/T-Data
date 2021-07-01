using System.Data;

namespace Thomas.Database
{
    internal class ParameterDefinition : IDataParameter
    {
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public string ParameterName { get; set; }
        public string SourceColumn { get; set; }
        public DataRowVersion SourceVersion { get; set; }
        public object Value { get; set; }
        public bool IsNullable => false;
    }
}
