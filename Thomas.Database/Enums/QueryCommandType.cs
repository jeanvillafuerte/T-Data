namespace Thomas.Database
{
    internal enum QueryCommandType : byte
    {
        QueryOnly,
        StaticParameterValues,
        DynamicParameterValues,
    }
}
