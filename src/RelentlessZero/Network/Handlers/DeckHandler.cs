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

using RelentlessZero.Managers;
using RelentlessZero.Network;
using System.Collections.Generic;

namespace RelentlessZero.Network.Handlers
{
    public static class DeckHandler
    {
        [PacketHandler("LibraryView")]
        public static void HandleLibraryView(object packet, Session session)
        {
            // temporary for testing purposes
            var libraryView = new PacketLibraryView()
            {
                ProfileId = session.Player.Id,
                Cards     = new List<PacketCard>()
            };

            foreach (var scrollTemplate in AssetManager.ScrollTemplateStore)
            {
                var packetCard = new PacketCard()
                {
                    Id       = scrollTemplate.Entry,
                    TypeId   = scrollTemplate.Entry,
                    Tradable = false,
                    IsToken  = false,
                    Level    = 0
                };

                libraryView.Cards.Add(packetCard);
            }

            session.Send(libraryView);

        }
    }
}
