/*
 * Copyright (C) 2013-2016 RelentlessZero
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using MySql.Data.MySqlClient;
using RelentlessZero.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using System.Threading;

namespace RelentlessZero.Database
{
    public static partial class DatabaseManager
    {
        private static ReadOnlyDictionary<MySqlDbType, Type> argumentType = new ReadOnlyDictionary<MySqlDbType, Type>(new Dictionary<MySqlDbType, Type>()
        {
            { MySqlDbType.UByte,   typeof(byte)   },
            { MySqlDbType.UInt16,  typeof(ushort) },
            { MySqlDbType.UInt32,  typeof(uint)   },
            { MySqlDbType.UInt64,  typeof(ulong)  },
            { MySqlDbType.Byte,    typeof(sbyte)  },
            { MySqlDbType.Int16,   typeof(short)  },
            { MySqlDbType.Int32,   typeof(int)    },
            { MySqlDbType.Int64,   typeof(long)   },
            { MySqlDbType.VarChar, typeof(string) }
        });

        private class StatementQuery
        {
            public PreparedStatement Id { get; }
            public string Text { get; }
            public List<MySqlDbType> ArgumentTypes { get; } = new List<MySqlDbType>();

            public StatementQuery(PreparedStatement id, string text, params MySqlDbType[] argumentTypes)
            {
                Id   = id;
                Text = text;
                ArgumentTypes.AddRange(argumentTypes);
            }

            public void CheckArguments(object[] arguments)
            {
                Contract.Requires(ArgumentTypes.Count == arguments.Length);

                for (int i = 0; i < ArgumentTypes.Count; i++)
                {
                    try
                    {
                        var argument = arguments[i];
                        if (argument is Enum || argument is bool)
                            arguments[i] = Convert.ChangeType(argument, argumentType[ArgumentTypes[i]]);
                    }
                    catch (Exception exception)
                    {
                        LogManager.Write("Database Manager", "An exception occured while converting query argument!");
                        LogManager.Write("Database Manager", $"Exception: {exception.Message}");
                    }

                    Contract.Assert(arguments[i].GetType() == argumentType[ArgumentTypes[i]]);
                }
            }
        }

        private class Statement
        {
            public StatementQuery Query { get; }
            public List<object> Data { get; } = new List<object>();

            public Statement(PreparedStatement id, object[] arguments)
            {
                var query = preparedStatements[id];
                query.CheckArguments(arguments);

                Query = query;
                Data.AddRange(arguments);
            }
        }

        public class Transaction
        {
            private List<Statement> statements;
            private Dictionary<PreparedStatement, uint> statementCount;

            private Dictionary<PreparedStatement, MySqlCommand> cachedPreparedStatements;

            public Transaction()
            {
                statements               = new List<Statement>();
                statementCount           = new Dictionary<PreparedStatement, uint>();

                cachedPreparedStatements = new Dictionary<PreparedStatement, MySqlCommand>();
            }

            public void AddStatement(PreparedStatement id, params object[] arguments)
            {
                if (statementCount.ContainsKey(id))
                    statementCount[id]++;
                else
                    statementCount.Add(id, 1);

                Contract.Assert(preparedStatements.ContainsKey(id));
                statements.Add(new Statement(id, arguments));
            }

            [SuppressMessage("Microsoft.Security", "CA2100")]
            public void Commit()
            {
                if (statements.Count == 0)
                    return;

                MySqlConnection connection = null;
                MySqlTransaction transaction = null;

                try
                {
                    connection = new MySqlConnection(connectionString);
                    connection.Open();

                    using (transaction = connection.BeginTransaction())
                    {
                        foreach (var statement in statements)
                        {
                            var query   = statement.Query;
                            var command = cachedPreparedStatements.ContainsKey(query.Id) ? cachedPreparedStatements[query.Id] : new MySqlCommand(query.Text, connection, transaction);

                            if (!command.IsPrepared)
                            {
                                for (int i = 0; i < statement.Query.ArgumentTypes.Count; i++)
                                    command.Parameters.Add($"@arg{i}", query.ArgumentTypes[i]);

                                if (statementCount[query.Id] > 1)
                                {
                                    command.Prepare();
                                    cachedPreparedStatements.Add(query.Id, command);
                                }
                            }

                            for (int i = 0; i < statement.Data.Count; i++)
                                command.Parameters[$"@arg{i}"].Value = statement.Data[i];

                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }
                catch (Exception exception)
                {
                    if (transaction != null)
                        transaction.Rollback();

                    LogManager.Write("Database Manager", "An exception occured while commiting transaction database!");
                    LogManager.Write("Database Manager", $"Exception: {exception.Message}");
                }
                finally
                {
                    statements.Clear();
                    statementCount.Clear();
                    cachedPreparedStatements.Clear();

                    connection.Close();
                }
            }
        }

        private static string connectionString;
        private static Dictionary<PreparedStatement, StatementQuery> preparedStatements;

        public static void Initialise(string host, int port, string user, string password, string database)
        {
            preparedStatements = new Dictionary<PreparedStatement, StatementQuery>();

            var connectionBuilder = new MySqlConnectionStringBuilder();
            connectionBuilder.Server          = host;
            connectionBuilder.UserID          = user;
            connectionBuilder.Password        = password;
            connectionBuilder.Database        = database;
            connectionBuilder.IgnorePrepare   = false;
            connectionBuilder.Pooling         = true;
            connectionBuilder.MaximumPoolSize = 25u;
            connectionString = connectionBuilder.ToString();

            for (;;)
            {
                try
                {
                    using (var connection = new MySqlConnection(connectionString))
                        connection.Open();

                    LogManager.Write("Database Manager", "Successfully connected to database.");
                    break;
                }
                catch (Exception exception)
                {
                    LogManager.Write("Database Manager", $"Exception: {exception.Message}");
                    LogManager.Write("Database Manager", "Attempting connection to database again in 5 seconds...");

                    Thread.Sleep(5000);
                }
            }

            InitialisePreparedStatements();
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        private static void AddPreparedStatement(PreparedStatement id, string query, params MySqlDbType[] types)
        {
            Contract.Requires(!preparedStatements.ContainsKey(id));

            var regex = new Regex("\\?");

            uint argumentCount = 0u;
            while (query.Contains("?"))
                query = regex.Replace(query, $"@arg{argumentCount++}", 1);

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        for (uint i = 0u; i < argumentCount; i++)
                        {
                            Contract.Assert(argumentType.ContainsKey(types[i]));
                            command.Parameters.Add($"@arg{i}", types[i]);
                        }

                        command.Prepare();

                        if (command.IsPrepared)
                            preparedStatements.Add(id, new StatementQuery(id, query, types));
                    }
                }
            }
            catch (Exception exception)
            {
                LogManager.Write("Database Manager", $"An exception occured while preparing statement {id}!");
                LogManager.Write("Database Manager", $"Exception: {exception.Message}");
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public static long ExecutePreparedStatement(PreparedStatement id, params object[] arguments)
        {
            Contract.Assert(preparedStatements.ContainsKey(id));

            var query = preparedStatements[id];
            query.CheckArguments(arguments);

            long lastId = 0;
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query.Text, connection))
                    {
                        for (int i = 0; i < query.ArgumentTypes.Count; i++)
                            command.Parameters.Add($"@arg{i}", query.ArgumentTypes[i]);

                        for (int i = 0; i < arguments.Length; i++)
                            command.Parameters[$"@arg{i}"].Value = arguments[i];

                        command.ExecuteNonQuery();
                        lastId = command.LastInsertedId;
                    }
                }
            }
            catch (Exception exception)
            {
                LogManager.Write("Database Manager", $"An exception occured while executing prepared statement {id}!");
                LogManager.Write("Database Manager", $"Exception: {exception.Message}");
            }

            return lastId;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public static SqlResult SelectPreparedStatement(PreparedStatement id, params object[] arguments)
        {
            Contract.Assert(preparedStatements.ContainsKey(id));

            var query = preparedStatements[id];
            query.CheckArguments(arguments);

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new MySqlCommand(query.Text, connection))
                    {
                        for (int i = 0; i < query.ArgumentTypes.Count; i++)
                            command.Parameters.Add($"@arg{i}", query.ArgumentTypes[i]);

                        for (int i = 0; i < arguments.Length; i++)
                            command.Parameters[$"@arg{i}"].Value = arguments[i];

                        using (var sqlData = command.ExecuteReader(CommandBehavior.Default))
                        {
                            using (var data = new SqlResult())
                            {
                                data.Load(sqlData);
                                data.Count = data.Rows.Count;
                                return data;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                LogManager.Write("Database Manager", $"An exception occured while selecting prepared statement {id}!");
                LogManager.Write("Database Manager", $"Exception: {exception.Message}");
            }

            return null;
        }
    }
}
