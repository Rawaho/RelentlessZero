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
using RelentlessZero.Managers;
using System.Collections.Generic;
using System.Net;

namespace RelentlessZero.Network.Handlers
{
    public static class BattleHandler
    {
        /* TODO :
         * - handle multiplayer. All this code is for single player only !
         */

        [PacketHandler("JoinBattle")]
        public static void HandleJoinBattle(object packet, Session session)
        {
            // TODO : handle avatars
            Battle battle;
            if (BattleManager.Battles.TryGetValue(session.Player.Id, out battle))
            {
                var gameInfo = new PacketGameInfo()
                {
                    White = battle.WhiteSide.PlayerName,
                    Black = battle.BlackSide.PlayerName,
                    GameType = battle.Type,
                    GameId = 1,
                    roundTimerSeconds = battle.RoundTimeSeconds,
                    Phase = battle.Phase,
                    WhiteAvatar = new PacketAvatar()
                    {
                        ProfileId = battle.WhiteSide.PlayerId,
                        Head = 15,
                        Body = 12,
                        Leg = 11,
                        ArmBack = 9,
                        ArmFront = 13
                    },
                    BlackAvatar = new PacketAvatar()
                    {
                        ProfileId = battle.BlackSide.PlayerId,
                        Head = 15,
                        Body = 12,
                        Leg = 11,
                        ArmBack = 9,
                        ArmFront = 13
                    },
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
        }

        [PacketHandler("LeaveGame")]
        public static void HandleLeaveGame(object packet, Session session)
        {
            BattleManager.Battles.Remove(session.Player.Id);
        }


        [PacketHandler("Surrender")]
        public static void HandleSurrender(object packet, Session session)
        {
            // TODO : send to both players
            Battle battle;
            if (BattleManager.Battles.TryGetValue(session.Player.Id, out battle))
            {
                BattleSide surrenderingSide = battle.FindSideByUsername(session.Player.Username);
                PlayerColor surrenderingColor = surrenderingSide.Color;

                var packetSurrenderEffect = new PacketNewEffects();
                packetSurrenderEffect.Effects.Add(new PacketSurrenderEffect()
                {
                    Color = surrenderingColor
                });
                session.Send(packetSurrenderEffect);

                var packetUpdateIdols = new PacketNewEffects();
                for (int i = 0; i < 5; ++i)
                {
                    surrenderingSide.Idols[i].Hp = 0;
                    packetUpdateIdols.Effects.Add(new PacketIdolUpdateEffect()
                    {
                        Idol = surrenderingSide.Idols[i]
                    });
                }
                packetUpdateIdols.Effects.Add(new PacketMulliganDisabledEffect()
                {
                    Color = surrenderingColor
                });
                session.Send(packetUpdateIdols);

                var packetEndGame = new PacketNewEffects();
                packetEndGame.Effects.Add(new PacketEndGameEffect()
                {
                    Winner = surrenderingSide.OpponentSide.Color,
                    WhiteStats = battle.WhiteSide.PlayerStats,
                    BlackStats = battle.BlackSide.PlayerStats,
                    WhiteGoldReward = battle.WhiteSide.GoldReward,
                    BlackGoldReward = battle.BlackSide.GoldReward
                });
                session.Send(packetEndGame);
            }
        }

    }
}
