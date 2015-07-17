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

using Newtonsoft.Json;
using RelentlessZero.Managers;
using RelentlessZero.Network;
using System;
using System.Collections.Generic;

namespace RelentlessZero.Entities
{
    public class Avatar
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "head")]
        public int Head { get; set; }
        [JsonProperty(PropertyName = "body")]
        public int Body { get; set; }
        [JsonProperty(PropertyName = "leg")]
        public int Leg { get; set; }
        [JsonProperty(PropertyName = "armBack")]
        public int ArmBack { get; set; }
        [JsonProperty(PropertyName = "armFront")]
        public int ArmFront { get; set; }
    }

    public class Player
    {
        public uint Id { get; set; }
        public Session Session { get; set; }
        public AdminRole AdminRole { get; set; }
        public string Username { get; set; }
        public uint Gold { get; set; }
        public uint Shards { get; set; }
        public ushort Rating { get; set; }
        public PlayerFlags Flags { get; set; }

        public Avatar Avatar { get; set; }
        public List<string> CurrentRooms { get; set; }

        public Player()
        {
            CurrentRooms = new List<string>();
        }

        public void OnDisconnect()
        {
            foreach (string roomName in CurrentRooms.ToArray())
                LobbyManager.RemovePlayer(roomName, this);
        }

        public bool HasFlag(PlayerFlags flag) { return (Flags & flag) != 0; }
        public void RemoveFlag(PlayerFlags flag) { Flags &= flag; }

        public PacketRoomInfoProfile GenerateRoomInfoProfile()
        {
            var roomInfoProfile = new PacketRoomInfoProfile()
            {
                ProfileId        = Id,
                Name             = Username,
                AcceptChallenges = HasFlag(PlayerFlags.AcceptChallenges),
                AcceptTrades     = HasFlag(PlayerFlags.AcceptTrades),
                AdminRole        = AdminRole,
                FeatureType      = "PREMIUM" // can also be "DEMO"
            };

            return roomInfoProfile;
        }

        public void SendRoomMessage(string roomName, string message, string from = "Scrolls")
        {
            if (!CurrentRooms.Contains(roomName) && roomName != "Message")
                return;

            var roomChatMessage = new PacketRoomChatMessage()
            {
                RoomName = roomName,
                From     = from,
                Text     = message
            };

            Session.Send(roomChatMessage);
        }
    }
}
