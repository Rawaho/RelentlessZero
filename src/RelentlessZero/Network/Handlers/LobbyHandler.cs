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

using RelentlessZero.Logging;
using RelentlessZero.Managers;
using RelentlessZero.Entities;
using System;
using System.Net;

namespace RelentlessZero.Network.Handlers
{
    public static class LobbyHandler
    {
        [PacketHandler("PlaySinglePlayerQuickMatch")]
        public static void HandlePlaySinglePlayerQuickMatch(object packet, Session session)
        {
            // TODO : handle AI robot name and deck

            BattleType[] typesToAbort = new BattleType[3] {BattleType.MP_QUICKMATCH, BattleType.MP_RANKED, BattleType.MP_LIMITED};

            foreach (BattleType battleType in typesToAbort)
            {
                var cancelQueue = new PacketGameMatchQueueStatus()
                {
                    InQueue = false,
                    GameType = battleType
                };
                session.Send(cancelQueue);
            }

            var battleRedirect = new PacketBattleRedirect()
            {
                IP = ((IPEndPoint)session.Socket.LocalEndPoint).Address.ToString(),
                Port = (uint)ConfigManager.Config.Network.BattlePort
            };
            session.Send(battleRedirect);
        }

        [PacketHandler("RoomChatMessage")]
        public static void HandleRoomChatMessage(object packet, Session session)
        {
            var roomChatMessage = (PacketRoomChatMessage)packet;

            // TODO: command stuff here...

            LobbyManager.BroadcastMessage(roomChatMessage.RoomName, roomChatMessage.Text, session.Player.Username);
        }

        [PacketHandler("RoomEnter")]
        [PacketHandler("RoomEnterFree")]
        public static void HandleRoomEnter(object packet, Session session)
        {
            var roomEnter = (PacketRoomEnter)packet;
            string packetName = packet.GetType().Name.Remove(0, 6);

            if (packetName == "RoomEnterFree")
            {
                if (!LobbyManager.RoomIncremental(roomEnter.RoomName))
                {
                    LogManager.Write("Lobby Handler", "Player {0} tried to enter room {1} via 'RoomEnterFree' but room isn't incremental!",
                        session.Player.Username, roomEnter.RoomName);
                    return;
                }

                // extra check required on 'RoomEnterFree' to prevent error message on reconnect
                if (session.Player.CurrentRooms.Contains(roomEnter.RoomName))
                    return;
            }

            if (LobbyManager.AddPlayer(roomEnter.RoomName, session.Player))
                session.SendOkPacket(packetName);
            else
                session.Player.SendRoomMessage("Message", "Failed to join room " + roomEnter.RoomName + "!");
        }

        [PacketHandler("RoomEnterMulti")]
        public static void HandleRoomEnterMulti(object packet, Session session)
        {
            var roomEnterMulti = (PacketRoomEnterMulti)packet;

            if (roomEnterMulti.RoomNames.Count > 10)
            {
                LogManager.Write("Lobby Handler", "Player {0} tried to enter {1} rooms via 'RoomEnterMulti', this is more then the maximum allowed!",
                    session.Player.Username, roomEnterMulti.RoomNames.Count);
                return;
            }

            foreach (string roomName in roomEnterMulti.RoomNames)
            {
                if (!LobbyManager.AddPlayer(roomName, session.Player, false))
                {
                    session.Player.SendRoomMessage("Message", "Failed to join room " + roomName + "!");
                    return;
                }
            }

            session.SendOkPacket("RoomEnterMulti");
        }

        [PacketHandler("RoomExit")]
        public static void HandleRoomExit(object packet, Session session)
        {
            var roomExit = (PacketRoomExit)packet;

            if (LobbyManager.RemovePlayer(roomExit.RoomName, session.Player))
                session.SendOkPacket("RoomExit");
            else
                session.Player.SendRoomMessage("Message", "Failed to exit room " + roomExit.RoomName + "!");
        }

        [PacketHandler("RoomsList")]
        public static void HandleRoomsList(object packet, Session session)
        { 
            var roomsList = new PacketRoomsList()
            {
                Rooms = LobbyManager.GenerateRoomList()
            };

            session.Send(roomsList);
        }
    }
}
