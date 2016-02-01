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
using System.Data;
using System.Collections.Generic;
using System.Text;

namespace RelentlessZero.Database
{
    public static class DatabaseManager
    {
        public static DatabaseConnection Database = new DatabaseConnection();
    }

    public class DatabaseConnection
    {
        private MySqlConnection connection;
        private string connectionString;
        private object mysqlLock;

        public void Initialise(string host, int port, string user, string password, string database)
        {
            if (connection != null)
                return;

            mysqlLock = new object();

            connectionString = "Server=" + host + ";User Id=" + user + ";Port=" + port + ";" +
                "Password=" + password + ";Database=" + database + ";CharSet=utf8";

            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();

                LogManager.Write("Database Manager", "Successfully connected to database.");
            }
            catch (MySqlException exception)
            {
                LogManager.Write("Database Manager", "An exception occured while connecting to database!");
                LogManager.Write("Database Manager", "Exception: {0}", exception.Message);
                Environment.Exit(0);
            }
        }

        public bool Execute(string sql, params object[] args)
        {
            if (connection == null)
                return false;

            lock (mysqlLock)
            {
                try
                {
                    using (MySqlCommand sqlCommand = new MySqlCommand(sql, connection))
                    {
                        var mParams = new List<MySqlParameter>(args.Length);

                        foreach (var a in args)
                            mParams.Add(new MySqlParameter("", a));

                        sqlCommand.Parameters.AddRange(mParams.ToArray());
                        sqlCommand.ExecuteNonQuery();
                    }

                    return true;
                }
                catch (MySqlException exception)
                {
                    LogManager.Write("Database Manager", "An exception occured while executing data against the database!");
                    LogManager.Write("Database Manager", "Exception: {0}", exception.Message);
                    return false;
                }
            }
        }

        public SqlResult Select(string sql, params object[] args)
        {
            if (connection == null)
                return null;

            lock (mysqlLock)
            {
                try
                {
                    using (var sqlCommand = new MySqlCommand(sql, connection))
                    {
                        var mParams = new List<MySqlParameter>(args.Length);

                        foreach (var a in args)
                            mParams.Add(new MySqlParameter("", a));

                        sqlCommand.Parameters.AddRange(mParams.ToArray());

                        using (var sqlData = sqlCommand.ExecuteReader(CommandBehavior.Default))
                        {
                            using (var retData = new SqlResult())
                            {
                                retData.Load(sqlData);
                                retData.Count = retData.Rows.Count;

                                return retData;
                            }
                        }
                    }
                }
                catch (MySqlException exception)
                {
                    LogManager.Write("Database Manager", "An exception occured while selecting data from the database!");
                    LogManager.Write("Database Manager", "Exception: {0}", exception.Message);
                }
            }

            return null;
        }
    }
}
