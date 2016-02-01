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

namespace RelentlessZero.Network.Handlers
{
    public static class BattleHandler
    {
        private static bool GetBattleInformation(Player player, string packetName, out Battle battle, out BattleInfo battleInfo)
        {
            battle = null;

            battleInfo = BattleManager.GetBattleInfo(player.Id);
            if (battleInfo == null)
            {
                // should only happen when a player reconnects to the battle server after crash or restart
                player.Session.SendFailPacket(packetName, "\nFailed to find battle session!");
                return false;
            }

            battle = BattleManager.GetBattle(battleInfo.BattleId);
            if (battle == null)
            {
                // shouldn't occur, remove battle info to prevent further issues
                BattleManager.RemoveBattleInfo(player.Id);

                LogManager.Write("Battle", $"Player {player.Id} has battle info for battle {battleInfo.BattleId}; it doesn't exist!");

                player.Session.SendFailPacket(packetName, "\nFailed to find battle session!");
                return false;
            }

            if (battle.Expired)
            {
                player.Session.SendFailPacket(packetName, "\nBattle session has closed.");
                return false;
            }

            return true;
        }

        [PacketHandler("EndPhase")]
        public static void HandleEndPhase(object packet, Session session)
        {
        }

        [PacketHandler("JoinBattle")]
        public static void HandleJoinBattle(object packet, Session session)
        {
            Battle battle;
            BattleInfo battleInfo;
            if (!GetBattleInformation(session.Player, "JoinBattle", out battle, out battleInfo))
                return;

            // wait for other player to connect
            battle.GetSide(battleInfo.SideColour).PlayerConnected(true);
        }

        [PacketHandler("LeaveGame")]
        public static void HandleLeaveGame(object packet, Session session)
        {
            Battle battle;
            BattleInfo battleInfo;
            if (!GetBattleInformation(session.Player, "LeaveGame", out battle, out battleInfo))
                return;

            if (battle.Phase != BattlePhase.End)
            {
                LogManager.Write("Battle", $"Player {session.Player.Id} tried to leave battle {battle.Id}; it hasn't ended!");
                return;
            }

            battle.GetSide(battleInfo.SideColour).AddPendingMove(BattleMoveType.LeaveGame);
        }

        [PacketHandler("Surrender")]
        public static void HandleSurrender(object packet, Session session)
        {
            var player = session.Player;

            Battle battle;
            BattleInfo battleInfo;
            if (!GetBattleInformation(player, "Surrender", out battle, out battleInfo))
                return;

            battle.GetSide(battleInfo.SideColour).AddPendingMove(BattleMoveType.Surrender);
        }
    }
}
