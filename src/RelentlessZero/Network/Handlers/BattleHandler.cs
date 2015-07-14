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
using System.Collections.Generic;
using System.Net;

namespace RelentlessZero.Network.Handlers
{
    public static class BattleHandler
    {
        /* TODO :
         * - store battle data in database, and fill the packet structures
         * with relevant data instead of dummies. 
         * - handle multiplayer. All this code is for single player only !
         */

        [PacketHandler("JoinBattle")]
        public static void HandleJoinBattle(object packet, Session session)
        {
            var gameInfo = new PacketGameInfo()
            {
                White = session.Player.Username,
                Black = "Easy AI",
                GameType = BattleType.SP_QUICKMATCH,
                GameId = 0,
                roundTimerSeconds = -1,
                Phase = BattlePhase.Init,
                WhiteAvatar = new PacketAvatar()
                {
                    ProfileId = session.Player.Id,
                    Head = 15,
                    Body = 12,
                    Leg = 11,
                    ArmBack = 9,
                    ArmFront = 13
                },
                BlackAvatar = new PacketAvatar()
                {
                    ProfileId = 1337,
                    Head = 15,
                    Body = 12,
                    Leg = 11,
                    ArmBack = 9,
                    ArmFront = 13
                },
                WhiteIdolTypes = new PacketIdolTypes()
                {
                    ProfileId = session.Player.Id,
                    Type = "DEFAULT",
                    Idol1 = 0,
                    Idol2 = 0,
                    Idol3 = 0,
                    Idol4 = 0,
                    Idol5 = 0,
                },
                BlackIdolTypes = new PacketIdolTypes()
                {
                    ProfileId = 1337,
                    Type = "DEFAULT",
                    Idol1 = 0,
                    Idol2 = 0,
                    Idol3 = 0,
                    Idol4 = 0,
                    Idol5 = 0,
                },
                CustomSettings = new List<string>(),
                RewardForIdolKill = 10,
                NodeId = ((IPEndPoint)session.Socket.LocalEndPoint).Address.ToString(),
                Port = (uint)ConfigManager.Config.Network.BattlePort,
                WhiteIdols = new List<PacketIdol>(),
                BlackIdols = new List<PacketIdol>(),
                MaxTierRewardMultiplier = 0.5f,
                TierRewardMultiplierDelta = new List<float>()
            };

            gameInfo.WhiteIdols.Add(new PacketIdol() { Color = PlayerColor.white, position = 0, Hp = 10, MaxHp = 10 });
            gameInfo.WhiteIdols.Add(new PacketIdol() { Color = PlayerColor.white, position = 1, Hp = 10, MaxHp = 10 });
            gameInfo.WhiteIdols.Add(new PacketIdol() { Color = PlayerColor.white, position = 2, Hp = 10, MaxHp = 10 });
            gameInfo.WhiteIdols.Add(new PacketIdol() { Color = PlayerColor.white, position = 3, Hp = 10, MaxHp = 10 });
            gameInfo.WhiteIdols.Add(new PacketIdol() { Color = PlayerColor.white, position = 4, Hp = 10, MaxHp = 10 });

            gameInfo.BlackIdols.Add(new PacketIdol() { Color = PlayerColor.black, position = 0, Hp = 10, MaxHp = 10 });
            gameInfo.BlackIdols.Add(new PacketIdol() { Color = PlayerColor.black, position = 1, Hp = 10, MaxHp = 10 });
            gameInfo.BlackIdols.Add(new PacketIdol() { Color = PlayerColor.black, position = 2, Hp = 10, MaxHp = 10 });
            gameInfo.BlackIdols.Add(new PacketIdol() { Color = PlayerColor.black, position = 3, Hp = 10, MaxHp = 10 });
            gameInfo.BlackIdols.Add(new PacketIdol() { Color = PlayerColor.black, position = 4, Hp = 10, MaxHp = 10 });

            
            session.Send(gameInfo);
        }

        [PacketHandler("LeaveGame")]
        public static void HandleLeaveGame(object packet, Session session)
        {
            // TODO : something ?
        }


        [PacketHandler("Surrender")]
        public static void HandleSurrender(object packet, Session session)
        {
            PlayerColor surrenderingColor = PlayerColor.white;
            
            var packetSurrenderEffect = new PacketNewEffects();
            packetSurrenderEffect.AddEffect(new PacketSurrenderEffect()
             {
                 Color = surrenderingColor
             });
            session.Send(packetSurrenderEffect);

            var packetUpdateIdols = new PacketNewEffects();
            for (int i = 0; i < 5; ++i)
            {
                packetUpdateIdols.AddEffect(new PacketIdolUpdateEffect()
                {
                    Idol = new PacketIdol()
                    {
                        Color = surrenderingColor,
                        position = i,
                        Hp = 0,
                        MaxHp = 10
                    }
                });
            }
            packetUpdateIdols.AddEffect(new PacketMulliganDisabledEffect()
            {
                Color = surrenderingColor
            });
            session.Send(packetUpdateIdols);

            var packetEndGame = new PacketNewEffects();
            packetEndGame.AddEffect(new PacketEndGameEffect()
            {
                Winner = PlayerColor.black,
                WhiteStats = new PacketPlayerStats()
                {
                    ProfileId = session.Player.Id,
                    IdolDamage = 0,
                    UnitDamage = 0,
                    UnitsPlayed = 0,
                    SpellsPlayed = 0,
                    EnchantmentsPlayed = 0,
                    ScrollsDrawn = 0,
                    TotalMs = 12,
                    MostDamageUnit = 0
                },
                BlackStats = new PacketPlayerStats()
                {
                    ProfileId = session.Player.Id,
                    IdolDamage = 0,
                    UnitDamage = 0,
                    UnitsPlayed = 0,
                    SpellsPlayed = 0,
                    EnchantmentsPlayed = 0,
                    ScrollsDrawn = 0,
                    TotalMs = 1,
                    MostDamageUnit = 0
                },
                WhiteGoldReward = new PacketGoldReward()
                {
                    MatchReward = 10,
                    TierMatchReward = 1,
                    MatchCompletionReward = 100,
                    IdolsDestroyedReward = 50,
                    betReward = 0
                },
                BlackGoldReward = new PacketGoldReward()
                {
                    MatchReward = 10,
                    TierMatchReward = 1,
                    MatchCompletionReward = 100,
                    IdolsDestroyedReward = 50,
                    betReward = 0
                }
            });
            session.Send(packetEndGame);
        }

    }
}
