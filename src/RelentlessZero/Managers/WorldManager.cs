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
using RelentlessZero.Network;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace RelentlessZero.Managers
{
    public static class WorldManager
    {
        private static uint sessionCounter;
        private static object sessionCounterLock;

        private static ConcurrentDictionary<string, Session> sessionMap;

        // contains sessions that have connected in last 24 hours, used with OverallStats packet
        private static ConcurrentDictionary<uint, DateTime> sessionTodayStore;

        private static volatile bool killWorldUpdate;

        static WorldManager()
        {
            sessionCounterLock = new object();
            sessionMap         = new ConcurrentDictionary<string, Session>();
            sessionTodayStore  = new ConcurrentDictionary<uint, DateTime>();
        }

        public static void StartWorldUpdate()
        {
            Thread thread = new Thread(new ThreadStart(UpdateWorld));
            thread.Start();
        }

        public static void StopWorldUpdate() { killWorldUpdate = true; }

        private static void UpdateWorld()
        {
            uint timeDiff = 0;
            while (!killWorldUpdate)
            {
                var startTime = DateTime.UtcNow;

                BattleManager.Update(timeDiff);

                // remove any tracked sessions that are over 24 hours old
                foreach (var session in sessionTodayStore.ToArray())
                {
                    if (session.Value.Second + 86400 <= startTime.Second)
                    {
                        DateTime timestamp;
                        sessionTodayStore.TryRemove(session.Key, out timestamp);
                    }
                }

                Thread.Sleep(1);
                timeDiff = (uint)(DateTime.UtcNow - startTime).TotalMilliseconds;
            }
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

                var time = DateTime.UtcNow;
                sessionTodayStore.AddOrUpdate(session.Player.Id, time, (k, v) => time);
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

        public static Session GetPlayerSession(string name) { return sessionMap.SingleOrDefault(session => session.Value.Player.Username == name).Value; }
        public static Session GetPlayerSession(uint id) { return sessionMap.SingleOrDefault(session => session.Value.Player.Id == id).Value; }
        public static bool IsPlayerOnline(string name) { return sessionMap.ContainsKey(name); }

        public static void Send(object packet, params uint[] ids)
        {
            foreach (uint id in ids)
            {
                if (id == 0)
                    continue;

                var session = GetPlayerSession(id);
                if (session != null)
                    session.Send(packet);
            }
        }

        public static uint GetSessionCountToday() { return (uint)sessionTodayStore.Count; }
        public static uint GetSessionCount() { return sessionCounter; }

        private static void SessionCountIncrement()
        {
            lock (sessionCounterLock)
                sessionCounter++;
        }

        private static void SessionCountDecrement()
        {
            lock (sessionCounterLock)
                sessionCounter--;
        }
    }
}
