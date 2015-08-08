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
using RelentlessZero.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RelentlessZero.Managers
{
    public class BattleInvite
    {
        public uint Opponent { get; set; }
        public uint TimeSinceInvite { get; set; }

        // cache incase either player is offline when invite is declined or accepted
        public PacketProfile ChallangerProfile { get; set; }
        public PacketProfile OpponentProfile { get; set; }

        public BattleInvite(Player challanger, Player opponent)
        {
            Opponent          = opponent.Id;
            TimeSinceInvite   = 0;
            ChallangerProfile = challanger.GeneratePacketProfile();
            OpponentProfile   = opponent.GeneratePacketProfile();
        }
    }

    public static class BattleManager
    {
        private static ConcurrentDictionary<uint, BattleInvite> inviteStore;
        public static ConcurrentDictionary<uint, Battle> Battles { get; set; }

        static BattleManager()
        {
            inviteStore = new ConcurrentDictionary<uint, BattleInvite>();
            Battles     = new ConcurrentDictionary<uint, Battle>();
        }

        public static void Update(uint diff)
        {
            var invitesToRemove = new List<uint>();
            foreach (var battleInvite in inviteStore)
            {
                var pendingInvite = battleInvite.Value;

                pendingInvite.TimeSinceInvite += diff;
                if (pendingInvite.TimeSinceInvite < 20000)
                    continue;

                // battle invite has exceded 20 second threshold, decline
                DeclineChallange(battleInvite.Key, battleInvite.Value);
                invitesToRemove.Add(battleInvite.Key);
            }

            foreach (var pendingInvite in invitesToRemove)
            {
                BattleInvite battleInvite;
                inviteStore.TryRemove(pendingInvite, out battleInvite);
            }
        }

        public static void ChallangePlayer(Player challanger, Player opponent)
        {
            // player can only have a single active battle invite
            if (GetPendingBattleInvite(opponent, challanger.Id) != null)
                return;

            inviteStore.TryAdd(challanger.Id, new BattleInvite(challanger, opponent));

            var packetGameChallenge = new PacketGameChallenge()
            {
                From            = challanger.GeneratePacketProfile(),
                ParentalConsent = false
            };

            opponent.Session.Send(packetGameChallenge);
        }

        public static void DeclineChallange(uint challanger, BattleInvite battleInvite, bool remove = false)
        {
            var gameChallengeResponse = new PacketGameChallengeResponse()
            {
                From   = battleInvite.ChallangerProfile,
                To     = battleInvite.OpponentProfile,
                Status = "DECLINE"
            };

            WorldManager.Send(challanger, gameChallengeResponse);
            WorldManager.Send(battleInvite.Opponent, gameChallengeResponse);

            if (remove)
            {
                BattleInvite removedBattleInvite;
                inviteStore.TryRemove(challanger, out removedBattleInvite);
            }
        }

        public static BattleInvite GetPendingBattleInvite(Player opponent, uint challanger)
        {
            foreach (var battleInvite in inviteStore)
                if (battleInvite.Key == challanger && battleInvite.Value.Opponent == opponent.Id)
                    return battleInvite.Value;

            return null;
        }
    }
}
