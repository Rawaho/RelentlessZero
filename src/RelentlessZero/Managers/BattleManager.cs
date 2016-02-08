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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RelentlessZero.Managers
{
    public class BattleInvite
    {
        public string ChallangerDeck { get; }
        public uint Opponent { get; }
        public uint TimeSinceInvite { get; set; }
        public bool Expired { get; set; }

        // cache incase either player is offline when invite is declined or accepted
        public PacketProfile ChallangerProfile { get; }
        public PacketProfile OpponentProfile { get; }

        public BattleInvite(Player challanger, string challangerDeck, Player opponent)
        {
            ChallangerDeck    = challangerDeck;
            Opponent          = opponent.Id;
            ChallangerProfile = challanger.GeneratePacketProfile();
            OpponentProfile   = opponent.GeneratePacketProfile();
        }
    }

    public class BattleInfo
    {
        public uint BattleId { get; }
        public TileColour SideColour { get; }

        public BattleInfo(uint battleId, TileColour sideColour)
        {
            BattleId   = battleId;
            SideColour = sideColour;
        }
    }

    public static class BattleManager
    {
        private const uint BattleInviteTimeout          = 20000u;
        private const uint BattleStartMaxTimePerAttempt = 1000u;
        private const byte BattleStartMaxAttempts       = 10;

        private static uint battleId;
        private static object battleIdLock;

        private static ConcurrentDictionary<uint, BattleInvite> inviteStore;
        private static ConcurrentDictionary<uint, Battle> battleStore;
        private static ConcurrentDictionary<uint, BattleInfo> battleInfoStore;

        static BattleManager()
        {
            inviteStore     = new ConcurrentDictionary<uint, BattleInvite>();
            battleStore     = new ConcurrentDictionary<uint, Battle>();
            battleInfoStore = new ConcurrentDictionary<uint, BattleInfo>();

            battleId        = 1u;
            battleIdLock    = new object();
        }

        public static void Update(uint diff)
        {
            // update pending invites
            var invitesToRemove = new List<uint>();
            foreach (var battleInvite in inviteStore)
            {
                var pendingInvite = battleInvite.Value;

                pendingInvite.TimeSinceInvite += diff;
                if (pendingInvite.TimeSinceInvite < BattleInviteTimeout)
                    continue;

                // battle invite has exceded 20 second threshold, decline
                DeclineChallange(battleInvite.Key, battleInvite.Value);
                invitesToRemove.Add(battleInvite.Key);
            }

            foreach (var challanger in invitesToRemove)
                RemoveChallange(challanger);

            // update battles in progress
            var battlesToRemove = new List<uint>();
            foreach (var battle in battleStore)
            {
                var currentBattle = battle.Value;
                switch (currentBattle.Phase)
                {
                    // start any pending battles
                    case BattlePhase.Init:
                    {
                        currentBattle.StartAttemptTimer += diff;
                        if (currentBattle.StartAttemptTimer < BattleStartMaxTimePerAttempt)
                            continue;

                        currentBattle.StartAttemptTimer = 0u;
                        currentBattle.StartAttemptCount++;

                        if (currentBattle.StartAttemptCount >= BattleStartMaxAttempts)
                        {
                            // one or both players have failed to connect
                            currentBattle.Expired = true;

                            foreach (var battleSide in currentBattle.Side)
                            {
                                if (battleSide.IsAI)
                                    continue;

                                var session = WorldManager.GetPlayerSession(battleSide.Id);
                                if (session != null)
                                    session.SendFailPacket("JoinBattle", "\nOpponent failed to connect to battle session!");
                            }

                            battlesToRemove.Add(currentBattle.Id);
                        }
                        else if (currentBattle.BlackSide.InitialConnect && currentBattle.WhiteSide.InitialConnect)
                        {
                            if (!currentBattle.SentGameInfo)
                            {
                                // player(s) have connected, send initial game information
                                foreach (var battleSide in currentBattle.Side)
                                    if (!battleSide.IsAI)
                                        WorldManager.Send(currentBattle.BuildGameInfo(battleSide.Colour), battleSide.Id);

                                currentBattle.SentGameInfo = true;
                            }
                            else
                            {
                                if (currentBattle.BlackSide.SentGameState && currentBattle.WhiteSide.SentGameState)
                                {
                                    // both players have requested game state information, start battle
                                    currentBattle.StartRound();
                                }
                            }
                        }
                        break;
                    }
                    //case BattlePhase.PreMain:
                    case BattlePhase.Main:
                    {
                        currentBattle.GetSide(currentBattle.CurrentTurn).Stats.TotalMs += diff;

                        // update battle round timer
                        if (currentBattle.Type != BattleType.SP_QUICKMATCH)
                        {
                            if (currentBattle.RoundTimer <= diff)
                                currentBattle.EndRound();
                            else
                                currentBattle.RoundTimer -= diff;
                        }
                        else
                        {
                            // AI currently just ends its turn
                            if (currentBattle.GetSide(currentBattle.CurrentTurn).IsAI)
                                currentBattle.EndRound();
                        }
                        break;
                    }
                    default:
                        break;
                }
            }

            // remove obsolete battles before processing pending battle moves
            foreach (uint battleId in battlesToRemove)
                RemoveBattle(battleId);

            HandlePendingMoves();
        }

        private static void HandlePendingMoves()
        {
            foreach (var battle in battleStore)
            {
                var currentBattle = battle.Value;
                foreach (var battleSide in currentBattle.Side)
                {
                    PendingMove pendingMove;
                    if (!battleSide.MoveQueue.TryDequeue(out pendingMove))
                        continue;

                    // only GameState is processed for battles still in the initialisation phase
                    if (currentBattle.Phase == BattlePhase.Init && pendingMove.MoveType != BattleMoveType.GameState)
                        continue;

                    switch (pendingMove.MoveType)
                    {
                        case BattleMoveType.Surrender:
                            currentBattle.EndGame(currentBattle.GetSide(battleSide.Colour, true).Colour, true);
                            break;
                        case BattleMoveType.StartRound:
                            currentBattle.PlayerStartRound();
                            break;
                        case BattleMoveType.EndRound:
                            currentBattle.EndRound();
                            break;
                        case BattleMoveType.LeaveGame:
                            currentBattle.LeaveGame(battleSide.Colour);
                            break;
                        case BattleMoveType.GameState:
                        {
                            WorldManager.Send(currentBattle.BuildGameState(), battleSide.Id);

                            if (currentBattle.Phase == BattlePhase.Init)
                                battleSide.SentGameState = true;

                            break;
                        }
                        default:
                            break;
                    }
                }
            }
        }

        public static void ChallangePlayer(Player challanger, Deck challanerDeck, Player opponent)
        {
            if (!inviteStore.TryAdd(challanger.Id, new BattleInvite(challanger, challanerDeck.Name, opponent)))
                return;

            var packetGameChallenge = new PacketGameChallenge()
            {
                From            = challanger.GeneratePacketProfile(),
                ParentalConsent = false
            };

            opponent.Session.Send(packetGameChallenge);
        }

        public static void DeclineChallange(uint challanger, BattleInvite battleInvite, bool remove = false)
        {
            battleInvite.Expired = true;

            var gameChallengeResponse = new PacketGameChallengeResponse()
            {
                From   = battleInvite.ChallangerProfile,
                To     = battleInvite.OpponentProfile,
                Status = "DECLINE"
            };

            WorldManager.Send(gameChallengeResponse, challanger, battleInvite.Opponent);

            if (remove)
                RemoveChallange(challanger);
        }

        public static void RemoveChallange(uint challanger)
        {
            BattleInvite removedBattleInvite;
            inviteStore.TryRemove(challanger, out removedBattleInvite);
        }

        public static BattleInvite GetPendingChallange(uint opponent, uint challanger)
        {
            return inviteStore.SingleOrDefault(battleInvite => battleInvite.Key == challanger 
                && battleInvite.Value.Opponent == opponent).Value;
        }

        private static uint GetNewBattleId()
        {
            lock (battleIdLock)
                return battleId++;
        }

        public static BattleInfo GetBattleInfo(uint id) { return battleInfoStore.SingleOrDefault(battleInfo => battleInfo.Key == id).Value; }

        public static void RemoveBattleInfo(uint id)
        {
            BattleInfo removedBattleInfo;
            battleInfoStore.TryRemove(id, out removedBattleInfo);
        }

        public static Battle GetBattle(uint battleId) { return battleStore.SingleOrDefault(battle => battle.Key == battleId).Value; }

        public static void RemoveBattle(uint battleId)
        {
            Battle removedBattle;
            if (!battleStore.TryRemove(battleId, out removedBattle))
                return;

            foreach (var battleSide in removedBattle.Side)
            {
                if (battleSide == null)
                    continue;

                if (battleSide.IsAI)
                    continue;

                // redirect player to lobby if still in battle during removal
                if (!battleSide.Disconnected)
                {
                    var session = WorldManager.GetPlayerSession(battleSide.Id);
                    if (session != null)
                        session.SendFailPacket("JoinBattle", "\nBattle session has closed.");
                }

                RemoveBattleInfo(battleSide.Id);
            }
        }

        private static Dictionary<AiDifficulty, string> difficultyName = new Dictionary<AiDifficulty, string>
        {
            { AiDifficulty.EASY,     "Easy AI"   },
            { AiDifficulty.MEDIUM,   "Medium AI" },
            { AiDifficulty.HARD,     "Easy AI"   },
            { AiDifficulty.TUTORIAL, "Grimbauld" }
        };

        public static void CreateBattle(BattleType battleType, Player challanger, Deck challangerDeck, Player opponent, Deck opponentDeck, AiDifficulty difficulty = AiDifficulty.EASY)
        {
            uint newBattleId = GetNewBattleId();

            var battle = new Battle(newBattleId, battleType, difficulty);

            // first side always contains a player (challanger)
            var challangerSide = new BattleSide(challanger.Id, challanger.Username, Helper.RandomColour());
            challangerSide.Avatar = challanger.Avatar;
            challangerSide.SetDeck(challangerDeck);

            if (!battleInfoStore.TryAdd(challanger.Id, new BattleInfo(newBattleId, challangerSide.Colour)))
                return;

            battle.AddSide(challangerSide);

            // second side contains either another player or AI (opponent)
            BattleSide opponentSide;
            TileColour opponentColour = challangerSide.Colour == TileColour.black ? TileColour.white : TileColour.black;

            if (battleType == BattleType.SP_QUICKMATCH)
            {
                string botName = difficultyName[difficulty];

                opponentSide = new BattleSide(0, botName, opponentColour, true);
                opponentSide.Avatar = new Avatar();
                opponentSide.Avatar.SetRandom();

                // TODO: handle AI deck
                opponentSide.SetDeck(challangerDeck);
            }
            else
            {
                opponentSide = new BattleSide(opponent.Id, opponent.Username, opponentColour);
                opponentSide.Avatar = opponent.Avatar;
                opponentSide.SetDeck(opponentDeck);

                if (!battleInfoStore.TryAdd(opponent.Id, new BattleInfo(newBattleId, opponentColour)))
                    return;
            }

            battle.AddSide(opponentSide);

            if (!battleStore.TryAdd(newBattleId, battle))
                return;

            // redirect player(s) to battle server
            var battleRedirect = new PacketBattleRedirect()
            {
                IP   = ConfigManager.Config.Network.Host,
                Port = (uint)ConfigManager.Config.Network.BattlePort
            };

            challanger.Session.Send(battleRedirect);

            if (battleType != BattleType.SP_QUICKMATCH)
                opponent.Session.Send(battleRedirect);
        }
    }
}
