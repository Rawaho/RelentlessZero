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
using RelentlessZero.Logging;
using RelentlessZero.Managers;
using RelentlessZero.Network;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Linq;

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
        public ulong Scroll { get; }
        public byte SourceTileX { get; }
        public byte SourceTileY { get; }
        public byte DestinationTileX { get; }
        public byte DestinationTileY { get; }
        public ResourceType Resource { get; }
        public TileColour Colour { get; }

        public PendingMove(BattleMoveType moveType, ulong scroll, ResourceType resource, TileColour colour, byte srcTileX, byte srcTileY, byte dstTileX, byte dstTileY)
        {
            MoveType         = moveType;
            Resource         = resource;
            Colour           = colour;
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

        private const uint DefaultHandSize = 4u;

        public TileColour Colour { get; }

        // player ID and name, ID is always 0 for AI
        public uint Id { get; }
        public string Name { get; }

        public PlayerStats Stats { get; } = new PlayerStats();
        public Idol[] Idols { get; } = new Idol[IdolCount];
        public List<Unit> Board { get; } = new List<Unit>();
        public Avatar Avatar { get; set; }
        public bool IsAI { get; }

        public ConcurrentQueue<PendingMove> MoveQueue { get; } = new ConcurrentQueue<PendingMove>();

        // player has connected to the battle for the first time, always true for AI
        public bool InitialConnect { get; set; }
        // player is currently disconnected from the battle
        public bool Disconnected { get; set; } = true;
        public bool SentGameState { get; set; }

        public bool MulliganAvaliable { get; set; } = true;
        public bool SacrificeAvaliable { get; set; } = true;

        private Deck deck;
        private List<ScrollInstance> hand;
        private List<ScrollInstance> library;
        private List<ScrollInstance> graveyard;

        private short[] availableResources;
        private short[] outputResources;

        public BattleSide(uint id, string name, TileColour colour, bool isAI = false)
        {
            Id             = id;
            Name           = name;
            Colour         = colour;
            IsAI           = isAI;
            InitialConnect = isAI;
            SentGameState  = isAI;

            availableResources = new short[(byte)ResourceType.Count];
            outputResources    = new short[(byte)ResourceType.Count];

            // initialise idols with default health
            for (uint i = 0u; i < IdolCount; i++)
                Idols[i] = new Idol(colour, i);
        }

        public void SetDeck(Deck deck)
        {
            this.deck = deck;
            library   = new List<ScrollInstance>(deck.Scrolls.ToArray());
            hand      = new List<ScrollInstance>();
            graveyard = new List<ScrollInstance>();

            library.Shuffle();
            DrawCard(null, DefaultHandSize);
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

        public void SendActiveResources()
        {
            var activeResources = new PacketActiveResources
            {
                Resources = deck.Resources
            };

            WorldManager.Send(activeResources, Id);
        }

        // add a pending move that will be processed next battle update
        public void AddPendingMove(BattleMoveType moveType, ulong scroll = 0ul, ResourceType resource = ResourceType.NONE, TileColour colour = TileColour.unknown, byte srcTileX = 0, byte srcTileY = 0, byte dstTileX = 0, byte dstTileY = 0)
        {
            MoveQueue.Enqueue(new PendingMove(moveType, scroll, resource, colour, srcTileX, srcTileY, dstTileX, dstTileY));
        }

        // incrememnt or decrement resource value, specifying output will also update max resource value
        public void ModifyResource(ResourceType resource, short value, bool output)
        {
            if (resource >= ResourceType.Count)
                return;

            try
            {
                checked
                {
                    availableResources[(byte)resource] += value;
                    if (output)
                        outputResources[(byte)resource] += value;
                }
            }
            catch
            {
                LogManager.Write("Battle", "Overflow occured when modifying battle resource!");
                return;
            }
        }

        public ushort GetResource(ResourceType resource, bool output)
        {
            if (resource >= ResourceType.Count)
                return 0;

            return (ushort)(output ? outputResources : availableResources)[(byte)resource];
        }

        private bool HasEnoughResource(ResourceType resource, ushort value, out ushort resourceDeficit)
        {
            resourceDeficit = 0;

            ushort resourceBalance = GetResource(resource, false);
            if (resourceBalance < value)
                resourceDeficit = (ushort)(value - resourceBalance);

            return resourceDeficit == 0;
        }

        public bool HasEnoughResource(ScrollTemplate scroll)
        {
            ushort resourceDeficit;
            if (!HasEnoughResource(scroll.Resource, scroll.Cost, out resourceDeficit))
            {
                // check if player has enough wild to cover main resource deficit
                ushort wildDeficit;
                return HasEnoughResource(ResourceType.SPECIAL, resourceDeficit, out wildDeficit);
            }

            return true;
        }

        public bool IsActiveResource(ResourceType resource) { return deck.HasResource(resource); }
        private void RestoreResources() { outputResources.CopyTo(availableResources, 0); }

        private PacketResources BuildResources(bool output)
        {
            var packetResources = new PacketResources
            {
                Decay  = GetResource(ResourceType.DECAY, output),
                Energy = GetResource(ResourceType.ENERGY, output),
                Growth = GetResource(ResourceType.GROWTH, output),
                Order  = GetResource(ResourceType.ORDER, output),
                Wild   = GetResource(ResourceType.SPECIAL, output),
            };

            return packetResources;
        }

        public void StartRound(PacketEffectWriter newEffects)
        {
            SacrificeAvaliable = true;

            RestoreResources();
            DrawCard(newEffects);
        }

        public ScrollInstance GetScrollFromHand(ulong id) { return hand.SingleOrDefault(scroll => scroll.Id == id); }

        public void RemoveScrollFromHand(ScrollInstance scroll, PacketEffectWriter newEffects)
        {
            if (!hand.Remove(scroll))
                return;

            graveyard.Add(scroll);

            if (newEffects != null)
                HandUpdate(newEffects);
        }

        public void DamageIdol(uint idol, uint damage, PacketEffectWriter newEffects)
        {
            if (idol >= IdolCount)
                return;

            // make sure damage doesn't exceed current idol health
            if (Idols[idol].Hp < damage)
                damage = Idols[idol].Hp;

            Idols[idol].Hp -= damage;
            newEffects.AddEffect(new PacketIdolUpdateEffect(Idols[idol]));
        }

        public void HandUpdate(PacketEffectWriter newEffects)
        {
            var handUpdate = new PacketHandUpdateEffect
            {
                ProfileId      = Id,
                SacrificeLimit = 7u,        // TODO
                Scrolls        = hand
            };

            newEffects.AddEffect(handUpdate, Id);

            // client doesn't update library or graveyard with only a ResourcesUpdate effect, this must be sent as well
            newEffects.AddEffect(new PacketCardStackUpdateEffect(Colour, library.Count, graveyard.Count));
        }

        public void DrawCard(PacketEffectWriter newEffects, uint count = 1u)
        {
            for (uint i = 0u; i < count; i++)
            {
                if (library.Count == 0)
                {
                    // no cards available, shuffle graveyard back into the library
                    library.InsertRange(0, graveyard.ToArray());
                    library.Shuffle();
                    graveyard.Clear();
                }

                // check size again, certain cases where the player has no graveyard or library
                if (library.Count != 0)
                {
                    hand.Add(library.Pop());
                    Stats.ScrollsDrawn++;
                }
            }

            if (newEffects != null)
                HandUpdate(newEffects);
        }

        public void SacrificeScroll(PacketEffectWriter newEffects, ulong scroll, ResourceType resource)
        {
            if (!SacrificeAvaliable)
                return;

            var scrollInstance = GetScrollFromHand(scroll);
            if (scrollInstance == null)
                return;

            SacrificeAvaliable = false;

            if (resource == ResourceType.CARDS)
                // HandUpdate and CardStackUpdate effect will be added on scroll removal from hand
                DrawCard(null, 2);
            else
                ModifyResource(resource, 1, true);

            RemoveScrollFromHand(scrollInstance, newEffects);
            RemoveMulligan(newEffects);

            newEffects.AddEffect(new PacketCardSacrificedEffect(Colour, resource));
        }

        public void RemoveMulligan(PacketEffectWriter newEffects)
        {
            if (!MulliganAvaliable)
                return;

            MulliganAvaliable = false;
            newEffects.AddEffect(new PacketMulliganDisabledEffect(Colour), Id);
        }

        public void Mulligan()
        {
            if (!MulliganAvaliable)
                return;

            MulliganAvaliable = false;

            // should only ever be 5 or 6, have explicit check for hand size?
            uint scrollCount = (uint)hand.Count;

            library.InsertRange(0, hand);
            library.Shuffle();
            hand.Clear();

            var newEffects = new PacketEffectWriter(Id);
            DrawCard(newEffects, scrollCount);

            newEffects.AddEffect(new PacketMulliganDisabledEffect(Colour));
            newEffects.Send();
        }

        public bool SummonUnit(ScrollInstance scrollInstance, byte positionX, byte positionY, PacketEffectWriter newEffects = null)
        {
            Contract.Requires<ArgumentNullException>(scrollInstance != null);
            Contract.Requires<ArgumentOutOfRangeException>(positionX < BoardSearcher.BoardWidth && positionY < BoardSearcher.BoardLength);

            if (BoardSearcher.IsTileOccupied(Board, positionX, positionY))
                return false;

            var child = AssetManager.GetUnitChild(scrollInstance.Scroll.Entry);
            var unit  = (child != null ? (Unit)Activator.CreateInstance(child) : new Unit(this, scrollInstance, positionX, positionY, newEffects));
            unit.OnSummon();

            Board.Add(unit);

            // add after OnSummon as stats might be changed in override
            if (newEffects != null)
                unit.StatsUpdate(newEffects);

            return true;
        }

        public PacketGameState.SideGameState BuildGameState()
        {
            var sideGameState = new PacketGameState.SideGameState
            {
                Name  = Name,
                Board = new PacketGameState.SideBoard
                {
                    Colour = Colour,
                    Tiles  = new string[0],                             // TODO
                    Idols  = new uint[5]
                },

                Mulligan = MulliganAvaliable,
                Assets   = BuildAssets()
            };

            for (uint i = 0u; i < IdolCount; i++)
                sideGameState.Board.Idols[i] = Idols[i].Hp;

            return sideGameState;
        }

        public PacketSideAssets BuildAssets()
        {
            var sideAssets = new PacketSideAssets
            {
                AvaliableResources = BuildResources(false),
                OutputResources    = BuildResources(true),
                RuleUpdates        = new string[0],                     // TODO
                HandSize           = hand.Count,
                LibrarySize        = library.Count,
                GraveyardSize      = graveyard.Count
            };

            return sideAssets;
        }
    }

    // ----------------------------------------------------------------

    public class Battle
    {
        public const uint SideCount         = 2u;
        public const uint RewardForIdolKill = 10u;

        private const uint DefaultHandSize  = 4u;

        private uint sideId;

        public uint Id { get; }
        public BattleType Type { get; }
        public AiDifficulty Difficulty { get; }
        public BattlePhase Phase { get; set; } = BattlePhase.Init;

        public uint RoundTimer { get; set; }            // time remaining in current turn (in milliseconds)
        public int RoundTimeSeconds { get; }            // total time of a single turn (-1 for unlimited)
        public TileColour CurrentTurn { get; set; }
        public uint CurrentRound { get; set; }

        public BattleSide[] Side { get; } = new BattleSide[SideCount];
        public BattleSide BlackSide { get; set; }
        public BattleSide WhiteSide { get; set; }

        public bool Expired { get; set; }
        public bool SentGameInfo { get; set; }

        // used when waiting for players to initially connect
        public uint StartAttemptTimer { get; set; }
        public byte StartAttemptCount { get; set; }

        public Battle(uint id, BattleType type, AiDifficulty difficulty)
        {
            Id                = id;
            Type              = type;
            Difficulty        = difficulty;
            RoundTimeSeconds  = type == BattleType.SP_QUICKMATCH ? -1 : 30;     // TODO: move turn timer to config file
            CurrentTurn       = Helper.RandomColour();
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
            return colour == TileColour.black ? (opponent == true ? WhiteSide : BlackSide) : (opponent == true ? BlackSide : WhiteSide);
        }

        private bool FirstRound() { return CurrentRound == 1; }

        public void StartRound()
        {
            CurrentRound++;

            if (Type != BattleType.SP_QUICKMATCH)
                RoundTimer = (uint)TimeSpan.FromSeconds(RoundTimeSeconds).TotalMilliseconds;

            if (FirstRound())
            {
                foreach (var side in Side)
                    side.SendActiveResources();
            }
            else
                CurrentTurn = GetSide(CurrentTurn, true).Colour;

            Phase = GetSide(CurrentTurn).IsAI ? BattlePhase.Main : BattlePhase.PreMain;

            // TurnBeginEffect makes client request new round, StartRoundPlayer will be called on request to continue round start
            var newEffects = new PacketEffectWriter(BlackSide.Id, WhiteSide.Id);
            newEffects.AddEffect(new PacketTurnBeginEffect(CurrentTurn, CurrentRound, RoundTimeSeconds));
            newEffects.Send();
        }

        public void StartRoundPlayer()
        {
            Phase = BattlePhase.Main;

            var newEffects = new PacketEffectWriter(BlackSide.Id, WhiteSide.Id);
            GetSide(CurrentTurn).StartRound(newEffects);
            GetSide(CurrentTurn, true).HandUpdate(newEffects);

            // must update resources last
            ResourceUpdate(newEffects);

            newEffects.Send();
        }

        public void EndRound()
        {
            var currentSide = GetSide(CurrentTurn);
            var newEffects  = new PacketEffectWriter(BlackSide.Id, WhiteSide.Id);

            currentSide.RemoveMulligan(newEffects);

            // TODO: update units, spells, enchantments ect...
            newEffects.Send();

            StartRound();
        }

        public void EndGame(TileColour winningColour, bool surrender = false)
        {
            if (Phase == BattlePhase.End)
                return;

            Phase = BattlePhase.End;

            var winningSide = GetSide(winningColour);
            var losingSide  = GetSide(winningColour, true);

            var newEffects = new PacketEffectWriter(BlackSide.Id, WhiteSide.Id);
            if (surrender)
            {
                newEffects.AddEffect(new PacketSurrenderEffect(losingSide.Colour));

                // destroy remaining idols on losing side
                for (uint i = 0; i < BattleSide.IdolCount; i++)
                    losingSide.DamageIdol(i, Idol.FullHp, newEffects);
            }

            // TODO: calculate rewards

            var endGameEffect = new PacketEndGameEffect()
            {
                Winner          = winningSide.Colour,
                BlackStats      = BlackSide.Stats,
                WhiteStats      = WhiteSide.Stats,
                BlackGoldReward = new GoldReward(),
                WhiteGoldReward = new GoldReward()
            };

            newEffects.AddEffect(endGameEffect);
            newEffects.Send();
        }

        public void LeaveGame(TileColour colour)
        {
            // TODO: broadcast left battle message to opponent
            GetSide(colour).LeaveGame(Phase == BattlePhase.End);
        }

        public void CardInfo(TileColour colour, ulong scroll)
        {
            if (CurrentTurn != colour)
                return;

            var side = GetSide(colour);

            var scrollInstance = side.GetScrollFromHand(scroll);
            if (scrollInstance == null)
                return;

            // use board searcher to find all tiles that the scroll can be played on
            var boardTiles = BoardSearcher.Search(scrollInstance, new BoardSearcherSide(side), new BoardSearcherSide(GetSide(colour, true)));

            var cardInfo = new PacketCardInfo()
            {
                Data = new PacketCardInfo.CardInfoData
                {
                    TargetArea      = scrollInstance.Scroll.TargetArea,
                    SelectableTiles = new PacketCardInfo.SelectableTiles
                    {
                        // client expects board tiles list inside a list, there a reason for this?
                        TileSets = new List<List<BoardSearcherTile>>() { boardTiles }
                    }
                },

                Scroll    = scrollInstance,
                Resources = side.HasEnoughResource(scrollInstance.Scroll)
            };

            WorldManager.Send(cardInfo, side.Id);
        }

        public void PlayScroll(TileColour colour, ulong scroll, byte positionX, byte positionY)
        {
            Contract.Requires<ArgumentOutOfRangeException>(colour != TileColour.unknown);
            Contract.Requires<ArgumentOutOfRangeException>(scroll != 0);
            Contract.Requires<ArgumentOutOfRangeException>(positionX < BoardSearcher.BoardWidth && positionY < BoardSearcher.BoardLength);

            if (CurrentTurn != colour)
                return;

            var side = GetSide(colour);
            Contract.Assert(side != null);

            var scrollInstance = side.GetScrollFromHand(scroll);
            if (scrollInstance == null)
                return;

            var newEffects = new PacketEffectWriter(BlackSide.Id, WhiteSide.Id);
            if (side.SummonUnit(scrollInstance, positionX, positionY, newEffects))
            {
                side.RemoveScrollFromHand(scrollInstance, newEffects);
                side.ModifyResource(scrollInstance.Scroll.Resource, (short)(-scrollInstance.Scroll.Cost), false);
                ResourceUpdate(newEffects);

                newEffects.AddEffect(new PacketCardPlayedEffect(colour, scrollInstance));
            }

            newEffects.Send();
        }

        public void ResourceUpdate(PacketEffectWriter newEffects) { newEffects.AddEffect(new PacketResourcesUpdateEffect(BlackSide.BuildAssets(), WhiteSide.BuildAssets())); }

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

        public PacketGameState BuildGameState()
        {
            var gameState = new PacketGameState()
            {
                BlackState   = BlackSide.BuildGameState(),
                WhiteState   = BlackSide.BuildGameState(),
                ActiveColour = CurrentTurn,
                Phase        = Phase,
                Turn         = CurrentRound,
                Sacrificed   = false,
                SecondsLeft  = Type == BattleType.SP_QUICKMATCH ? -1 : (int)TimeSpan.FromMilliseconds(RoundTimer).TotalSeconds,
            };

            return gameState;
        }
    }
}
