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
using System;
using System.Collections.Generic;
using System.Reflection;

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

    public class PacketAvatar
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

    public class PacketCard
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }
        [JsonProperty(PropertyName = "typeId")]
        public uint TypeId { get; set; }
        [JsonProperty(PropertyName = "tradable")]
        public bool Tradable { get; set; }
        [JsonProperty(PropertyName = "isToken")]
        public bool IsToken { get; set; }
        [JsonProperty(PropertyName = "level")]
        public uint Level { get; set; }
    }

    public class PacketDeck
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "resources")]
        public string Resources { get; set; }
        [JsonProperty(PropertyName = "valid")]
        public bool Valid { get; set; }
        [JsonProperty(PropertyName = "updated")]
        public string Updated { get; set; }
        [JsonProperty(PropertyName = "timestamp")]
        public uint TimeStamp { get; set; }
    }

    public class PacketIdol
    {
        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PlayerColor Color { get; set; }
        [JsonProperty(PropertyName = "position")]
        public int position { get; set; }
        [JsonProperty(PropertyName = "hp")]
        public int Hp { get; set; }
        [JsonProperty(PropertyName = "maxHp")]
        public int MaxHp { get; set; }
    }

    public class PacketIdolTypes
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "type")] // TODO : check what this type is, and maybe replace string by enum ?
        public string Type { get; set; }
        [JsonProperty(PropertyName = "idol1")]
        public int Idol1 { get; set; }
        [JsonProperty(PropertyName = "idol2")]
        public int Idol2 { get; set; }
        [JsonProperty(PropertyName = "idol3")]
        public int Idol3 { get; set; }
        [JsonProperty(PropertyName = "idol4")]
        public int Idol4 { get; set; }
        [JsonProperty(PropertyName = "idol5")]
        public int Idol5 { get; set; }
    }

    public class PacketFullRoom
    {
        [JsonProperty(PropertyName = "room")]
        public PacketRoom Room { get; set; }
        [JsonProperty(PropertyName = "numberOfUsers")]
        public int NumberOfUsers { get; set; }
    }

    public class PacketGoldReward
    {
        [JsonProperty(PropertyName = "matchReward")]
        public uint MatchReward { get; set; }
        [JsonProperty(PropertyName = "tierMatchReward")]
        public uint TierMatchReward { get; set; }
        [JsonProperty(PropertyName = "matchCompletionReward")]
        public uint MatchCompletionReward { get; set; }
        [JsonProperty(PropertyName = "idolsDestroyedReward")]
        public uint IdolsDestroyedReward { get; set; }
        [JsonProperty(PropertyName = "betReward")]
        public uint betReward { get; set; }
    }

    public class PacketPlayerStats
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "idolDamage")]
        public uint IdolDamage { get; set; }
        [JsonProperty(PropertyName = "unitDamage")]
        public uint UnitDamage { get; set; }
        [JsonProperty(PropertyName = "unitsPlayed")]
        public uint UnitsPlayed { get; set; }
        [JsonProperty(PropertyName = "spellsPlayed")]
        public uint SpellsPlayed { get; set; }
        [JsonProperty(PropertyName = "enchantmentsPlayed")]
        public uint EnchantmentsPlayed { get; set; }
        [JsonProperty(PropertyName = "scrollsDrawn")]
        public uint ScrollsDrawn { get; set; }
        [JsonProperty(PropertyName = "totalMs")]
        public uint TotalMs { get; set; }
        [JsonProperty(PropertyName = "mostDamageUnit")]
        public uint MostDamageUnit { get; set; }
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
    // Packet Effects Structures
    // ----------------------------------------------------------------

    // custom serializer for effects subpackets. Meh.
    public class JsonPacketEffectSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
                PacketEffect effect = (PacketEffect)value;
                PacketEffectAttribute effectAttribute = effect.GetType().GetCustomAttribute<PacketEffectAttribute>();
                if (effectAttribute == null)
                    throw new NotSupportedException("All PacketEffect classes must have a PacketEffectAttribute !");

                writer.WriteStartObject();
                writer.WritePropertyName(effectAttribute.Name);
                writer.WriteStartObject();
                foreach (PropertyInfo info in value.GetType().GetProperties())
                {
                    string propertyName;
                    JsonPropertyAttribute propertyAttribute = info.GetCustomAttribute<JsonPropertyAttribute>();
                    if (propertyAttribute != null)
                        propertyName = propertyAttribute.PropertyName;
                    else
                        propertyName = info.Name;
                    writer.WritePropertyName(propertyName);

                    JsonConverterAttribute conversionAttribute = info.GetCustomAttribute<JsonConverterAttribute>();
                    if (conversionAttribute != null)
                        writer.WriteRawValue(JsonConvert.SerializeObject(info.GetValue(value), Formatting.None, (JsonConverter)Activator.CreateInstance(conversionAttribute.ConverterType)));
                    else
                        writer.WriteRawValue(JsonConvert.SerializeObject(info.GetValue(value)));
                }
                writer.WriteEndObject();
                writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // effects packets are sent by server and should never be received from client
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(PacketEffect).IsAssignableFrom(objectType);
        }
    }

    [JsonConverter(typeof(JsonPacketEffectSerializer))]
    public abstract class PacketEffect {}

    [PacketEffect("IdolUpdateEffect")]
    public class PacketIdolUpdateEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "idol")]
        public PacketIdol Idol { get; set; }
    }

    [PacketEffect("MulliganDisabledEffect")]
    public class PacketMulliganDisabledEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        public PlayerColor Color { get; set; }
    }

    [PacketEffect("EndGame")]
    public class PacketEndGameEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "winner")]
        public PlayerColor Winner { get; set; }
        [JsonProperty(PropertyName = "whiteStats")]
        public PacketPlayerStats WhiteStats { get; set; }
        [JsonProperty(PropertyName = "blackStats")]
        public PacketPlayerStats BlackStats { get; set; }
        [JsonProperty(PropertyName = "whiteGoldReward")]
        public PacketGoldReward WhiteGoldReward { get; set; }
        [JsonProperty(PropertyName = "blackGoldReward")]
        public PacketGoldReward BlackGoldReward { get; set; }
    }

    [PacketEffect("SurrenderEffect")]
    public class PacketSurrenderEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PlayerColor Color { get; set; }
    }


    // ----------------------------------------------------------------
    // Packet Structures
    // ----------------------------------------------------------------

    [Packet("ActivateGame", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketActivateGame : PacketHeader { }

    [Packet("BattleRedirect", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketBattleRedirect : PacketHeader
    {
        [JsonProperty(PropertyName = "ip")]
        public string IP { get; set; }
        [JsonProperty(PropertyName = "port")]
        public uint Port { get; set; }
    }

    [Packet("CardTypes", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketCardTypes : PacketHeader
    {
        [JsonProperty(PropertyName = "cardTypes")]
        public List<ScrollTemplate> CardTypes { get; set; }
    }

    [Packet("Connect", PacketDirection.ClientToServer, SessionType.Lobby | SessionType.Battle, false)]
    public class PacketConnect
    {
        [JsonProperty(PropertyName = "email")]
        public string Username { get; set; }
        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }
        [JsonProperty(PropertyName = "authHash")]
        public string AuthHash { get; set; }
    }

    [Packet("DeckList", PacketDirection.Bidirectional, SessionType.Lobby)]
    public class PacketDeckList : PacketHeader
    {
        [JsonProperty(PropertyName = "decks")]
        public List<PacketDeck> Decks { get; set; }
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

    [Packet("GameInfo", PacketDirection.ServerToClient, SessionType.Battle)]
    public class PacketGameInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "white")] // name of left player
        public string White { get; set; }
        [JsonProperty(PropertyName = "black")] // name of right player
        public string Black { get; set; }
        [JsonProperty(PropertyName = "gameType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BattleType GameType { get; set; }
        [JsonProperty(PropertyName = "gameId")]
        public uint GameId { get; set; } // probably id of battle instance in database
        [JsonProperty(PropertyName = "roundTimerSeconds")]
        public int roundTimerSeconds { get; set; }
        [JsonProperty(PropertyName = "phase")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BattlePhase Phase { get; set; }
        [JsonProperty(PropertyName = "whiteAvatar")]
        public PacketAvatar WhiteAvatar { get; set; }
        [JsonProperty(PropertyName = "blackAvatar")]
        public PacketAvatar BlackAvatar { get; set; }
        [JsonProperty(PropertyName = "whiteIdolTypes")]
        public PacketIdolTypes WhiteIdolTypes { get; set; }
        [JsonProperty(PropertyName = "blackIdolTypes")]
        public PacketIdolTypes BlackIdolTypes { get; set; }
        [JsonProperty(PropertyName = "customSettings")]
        public List<string> CustomSettings { get; set; } // TODO : gather more info and change type
        [JsonProperty(PropertyName = "rewardForIdolKill")]
        public uint RewardForIdolKill { get; set; }
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }
        [JsonProperty(PropertyName = "port")]
        public uint Port { get; set; }
        [JsonProperty(PropertyName = "whiteIdols")]
        public List<PacketIdol> WhiteIdols { get; set; }
        [JsonProperty(PropertyName = "blackIdols")]
        public List<PacketIdol> BlackIdols { get; set; }
        [JsonProperty(PropertyName = "refId")] //TODO : what's that ?
        public int RefId { get; set; }
        [JsonProperty(PropertyName = "maxTierRewardMultiplier")]
        public float MaxTierRewardMultiplier { get; set; }
        [JsonProperty(PropertyName = "tierRewardMultiplierDelta")]
        public List<float> TierRewardMultiplierDelta { get; set; }

    }

    [Packet("GameMatchQueueStatus", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketGameMatchQueueStatus : PacketHeader
    {
        [JsonProperty(PropertyName = "inQueue")]
        public bool InQueue { get; set; }
        [JsonProperty(PropertyName = "gameType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BattleType GameType { get; set; }

    }

    [Packet("JoinLobby", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketJoinLobby { }

    [Packet("JoinBattle", PacketDirection.ClientToServer, SessionType.Battle)]
    public class PacketJoinBattle { }

    [Packet("LeaveGame", PacketDirection.ClientToServer, SessionType.Battle)]
    public class PacketLeaveGame { }

    [Packet("LibraryView", PacketDirection.Bidirectional, SessionType.Lobby)]
    public class PacketLibraryView : PacketHeader
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "cards")]
        public List<PacketCard> Cards { get; set; }
    }

    [Packet("LobbyLookup", PacketDirection.Bidirectional, SessionType.Lookup, false)]
    public class PacketLobbyLookup : PacketHeader
    {
        [JsonProperty(PropertyName = "ip")]
        public string Ip { get; set; }
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
    }

    [Packet("NewEffects", PacketDirection.ServerToClient, SessionType.Battle)]
    public class PacketNewEffects : PacketHeader
    {
        [JsonProperty(PropertyName = "effects")]
        public List<PacketEffect> Effects = new List<PacketEffect>(); 
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

    [Packet("PlaySinglePlayerQuickMatch", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketPlaySinglePlayerQuickMatch : PacketHeader
    {
        [JsonProperty(PropertyName = "robotName")]
        public string RobotName { get; set; }
        [JsonProperty(PropertyName = "deck")]
        public string Deck { get; set; }
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

    [Packet("Surrender", PacketDirection.ClientToServer, SessionType.Battle)]
    public class PacketSurrender : PacketHeader { }
}
