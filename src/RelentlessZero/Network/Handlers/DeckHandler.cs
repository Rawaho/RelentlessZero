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
using RelentlessZero.Logging;
using RelentlessZero.Managers;
using RelentlessZero.Network;
using System;
using System.Collections.Generic;

namespace RelentlessZero.Network.Handlers
{
    public static class DeckHandler
    {
        [PacketHandler("DeckCards")]
        public static void HandleDeckCards(object packet, Session session)
        {
            var packetDeckCardsCli = (PacketDeckCards)packet;

            var deck = session.Player.GetDeck(packetDeckCardsCli.Deck);
            if (deck == null)
            {
                LogManager.Write("Player", "Player {0} requested cards for non existant deck {1}!",
                    session.Player.Id, packetDeckCardsCli.Deck);
                return;
            }

            var deckCards = new PacketDeckCards()
            {
                Deck    = deck.Name,
                Scrolls = deck.Scrolls
            };

            session.Send(deckCards);
        }

        [PacketHandler("DeckDelete")]
        public static void HandleDeckDelete(object packet, Session session)
        {
            var packetDeckDeleteCli = (PacketDeckDelete)packet;

            var deck = session.Player.GetDeck(packetDeckDeleteCli.Name);
            if (deck == null)
            {
                LogManager.Write("Player", "Player {0} requested deletion of non existant deck {1}!",
                    session.Player.Id, packetDeckDeleteCli.Name);
                return;
            }

            session.Player.Decks.Remove(deck);
            deck.Delete();

            session.SendOkPacket("DeckDelete");
        }

        [PacketHandler("DeckList")]
        public static void HandleDeckList(object packet, Session session)
        {
            foreach (var deck in session.Player.Decks)
                deck.CalculateAge();

            var deckList = new PacketDeckList()
            {
                Decks = session.Player.Decks
            };

            session.Send(deckList);
        }

        [PacketHandler("DeckSave")]
        public static void HandleDeckSave(object packet, Session session)
        {
            if (session.Player.ValidatedDeck.Count == 0)
            {
                LogManager.Write("Player", "Player {0} tried to save a deck without validating it!", session.Player.Id);
                return;
            }

            // scrolls sent are completly ignored by server, saved scrolls from DeckValidate are used instead
            var packetDeckSaveCli = (PacketDeckSave)packet;

            var deck = session.Player.GetDeck(packetDeckSaveCli.Name);
            if (deck != null)
            {
                // update current deck
                deck.Scrolls.Clear();
                foreach (var scrollId in session.Player.ValidatedDeck)
                    deck.Scrolls.Add(session.Player.GetScroll(scrollId));

                deck.Timestamp = (ulong)DateTime.UtcNow.Ticks;
                deck.CalculateResources();
                deck.Save();
            }
            else
            {
                // create new deck
                var newDeck = new Deck(session.Player, AssetManager.GetNewDeckInstanceId(), packetDeckSaveCli.Name,
                    (ulong)DateTime.UtcNow.Ticks, DeckFlags.None);

                foreach (var scrollId in session.Player.ValidatedDeck)
                    newDeck.Scrolls.Add(session.Player.GetScroll(scrollId));

                newDeck.CalculateResources();
                newDeck.Save();

                session.Player.Decks.Add(newDeck);
            }

            session.Player.ValidatedDeck.Clear();
            session.SendOkPacket("DeckSave");
        }

        // TODO: handle proper validation
        [PacketHandler("DeckValidate")]
        public static void HandleDeckValidate(object packet, Session session)
        {
            var packetDeckValidateCli = (PacketDeckValidate)packet;

            if (packetDeckValidateCli.Scolls.Count == 0)
            {
                LogManager.Write("Player", "Player {0} tried to validate a deck with no cards!", session.Player.Id);
                return;
            }

            // make sure player is owner of every scroll
            foreach (var scrollId in packetDeckValidateCli.Scolls)
            {
                if (session.Player.GetScroll(scrollId) == null)
                {
                    LogManager.Write("Player", "Player {0} tried to validate a deck with cards they don't own!", session.Player.Id);
                    return;
                }
            }

            session.Send(new PacketDeckValidate());
            session.Player.ValidatedDeck = packetDeckValidateCli.Scolls;
        }

        [PacketHandler("LibraryView")]
        public static void HandleLibraryView(object packet, Session session)
        {
            var libraryView = new PacketLibraryView()
            {
                ProfileId = session.Player.Id,
                Cards     = session.Player.Scrolls
            };

            session.Send(libraryView);
        }
    }
}
