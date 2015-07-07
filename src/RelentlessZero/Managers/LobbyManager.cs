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

using RelentlessZero.Entities;
using RelentlessZero.Logging;
using RelentlessZero.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace RelentlessZero.Managers
{
    public class RoomInfo
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public AdminRole Permission { get; set; }
        public bool Incremental { get; set; }
        public uint Id { get; set; }

        private ConcurrentDictionary<string, Player> playerStore;
        private int sessionCounter;

        public RoomInfo(string name, string password, AdminRole permission, bool incremental, uint id) 
        {
            Name        = name;
            Password    = password;
            Permission  = permission;
            Incremental = incremental;
            Id          = id;

            playerStore = new ConcurrentDictionary<string, Player>();
        }

        public bool AddPlayer(Player player)
        {
            if (!playerStore.TryAdd(player.Username, player))
                return false;

            // don't modify room player count if player is hidden
            bool isHidden = player.HasFlag(PlayerFlags.HidePlayer);
            if (!isHidden)
                Interlocked.Increment(ref sessionCounter);

            var roomInfo = new PacketRoomInfo()
            {
                RoomName = Name,
                Reset    = false,
                Updated  = new List<PacketRoomInfoProfile>() 
                { 
                    player.GenerateRoomInfoProfile() 
                }
            };

            // notify other players in the room that a new player has joined
            foreach (var otherPlayer in playerStore)
            {
                if (otherPlayer.Value == player)
                    continue;

                // hidden players will only be visible to staff members
                if (isHidden && otherPlayer.Value.AdminRole != AdminRole.Mojang)
                    continue;

                otherPlayer.Value.Session.Send(roomInfo);
            }

            return true;
        }

        public bool RemovePlayer(Player player)
        {
            Player removedPlayer;
            if (!playerStore.TryRemove(player.Username, out removedPlayer))
                return false;

            // don't modify room player count if player is hidden
            if (!player.HasFlag(PlayerFlags.HidePlayer))
                Interlocked.Decrement(ref sessionCounter);

            var roomInfo = new PacketRoomInfo()
            {
                RoomName = Name,
                Removed  = new List<PacketRoomInfoProfile>() 
                { 
                    player.GenerateRoomInfoProfile() 
                }
            };

            // notify other players in the room that a new player has left
            foreach (var otherPlayer in playerStore)
            {
                if (otherPlayer.Value == player)
                    continue;

                if (player.HasFlag(PlayerFlags.HidePlayer) && otherPlayer.Value.AdminRole != AdminRole.Mojang)
                    continue;

                otherPlayer.Value.Session.Send(roomInfo);
            }

            return true;
        }

        public void BroadcastMessage(string from, string message)
        {
            var roomChatMessage = new PacketRoomChatMessage()
            {
                From     = from,
                Text     = message,
                RoomName = Name
            };

            // send a chat message to all players in room
            foreach (var session in playerStore)
                session.Value.Session.Send(roomChatMessage);
        }

        public int GetPlayerCount()
        {
            return Thread.VolatileRead(ref sessionCounter);
        }

        // returns information on all players in a room, this is called on inital join to a room
        public List<PacketRoomInfoProfile> GeneratePlayerList(Player player)
        {
            var roomInfoProfileList = new List<PacketRoomInfoProfile>();

            foreach (var otherPlayer in playerStore)
            {
                if (otherPlayer.Value.HasFlag(PlayerFlags.HidePlayer) && player.AdminRole != AdminRole.Mojang)
                    continue;

                roomInfoProfileList.Add(otherPlayer.Value.GenerateRoomInfoProfile());
            }

            return roomInfoProfileList;
        }
    }

    public static class LobbyManager
    {
        private static ConcurrentDictionary<string, RoomInfo> roomStore;
        private static List<string> roomList;
        private static bool isInitialised = false;

        public static void Initailse()
        {
            roomStore = new ConcurrentDictionary<string, RoomInfo>();
            roomList = new List<string>();

            foreach (ConfigDefaultRoom room in ConfigManager.Config.Room.DefaultRooms)
                AddRoom(room.Name, room.Password, room.Permission, room.Incremental, 1u, room.RoomList);

            isInitialised = true;

            LogManager.Write("Lobby Manager", "Initialised {0} default chat channel(s).", roomStore.Count);
        }

        // TODO: handle better return results
        public static bool AddRoom(string roomName, string password = "", AdminRole permission = AdminRole.None, bool incremental = false, uint id = 0u, bool displayInList = false)
        {
            if (roomName == "Message")
                return true;

            if (password.Length > ConfigManager.Config.Room.MaxRoomPasswordLength)
            {
                LogManager.Write("Lobby Manager", "Failed to add room {0}, password length is too long!", roomName);
                return false;
            }

            if (permission > AdminRole.Mojang)
            {
                LogManager.Write("Lobby Manager", "Failed to add room {0}, permision id is invalid!");
                return false;
            }

            if (roomStore.Count > ConfigManager.Config.Room.MaxRooms)
            {
                LogManager.Write("Lobby Manager", "Failed to add room {0}, max room limit reached!", roomName);
                return false;
            }

            if (!roomStore.TryAdd(roomName, new RoomInfo(roomName, password, permission, incremental, id)))
            {
                //LogManager.Write("Lobby Manager", "Failed to add room {0}, it already exists!", roomName);
                return false;
            }

            if (displayInList && !isInitialised)
                roomList.Add(roomName);

            return true;
        }

        public static bool AddPlayer(string roomName, Player player, bool welcomeMessage = true)
        {
            if (roomName == "Message")
                return true;

            if (player.CurrentRooms.Contains(roomName))
                return false;

            if (player.CurrentRooms.Count >= 10)
                return false;

            RoomInfo room;
            if (!roomStore.TryGetValue(roomName, out room))
            {
                if (!AddRoom(roomName))
                    return false;

                room = roomStore[roomName];
            }

            if (room.GetPlayerCount() >= ConfigManager.Config.Room.MaxPlayersPerRoom)
            {
                if (room.Incremental)
                {
                    // find next avaliable name to create new room
                    uint offset = 1u;
                    string newName = room.Name + "-" + (room.Id + offset);

                    while (!AddRoom(newName, room.Password, room.Permission, true, (room.Id + offset), false))
                        newName = room.Name + "-" + (room.Id + offset++);

                    room = roomStore[newName];
                }
                else
                    return false;
            }

            if (player.AdminRole < room.Permission)
                return false;

            if (!room.AddPlayer(player))
                return false;

            player.CurrentRooms.Add(room.Name);

            // notify client that joining room was successful
            var roomEnter = new PacketRoomEnter
            {
                RoomName = room.Name
            };

            player.Session.Send(roomEnter);

            // send current players in room to player joining
            var roomInfo = new PacketRoomInfo()
            {
                RoomName = room.Name,
                Reset    = false,
                Updated  = room.GeneratePlayerList(player)
            };

            player.Session.Send(roomInfo);

            // only display welcome message if player is not reconnecting to room
            if (welcomeMessage)
                player.SendRoomMessage(room.Name, "You have joined \"" + room.Name + "\"");

            return true;
        }

        public static bool RemovePlayer(string roomName, Player player)
        {
            if (roomName == "Message")
                return true;

            if (!player.CurrentRooms.Contains(roomName))
                return false;

            RoomInfo room;
            if (!roomStore.TryGetValue(roomName, out room))
                return false;

            if (room.RemovePlayer(player))
                player.CurrentRooms.Remove(roomName);

            return true;
        }

        public static bool RoomIncremental(string roomName)
        {
            RoomInfo room;
            if (!roomStore.TryGetValue(roomName, out room))
                return false;

            return room.Incremental;
        }

        public static bool BroadcastMessage(string roomName, string message, string from)
        {
            // both these checks are preformed client side as well
            if (string.IsNullOrWhiteSpace(message) || message.Length > 512)
                return false;

            RoomInfo room;
            if (!roomStore.TryGetValue(roomName, out room))
                return false;

            room.BroadcastMessage(from, message);
            return true;
        }

        public static List<PacketFullRoom> GenerateRoomList()
        {
            var fullRoomList = new List<PacketFullRoom>();

            foreach (string roomName in roomList)
            {
                RoomInfo roomInfo;
                if (!roomStore.TryGetValue(roomName, out roomInfo))
                    continue;

                var room = new PacketFullRoom
                {
                    NumberOfUsers = roomInfo.Incremental ? 0 : roomInfo.GetPlayerCount(),
                    Room = new PacketRoom()
                    {
                        Name          = roomInfo.Name,
                        AutoIncrement = roomInfo.Incremental
                    }
                };

                fullRoomList.Add(room);
            }

            return fullRoomList;
        }
    }
}
