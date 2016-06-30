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

using RelentlessZero.Entities;
using System;

namespace RelentlessZero.Command
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CommandHandlerAttribute : Attribute
    {
        public string Command { get; }
        public AdminRole AdminRole { get; }
        public bool Console { get; }
        public int Arguments { get; }

        public CommandHandlerAttribute(string command, AdminRole adminRole = AdminRole.Mojang, int arguments = -1, bool console = true)
        {
            Command   = command;
            AdminRole = adminRole;
            Arguments = arguments;
            Console   = console;
        }
    }
}
