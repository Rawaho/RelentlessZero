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
using Newtonsoft.Json.Converters;
using RelentlessZero.Entities;
using System.Collections.Generic;

namespace RelentlessZero.Network
{
    public enum PacketDirection
    {
        ClientToServer,
        ServerToClient,
        Bidirectional
    }

    public class PacketHeader
    {
        [JsonProperty(PropertyName = "msg")]
        public string Msg { get; set; }
    }

    // ----------------------------------------------------------------
    // Sub Packet Structures
    // ----------------------------------------------------------------

    public class PacketFullRoom
    {
        [JsonProperty(PropertyName = "room")]
        public PacketRoom Room { get; set; }
        [JsonProperty(PropertyName = "numberOfUsers")]
        public int NumberOfUsers { get; set; }
    }

    public class PacketProfile
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "adminRole")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AdminRole AdminRole { get; set; }
        [JsonProperty(PropertyName = "featureType")]
        public string FeatureType { get; set; }
    }

    public class PacketProfileData
    {
        [JsonProperty(PropertyName = "gold")]
        public uint Gold { get; set; }
        [JsonProperty(PropertyName = "shards")]
        public uint Shards { get; set; }
        [JsonProperty(PropertyName = "starterDeckVersion")]
        public uint StarterDeckVersion { get; set; }
        [JsonProperty(PropertyName = "spectatePermission")]
        public string SpectatePermission { get; set; }
        [JsonProperty(PropertyName = "acceptTrades")]
        public bool AcceptTrades { get; set; }
        [JsonProperty(PropertyName = "acceptChallenges")]
        public bool AcceptChallenges { get; set; }
        [JsonProperty(PropertyName = "rating")]
        public uint Rating { get; set; }
    }

    public class PacketRoom
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "autoIncrement")]
        public bool AutoIncrement { get; set; }
    }

    public class PacketRoomInfoProfile
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "acceptChallenges")]
        public bool AcceptChallenges { get; set; }
        [JsonProperty(PropertyName = "acceptTrades")]
        public bool AcceptTrades { get; set; }
        [JsonProperty(PropertyName = "adminRole")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AdminRole AdminRole { get; set; }
        [JsonProperty(PropertyName = "featureType")]
        public string FeatureType { get; set; }
    }

    // ----------------------------------------------------------------
    // Packet Structures
    // ----------------------------------------------------------------

    [Packet("ActivateGame", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketActivateGame : PacketHeader { }

    [Packet("CardTypes", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketCardTypes : PacketHeader
    {
        [JsonProperty(PropertyName = "cardTypes")]
        public List<ScrollTemplate> CardTypes { get; set; }
    }

    [Packet("Connect", PacketDirection.ClientToServer, SessionType.Lobby, false)]
    public class PacketConnect
    {
        [JsonProperty(PropertyName = "email")]
        public string Username { get; set; }
        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }
        [JsonProperty(PropertyName = "authHash")]
        public string AuthHash { get; set; }
    }

    [Packet("DidYouKnow", PacketDirection.Bidirectional, SessionType.Lobby, false)]
    public class PacketDidYouKnow : PacketHeader
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }
        [JsonProperty(PropertyName = "hint")]
        public string Hint { get; set; }
    }

    [Packet("Fail", PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketFail : PacketHeader
    {
        [JsonProperty(PropertyName = "op")]
        public string Origin { get; set; }
        [JsonProperty(PropertyName = "info")]
        public string Info { get; set; }
    }

    [Packet("FatalFail", PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketFatalFail : PacketFail { }

    [Packet("FirstConnect", PacketDirection.ClientToServer, SessionType.Lobby, false)]
    public class PacketFirstConnect : PacketConnect { }

    [Packet("JoinLobby", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketJoinLobby { }

    [Packet("LibraryView", PacketDirection.Bidirectional, SessionType.Lobby)]
    public class PacketLibraryView : PacketHeader
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "cards")]
        public List<ScrollInstance> Cards { get; set; }
    }

    [Packet("LobbyLookup", PacketDirection.Bidirectional, SessionType.Lookup, false)]
    public class PacketLobbyLookup : PacketHeader
    {
        [JsonProperty(PropertyName = "ip")]
        public string Ip { get; set; }
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
    }

    [Packet("Ok", PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketOk : PacketHeader
    {
        [JsonProperty(PropertyName = "op")]
        public string Origin { get; set; }
    }

    [Packet("OverallStats", PacketDirection.Bidirectional, SessionType.Lobby)]
    public class PacketOverallStats : PacketHeader
    {
        [JsonProperty(PropertyName = "loginsLast24h")]
        public uint PlayersOnline24h { get; set; }
        [JsonProperty(PropertyName = "nrOfProfiles")]
        public int PlayersOnline { get; set; }
        [JsonProperty(PropertyName = "serverName")]
        public string ServerName { get; set; }

        // TODO: implement this correctly
        [JsonProperty(PropertyName = "topRanked")]
        public string[] TopRanked { get; set; }
        [JsonProperty(PropertyName = "weeklyWinners")]
        public string[] WeeklyWinners { get; set; }
    }

    [Packet("Ping", PacketDirection.Bidirectional, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketPing : PacketHeader
    {
        [JsonProperty(PropertyName = "time")]
        public uint Time { get; set; }
    }

    [Packet("ProfileDataInfo", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketProfileDataInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "profileData")]
        public PacketProfileData ProfileData { get; set; }
    }

    [Packet("ProfileInfo", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketProfileInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "profile")]
        public PacketProfile Profile { get; set; }
    }

    [Packet("RoomChatMessage", PacketDirection.Bidirectional, SessionType.Lobby)]
    public class PacketRoomChatMessage : PacketHeader
    {
        [JsonProperty(PropertyName = "roomName")]
        public string RoomName { get; set; }
        [JsonProperty(PropertyName = "from")]
        public string From { get; set; }
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }

    [Packet("RoomEnter", PacketDirection.Bidirectional, SessionType.Lobby)]
    public class PacketRoomEnter : PacketHeader
    {
        [JsonProperty(PropertyName = "roomName")]
        public string RoomName { get; set; }
    }

    [Packet("RoomEnterFree", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketRoomEnterFree : PacketRoomEnter { }

    [Packet("RoomEnterMulti", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketRoomEnterMulti
    {
        [JsonProperty(PropertyName = "roomNames")]
        public List<string> RoomNames { get; set; }
    }

    [Packet("RoomExit", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketRoomExit : PacketRoomEnter { }

    [Packet("RoomInfo", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketRoomInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "roomName")]
        public string RoomName { get; set; }
        [JsonProperty(PropertyName = "reset")]
        public bool Reset { get; set; }
        [JsonProperty(PropertyName = "updated")]
        public List<PacketRoomInfoProfile> Updated { get; set; }
        [JsonProperty(PropertyName = "removed")]
        public List<PacketRoomInfoProfile> Removed { get; set; }
    }

    [Packet("RoomsList", PacketDirection.Bidirectional, SessionType.Lobby)]
    public class PacketRoomsList : PacketHeader
    {
        [JsonProperty(PropertyName = "rooms")]
        public List<PacketFullRoom> Rooms { get; set; }
    }

    [Packet("ServerInfo", PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketServerInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }
        [JsonProperty(PropertyName = "assetURL")]
        public string AssetUrl { get; set; }
        [JsonProperty(PropertyName = "newsURL")]
        public string NewsUrl { get; set; }
        [JsonProperty(PropertyName = "roles")]
        public string Roles { get; set; }
    }
}
