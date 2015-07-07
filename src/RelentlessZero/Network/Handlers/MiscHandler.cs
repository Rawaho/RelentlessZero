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

using RelentlessZero.Managers;
using System;

namespace RelentlessZero.Network.Handlers
{
    public static class MiscHandler
    {
        [PacketHandler("DidYouKnow")]
        public static void HandleDidYouKnow(object packet, Session session)
        {
            // TODO: implement this correctly
            var didYouKnow = new PacketDidYouKnow()
            {
                Id   = 69u,
                Hint = "Welcome to this RelentlessZero server!"
            };

            session.Send(didYouKnow);
        }

        [PacketHandler("LobbyLookup")]
        public static void HandleLobbyLookup(object packet, Session session)
        {
            var lobbyLookup = new PacketLobbyLookup()
            {
                Ip   = ConfigManager.Config.Network.Host,
                Port = ConfigManager.Config.Network.LobbyPort,
            };

            session.Send(lobbyLookup);
        }

        [PacketHandler("OverallStats")]
        public static void HandleOverallStats(object packet, Session session)
        {
            var overallStats = new PacketOverallStats()
            {
                ServerName       = ConfigManager.Config.Server.Name,
                PlayersOnline    = WorldManager.GetSessionCount(),
                PlayersOnline24h = 0u,

                // TODO: implement this correctly
                TopRanked     = new string[0],
                WeeklyWinners = new string[0]
            };

            session.Send(overallStats);
        }

        [PacketHandler("Ping")]
        public static void HandlePing(object packet, Session session)
        {
            var ping = new PacketPing()
            {
                Time = (uint)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
            };

            session.Send(ping);
        }
    }
}
