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
using RelentlessZero.Managers;
using System.Collections.Generic;
using System.Net;

namespace RelentlessZero.Network.Handlers
{
    public static class BattleHandler
    {
        [PacketHandler("JoinBattle")]
        public static void HandleJoinBattle(object packet, Session session)
        {
            // TODO : handle better AI avatar
            Battle battle;
            if (BattleManager.Battles.TryGetValue(session.Player.Id, out battle) && WorldManager.AddPlayerSession(session))
            {
                BattleSide playerSide = battle.FindSideByUsername(session.Player.Username);
                Avatar playerAvatar = session.Player.Avatar;
                Avatar opponentAvatar;
                Session opponentSession = WorldManager.GetPlayerSession(playerSide.OpponentSide.PlayerName);
                if (opponentSession == null)
                    opponentAvatar = new Avatar();
                else
                    opponentAvatar = opponentSession.Player.Avatar;

                Avatar whiteAvatar;
                Avatar blackAvatar;
                if (playerSide.Color == PlayerColor.white)
                {
                    whiteAvatar = playerAvatar;
                    blackAvatar = opponentAvatar;
                }
                else
                {
                    whiteAvatar = opponentAvatar;
                    blackAvatar = playerAvatar;
                }

                var gameInfo = new PacketGameInfo()
                {
                    White = battle.WhiteSide.PlayerName,
                    Black = battle.BlackSide.PlayerName,
                    GameType = battle.Type,
                    GameId = 1,
                    Color = playerSide.Color,
                    roundTimerSeconds = battle.RoundTimeSeconds,
                    Phase = battle.Phase,
                    WhiteAvatar = whiteAvatar,
                    BlackAvatar = blackAvatar,
                    WhiteIdolTypes = new PacketIdolTypes()
                    {
                        ProfileId = battle.WhiteSide.PlayerId,
                        Type = "DEFAULT",
                        Idol1 = 0,
                        Idol2 = 0,
                        Idol3 = 0,
                        Idol4 = 0,
                        Idol5 = 0,
                    },
                    BlackIdolTypes = new PacketIdolTypes()
                    {
                        ProfileId = battle.BlackSide.PlayerId,
                        Type = "DEFAULT",
                        Idol1 = 0,
                        Idol2 = 0,
                        Idol3 = 0,
                        Idol4 = 0,
                        Idol5 = 0,
                    },
                    CustomSettings = new List<string>(),
                    RewardForIdolKill = battle.RewardForIdolKill,
                    NodeId = ((IPEndPoint)session.Socket.LocalEndPoint).Address.ToString(),
                    Port = (uint)ConfigManager.Config.Network.BattlePort,
                    MaxTierRewardMultiplier = 0.5f,
                    TierRewardMultiplierDelta = new List<float>(),
                    WhiteIdols = battle.WhiteSide.Idols,
                    BlackIdols = battle.BlackSide.Idols
                };

            
                session.Send(gameInfo);
            }
            else
            {
                LogManager.Write("JoinBattle", "player {0} failed to join battle ! Either battle not created or could not add player session!", session.Player.Username);
            }
        }

        [PacketHandler("LeaveGame")]
        public static void HandleLeaveGame(object packet, Session session)
        {
            Battle battle = null;
            BattleManager.Battles.TryRemove(session.Player.Id, out battle);
            if (battle == null)
                LogManager.Write("Battle", "could not remove client {0} battle from battle map !", session.Player.Username);
        }

        [PacketHandler("Surrender")]
        public static void HandleSurrender(object packet, Session session)
        {
            Battle battle;
            if (BattleManager.Battles.TryGetValue(session.Player.Id, out battle))
            {
                BattleSide surrenderingSide = battle.FindSideByUsername(session.Player.Username);
                BattleSide winningSide = surrenderingSide.OpponentSide;
                Session winningSession = WorldManager.GetPlayerSession(surrenderingSide.OpponentSide.PlayerName);
                PlayerColor surrenderingColor = surrenderingSide.Color;

                for (int i = 0; i < 5; ++i)
                {
                    surrenderingSide.Idols[i].Hp = 0;
                }

                Session[] sessionsToInform;
                if (winningSession != null) // no winning session if versus AI
                    sessionsToInform = new Session[2] { session, winningSession };
                else
                    sessionsToInform = new Session[1] { session };

                foreach (Session itSession in sessionsToInform)
                {
                    var packetSurrenderEffect = new PacketNewEffects();
                    packetSurrenderEffect.Effects.Add(new PacketSurrenderEffect()
                    {
                        Color = surrenderingColor
                    });
                    itSession.Send(packetSurrenderEffect);

                    var packetUpdateIdols = new PacketNewEffects();
                    for (int i = 0; i < 5; ++i)
                    {
                        packetUpdateIdols.Effects.Add(new PacketIdolUpdateEffect()
                        {
                            Idol = surrenderingSide.Idols[i]
                        });
                    }
                    packetUpdateIdols.Effects.Add(new PacketMulliganDisabledEffect()
                    {
                        Color = surrenderingColor
                    });
                    itSession.Send(packetUpdateIdols);

                    var packetEndGame = new PacketNewEffects();
                    packetEndGame.Effects.Add(new PacketEndGameEffect()
                    {
                        Winner = surrenderingSide.OpponentSide.Color,
                        WhiteStats = battle.WhiteSide.PlayerStats,
                        BlackStats = battle.BlackSide.PlayerStats,
                        WhiteGoldReward = battle.WhiteSide.GoldReward,
                        BlackGoldReward = battle.BlackSide.GoldReward
                    });
                    itSession.Send(packetEndGame);
                }
            }
        }

    }
}
