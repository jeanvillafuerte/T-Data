namespace TData.DbResult
{
    public class TDataRow
    {
        private readonly object[] _data;

        public object this[int index]
        {
            get { return _data[index]; }
        }

        public TDataRow(in object[] data)
        {
            _data = data;
        }
    }
}
