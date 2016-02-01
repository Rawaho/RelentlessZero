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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RelentlessZero.Managers;
using RelentlessZero.Network;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace RelentlessZero.Entities
{
    public class GoldReward
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
        public uint BetReward { get; set; }
    }

    public class PlayerStats
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

    public class Idol
    {
        public const uint FullHp = 10u;

        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TileColour Colour { get; set; }
        [JsonProperty(PropertyName = "position")]
        public uint Position { get; set; }
        [JsonProperty(PropertyName = "hp")]
        public uint Hp { get; set; }
        [JsonProperty(PropertyName = "maxHp")]
        public uint MaxHp { get; set; }

        public Idol(TileColour colour, uint position)
        {
            Colour   = colour;
            Position = position;
            Hp       = MaxHp = FullHp;
        }
    }

    // ----------------------------------------------------------------

    public struct PendingMove
    {
        public BattleMoveType MoveType { get; }
        public uint Scroll { get; }
        public uint SourceTileX { get; }
        public uint SourceTileY { get; }
        public uint DestinationTileX { get; }
        public uint DestinationTileY { get; }

        public PendingMove(BattleMoveType moveType, uint scroll, uint srcTileX, uint srcTileY, uint dstTileX, uint dstTileY)
        {
            MoveType         = moveType;
            Scroll           = scroll;
            SourceTileX      = srcTileX;
            SourceTileY      = srcTileY;
            DestinationTileX = dstTileX;
            DestinationTileY = dstTileY;
        }
    }

    public class BattleSide
    {
        public const uint IdolCount = 5u;

        public TileColour Colour { get; }

        // player ID and name, ID is always 0 for AI
        public uint Id { get; }
        public string Name { get; }

        public PlayerStats Stats { get; } = new PlayerStats();
        public Idol[] Idols { get; } = new Idol[IdolCount];
        public Deck Deck { get; set; }
        public Avatar Avatar { get; set; }
        public bool IsAI { get; }

        public ConcurrentQueue<PendingMove> MoveQueue { get; } = new ConcurrentQueue<PendingMove>();

        // player has connected to the battle for the first time, always true for AI
        public bool InitialConnect { get; set; }
        // player is currently disconnected from the battle
        public bool Disconnected { get; set; } = true;

        public BattleSide(uint id, string name, TileColour colour, bool isAI = false)
        {
            Id             = id;
            Name           = name;
            Colour         = colour;
            IsAI           = isAI;
            InitialConnect = isAI;

            // initialise idols with default health
            for (uint i = 0; i < IdolCount; i++)
                Idols[i] = new Idol(colour, i);
        }

        public void PlayerConnected(bool firstConnect)
        {
            if (firstConnect)
                InitialConnect = true;

            Disconnected = false;
        }

        public void LeaveGame(bool battleEnded)
        {
            if (battleEnded)
                BattleManager.RemoveBattleInfo(Id);

            Disconnected = true;
        }

        // add a pending move that will be processed next battle update
        public void AddPendingMove(BattleMoveType moveType, uint scroll = 0u, uint srcTileX = 0u, uint srcTileY = 0u, uint dstTileX = 0u, uint dstTileY = 0u)
        {
            MoveQueue.Enqueue(new PendingMove(moveType, scroll, srcTileX, srcTileY, dstTileX, srcTileY));
        }

        public void DamageIdol(uint idol, uint damage, PacketNewEffects packetNewEffects)
        {
            if (idol >= IdolCount)
                return;

            // make sure damage doesn't exceed current idol health
            if (Idols[idol].Hp < damage)
                damage = Idols[idol].Hp;

            Idols[idol].Hp -= damage;

            packetNewEffects.Effects.Add(new PacketIdolUpdateEffect(Idols[idol]));
        }
    }

    // ----------------------------------------------------------------

    public class Battle
    {
        public const uint SideCount         = 2u;
        public const uint RewardForIdolKill = 10u;

        private uint sideId;

        public uint Id { get; }
        public BattleType Type { get; }
        public AiDifficulty Difficulty { get; }
        public BattlePhase Phase { get; set; } = BattlePhase.Init;

        public uint RoundTimer { get; set; }            // time remaining in current turn
        public int RoundTimeSeconds { get; }            // total time of a single turn (-1 for unlimited)
        public TileColour CurrentTurn { get; set; } = TileColour.unknown;

        public BattleSide[] Side { get; } = new BattleSide[SideCount];
        public BattleSide BlackSide { get; set; }
        public BattleSide WhiteSide { get; set; }

        public bool Expired { get; set; }

        // used when waiting for players to initially connect
        public uint StartAttemptTimer { get; set; }
        public byte StartAttemptCount { get; set; }

        public Battle(uint id, BattleType type, AiDifficulty difficulty)
        {
            Id                = id;
            Type              = type;
            Difficulty        = difficulty;
            RoundTimeSeconds  = type == BattleType.SP_QUICKMATCH ? -1 : 30;     // TODO: move turn timer to config file
        }

        public void AddSide(BattleSide battleSide)
        {
            if (sideId >= SideCount)
                return;

            // battle side is stored in a separate array for iteration and by side colour
            Side[sideId++] = battleSide;

            if (battleSide.Colour == TileColour.black)
                BlackSide = battleSide;
            else
                WhiteSide = battleSide;
        }

        // returns the side corresponding to a colour, if opponent is true the opposite side is returned instead
        public BattleSide GetSide(TileColour colour, bool opponent = false)
        {
            return (colour == TileColour.black ? (opponent == true ? WhiteSide : BlackSide) : (opponent == true ? BlackSide : WhiteSide));
        }

        public void EndGame(TileColour winningColour, bool surrender = false)
        {
            if (Phase == BattlePhase.End)
                return;

            Phase = BattlePhase.End;

            var winningSide = GetSide(winningColour);
            var losingSide  = GetSide(winningColour, true);

            var packetNewEffects = new PacketNewEffects();

            if (surrender)
            {
                packetNewEffects.Effects.Add(new PacketSurrenderEffect(losingSide.Colour));

                // destroy remaining idols on losing side
                for (uint i = 0; i < BattleSide.IdolCount; i++)
                    losingSide.DamageIdol(i, Idol.FullHp, packetNewEffects);
            }

            // TODO: calculate rewards

            var packetEndGameEffect = new PacketEndGameEffect()
            {
                Winner          = winningSide.Colour,
                BlackStats      = BlackSide.Stats,
                WhiteStats      = WhiteSide.Stats,
                BlackGoldReward = new GoldReward(),
                WhiteGoldReward = new GoldReward()
            };

            packetNewEffects.Effects.Add(packetEndGameEffect);

            WorldManager.Send(packetNewEffects, BlackSide.Id, WhiteSide.Id);
        }

        public void EndTurn()
        {
        }

        public void LeaveGame(TileColour colour)
        {
            // TODO: broadcast left battle message to opponent
            GetSide(colour).LeaveGame(Phase == BattlePhase.End);
        }

        public PacketGameInfo BuildGameInfo(TileColour colour)
        {
            var gameInfo = new PacketGameInfo()
            {
                White                     = WhiteSide.Name,
                Black                     = BlackSide.Name,
                GameType                  = Type,
                GameId                    = Id,
                Colour                    = colour,
                roundTimerSeconds         = RoundTimeSeconds,
                Phase                     = Phase,
                WhiteAvatar               = WhiteSide.Avatar,
                BlackAvatar               = BlackSide.Avatar,

                // TODO: implement this
                #region NYI
                /*WhiteIdolTypes = new PacketIdolTypes()
                {
                    ProfileId = 0,
                    Type = "DEFAULT",
                    Idol1 = 0,
                    Idol2 = 0,
                    Idol3 = 0,
                    Idol4 = 0,
                    Idol5 = 0,
                },
                BlackIdolTypes = new PacketIdolTypes()
                {
                    ProfileId = 1,
                    Type = "DEFAULT",
                    Idol1 = 0,
                    Idol2 = 0,
                    Idol3 = 0,
                    Idol4 = 0,
                    Idol5 = 0,
                },*/
                #endregion

                CustomSettings            = new List<string>(),
                RewardForIdolKill         = Battle.RewardForIdolKill,
                NodeId                    = ConfigManager.Config.Network.Host,
                Port                      = (uint)ConfigManager.Config.Network.BattlePort,
                MaxTierRewardMultiplier   = 0.5f,
                TierRewardMultiplierDelta = new List<float>(),
                WhiteIdols                = WhiteSide.Idols,
                BlackIdols                = BlackSide.Idols
            };

            return gameInfo;
        }

        /*public PacketGameState BuildGameState()
        {

        }*/
    }
}
