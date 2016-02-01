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

namespace RelentlessZero.Network
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PacketAttribute : Attribute
    {
        public string Name { get; set; }
        public PacketDirection Direction { get; set; }
        public SessionType SessionType { get; set; }
        public bool AuthRequired { get; set; }

        public bool HasDirection(PacketDirection direction) { return (Direction & direction) != 0; }

        public PacketAttribute(string name, PacketDirection direction, SessionType sessionType = SessionType.None, bool authRequired = true)
        {
            Name         = name;
            Direction    = direction;
            SessionType  = sessionType;
            AuthRequired = authRequired;
        }
    }
}
