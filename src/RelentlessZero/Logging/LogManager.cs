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

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace RelentlessZero.Logging
{
    public static class LogManager
    {
        private static object logLock;
        private static Regex replaceRegex;

        private const int MAX_PREFIX_LENGTH = 18;

        static LogManager()
        {
            if (File.Exists("./Server.log"))
                File.Delete("./Server.log");

            logLock = new object();
            replaceRegex = new Regex("{[0-9]+}");
        }

        public static void Write(string prefix, string message, params object[] args)
        {
            InsertParams(ref message, args);
            FormatPrefix(ref prefix);

            Console.WriteLine(prefix + message);

            lock (logLock)
                File.AppendAllText("./Server.log", DateTime.Now + " - " + prefix + message + Environment.NewLine);
        }

        private static void FormatPrefix(ref string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return;

            prefix = prefix.Insert(0, "[") + "] " ;

            int insertLength = (MAX_PREFIX_LENGTH + 1) - prefix.Length;
            if (insertLength > 0)
                prefix = prefix.Insert(0, new string(' ', insertLength));
        }

        private static void InsertParams(ref string message, params object[] args)
        {
            if (args.Length == 0)
                return;

            Match match = replaceRegex.Match(message);
            while (match.Success)
            {
                try
                {
                    int replaceIndex = Convert.ToInt32(match.Value.Trim('{', '}'));
                    if (args[replaceIndex] == null)
                        return;

                    message = message.Remove(match.Index, replaceIndex.ToString().Length + 2).Insert(match.Index, Convert.ToString(args[replaceIndex]));
                }
                catch (Exception exception)
                {
                    Write("Log Manager", "An exception occured while inserting parameters!");
                    Write("Log Manager", "Exception: " + exception.Message);
                }

                match = replaceRegex.Match(message);
            }
        }
    }
}
