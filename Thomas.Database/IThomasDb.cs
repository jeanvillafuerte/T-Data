﻿using System;
using System.Collections.Generic;
using System.Data;

namespace Thomas.Database
{
    public interface IThomasDb
    {
        IReadOnlyList<T> DataReaderToList<T>(IDataReader listReader, string script, bool closeConnection = false) where T : new();

        DataBaseOperationResult Execute(string script, bool isStoreProcedure = true);
        DataBaseOperationResult Execute(object inputData, string procedureName);

        DataBaseOperationResult<T> ToSingle<T>(string script, bool isStoreProcedure = true) where T : class, new();
        DataBaseOperationResult<T> ToSingle<T>(object inputData, string procedureName) where T : class, new();

        DataBaseOperationResult<IReadOnlyList<T>> ToList<T>(string script, bool isStoreProcedure = true) where T : class, new();
        DataBaseOperationResult<IReadOnlyList<T>> ToList<T>(object inputData, string procedureName) where T : class, new();

        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>> ToTuple<T1, T2>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>> ToTuple<T1, T2, T3>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>> ToTuple<T1, T2, T3, T4>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>> ToTuple<T1, T2, T3, T4, T5>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>> ToTuple<T1, T2, T3, T4, T5, T6>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(string script, bool isStoreProcedure = true) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new();

        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>>> ToTuple<T1, T2>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>>> ToTuple<T1, T2, T3>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>>> ToTuple<T1, T2, T3, T4>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>>> ToTuple<T1, T2, T3, T4, T5>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>>> ToTuple<T1, T2, T3, T4, T5, T6>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new();
        DataBaseOperationResult<Tuple<IReadOnlyList<T1>, IReadOnlyList<T2>, IReadOnlyList<T3>, IReadOnlyList<T4>, IReadOnlyList<T5>, IReadOnlyList<T6>, IReadOnlyList<T7>>> ToTuple<T1, T2, T3, T4, T5, T6, T7>(object inputData, string procedureName) where T1 : class, new() where T2 : class, new() where T3 : class, new() where T4 : class, new() where T5 : class, new() where T6 : class, new() where T7 : class, new();
    }
}
