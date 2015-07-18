/*
 * Copyright (C) 2013-2015 RelentlessZero
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

using RelentlessZero.Database;
using RelentlessZero.Entities;
using RelentlessZero.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace RelentlessZero.Managers
{
    public static class WorldManager
    {
        private static int sessionCounter;
        private static ConcurrentDictionary<string, Session> sessionMap;

        static WorldManager()
        {
            sessionCounter = 0;
            sessionMap = new ConcurrentDictionary<string, Session>();
        }

        public static bool AddPlayerSession(Session session)
        {
            if (session.Player == null)
                return false;

            if (IsPlayerOnline(session.Player.Username))
                return false;

            if (sessionMap.TryAdd(session.Player.Username, session))
            {
                if (!session.Player.HasFlag(PlayerFlags.HidePlayer))
                    SessionCountIncrement();

                return true;
            }

            return false;
        }

        public static bool RemovePlayerSession(Session session)
        {
            if (session.Player == null)
                return false;

            if (!IsPlayerOnline(session.Player.Username))
                return false;

            Session removedSession;
            if (sessionMap.TryRemove(session.Player.Username, out removedSession))
            {
                if (!session.Player.HasFlag(PlayerFlags.HidePlayer))
                    SessionCountDecrement();

                return true;
            }

            return false;
        }

        public static Session GetPlayerSession(string name)
        {
            if (!IsPlayerOnline(name))
                return null;

            Session session;
            sessionMap.TryGetValue(name, out session);

            return session;
        }

        public static Session GetPlayerSessionById(uint id)
        {
            foreach (KeyValuePair<string, Session> itSession in sessionMap)
            {
                if (itSession.Value.Player.Id == id)
                {
                    return itSession.Value;
                }
            }
            return null;
        }

        public static bool IsPlayerOnline(string name)
        {
            return sessionMap.ContainsKey(name);
        }

        public static int GetSessionCount()
        {
            return Thread.VolatileRead(ref sessionCounter);
        }

        private static void SessionCountIncrement()
        {
            Interlocked.Increment(ref sessionCounter);
        }

        private static void SessionCountDecrement()
        {
            Interlocked.Decrement(ref sessionCounter);
        }
    }
}
