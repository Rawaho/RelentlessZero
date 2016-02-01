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

using RelentlessZero.Logging;
using RelentlessZero.Managers;
using RelentlessZero.Entities;
using System;

namespace RelentlessZero.Network.Handlers
{
    public static class LobbyHandler
    {
        [PacketHandler("GameChallengeAccept")]
        public static void HandleGameChallengeAccept(object packet, Session session)
        {
            var gameChallangeAccept = (PacketGameChallengeAccept)packet;

            var battleInvite = BattleManager.GetPendingChallange(session.Player.Id, gameChallangeAccept.ProfileId);
            if (battleInvite == null)
            {
                LogManager.Write("Player", "Player {0} tried to accept challange from player {1}, no pending invite exists!",
                    session.Player.Id, gameChallangeAccept.ProfileId);
                return;
            }

            if (battleInvite.Expired)
            {
                LogManager.Write("Player", "Player {0} tried to accept challange from player {1}, but invite has already expired!",
                    session.Player.Id, gameChallangeAccept.ProfileId);
                return;
            }

            var battleInfo = BattleManager.GetBattleInfo(session.Player.Id);
            if (battleInfo != null)
            {
                LogManager.Write("Player", "Player {0} tried to accept challange from player {1}, but they are already in an active battle!",
                    session.Player.Id, gameChallangeAccept.ProfileId);
                return;
            }

            var challanger = WorldManager.GetPlayerSession(gameChallangeAccept.ProfileId);
            if (challanger == null)
            {
                LogManager.Write("Player", "Player {0} tried to accept challange from player {1}, but challanger is offline!",
                    session.Player.Id, gameChallangeAccept.ProfileId);
                return;
            }

            // this should only fail if the challanger deletes their deck before the opponent accepts
            var challangerDeck = challanger.Player.GetDeck(battleInvite.ChallangerDeck);
            if (challangerDeck == null)
            {
                LogManager.Write("Player", "Player {0} tried to accept challange from player {1}, but challanger has non existant deck {2}!",
                    session.Player.Id, gameChallangeAccept.ProfileId, challangerDeck.Name);
                return;
            }

            var deck = session.Player.GetDeck(gameChallangeAccept.Deck);
            if (deck == null)
            {
                LogManager.Write("Player", "Player {0} tried to accept challange from player {1} but has non existant deck {2}!",
                    session.Player.Id, gameChallangeAccept.ProfileId, gameChallangeAccept.Deck);
                return;
            }

            BattleManager.CreateBattle(BattleType.MP_UNRANKED, challanger.Player, challangerDeck, session.Player, deck);
            BattleManager.RemoveChallange(gameChallangeAccept.ProfileId);
        }

        [PacketHandler("GameChallengeDecline")]
        public static void HandleGameChallengeDecline(object packet, Session session)
        {
            var gameChallengeDecline = (PacketGameChallengeDecline)packet;

            var battleInvite = BattleManager.GetPendingChallange(session.Player.Id, gameChallengeDecline.ProfileId);
            if (battleInvite == null)
            {
                LogManager.Write("Player", "Player {0} tried to decline challange from player {1}, no pending invite exists!",
                    session.Player.Id, gameChallengeDecline.ProfileId);
                return;
            }

            if (battleInvite.Expired)
            {
                LogManager.Write("Player", "Player {0} tried to decline challange from player {1}, but invite has already expired!",
                    session.Player.Id, gameChallengeDecline.ProfileId);
                return;
            }

            BattleManager.DeclineChallange(gameChallengeDecline.ProfileId, battleInvite, true);
        }

        [PacketHandler("GameChallengeRequest")]
        public static void HandleGameChallengeRequest(object packet, Session session)
        {
            var gameChallengeRequest = (PacketGameChallengeRequest)packet;

            if (gameChallengeRequest.ProfileId == session.Player.Id)
            {
                LogManager.Write("Player", "Player {0} tried to challange themself to a battle!",
                    session.Player.Id, gameChallengeRequest.ProfileId);
                return;
            }

            var deck = session.Player.GetDeck(gameChallengeRequest.Deck);
            if (deck == null)
            {
                LogManager.Write("Player", "Player {0} tried to challange another player with non existant deck {1}!",
                    session.Player.Id, gameChallengeRequest.Deck);
                return;
            }

            var opponentSession = WorldManager.GetPlayerSession(gameChallengeRequest.ProfileId);
            if (opponentSession == null)
            {
                LogManager.Write("Player", "Player {0} tried to challange non existant player {1} to a battle!",
                    session.Player.Id, gameChallengeRequest.ProfileId);
                return;
            }

            if (BattleManager.GetPendingChallange(opponentSession.Player.Id, session.Player.Id) != null)
            {
                LogManager.Write("Player", $"Player {session.Player.Id} tried to challange a player that already has pending invite!");
                return;
            }

            BattleManager.ChallangePlayer(session.Player, deck, opponentSession.Player);
            session.SendOkPacket("GameChallengeRequest");
        }
        
        [PacketHandler("PlaySinglePlayerQuickMatch")]
        public static void HandlePlaySinglePlayerQuickMatch(object packet, Session session)
        {
            var playSinglePlayerQuickMatch = (PacketPlaySinglePlayerQuickMatch)packet;

            var deck = session.Player.GetDeck(playSinglePlayerQuickMatch.Deck);
            if (deck == null)
            {
                LogManager.Write("Player", "Player {0} tried to enter battle with non existent deck {1}!",
                    session.Player.Id, playSinglePlayerQuickMatch.Deck);
                return;
            }

            var battleInfo = BattleManager.GetBattleInfo(session.Player.Id);
            if (battleInfo != null)
            {
                LogManager.Write("Player", "Player {0} tried to enter battle but they are already in an active battle!",
                    session.Player.Id);
                return;
            }

            // convert AI name sent by client to difficulty
            string difficultyString = playSinglePlayerQuickMatch.RobotName.Remove(0, 5).ToUpper();
            if (!Enum.IsDefined(typeof(AiDifficulty), difficultyString))
            {
                LogManager.Write("Player", "Player {0} tried to enter battle with invalid difficulty {1}!",
                    session.Player.Id, difficultyString);
                return;
            }

            var difficulty = (AiDifficulty)Enum.Parse(typeof(AiDifficulty), difficultyString);
            if (difficulty == AiDifficulty.TUTORIAL)
            {
                LogManager.Write("Player", "Player {0} tried to enter a tutorial battle via quickmatch!",
                    session.Player.Id, difficultyString);
                return;
            }

            BattleManager.CreateBattle(BattleType.SP_QUICKMATCH, session.Player, deck, null, null, difficulty);
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
