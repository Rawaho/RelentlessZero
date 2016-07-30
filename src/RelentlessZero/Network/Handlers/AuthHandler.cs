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

using RelentlessZero.Cryptography;
using RelentlessZero.Database;
using RelentlessZero.Entities;
using RelentlessZero.Managers;
using System;
using System.Diagnostics.Contracts;

namespace RelentlessZero.Network.Handlers
{
    public static class AuthHandler
    {
        private enum AuthStatus
        {
            Ok,
            InvalidCredentials,
            BannedTemporary,
            BannedPermently,
            BannedIpAddress,
            AlreadySignedIn,
            InternalError
        }

        [PacketHandler("Connect")]
        [PacketHandler("FirstConnect")]
        public static void HandleConnect(object packet, Session session)
        {
            var firstConnect = (PacketConnect)packet;
            string packetName = packet.GetType().Name.Remove(0, 6);

            // decrypt login information using RSA private key
            string username = Rsa.Decrypt(firstConnect.Username);
            string password = Rsa.Decrypt(firstConnect.Password);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                session.SendFatalFailPacket(packetName, "An internal error has occurred!");
                return;
            }

            var authStatus = AuthStatus.InvalidCredentials;

            // retrieve account information
            var accountResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.AccountSelect, username);
            Contract.Assert(accountResult != null);

            if (accountResult.Count != 0)
                authStatus = GetAuthStatus(accountResult.Read<uint>(0, "id"), accountResult.Read<string>(0, "password"), accountResult.Read<string>(0, "salt"), username, password);

            if (authStatus != AuthStatus.Ok)
            {
                session.SendFailPacket(packetName, GetAuthFailStatusString(authStatus));
                return;
            }

            // authentication was successful
            session.Player = new Player()
            {
                Id        = accountResult.Read<uint>(0, "id"),
                Session   = session,
                Username  = username,
                AdminRole = accountResult.Read<AdminRole>(0, "adminRole"),
                Gold      = accountResult.Read<uint>(0, "gold"),
                Shards    = accountResult.Read<uint>(0, "shards"),
                Rating    = accountResult.Read<ushort>(0, "rating"),
                Flags     = accountResult.Read<PlayerFlags>(0, "flags"),
            };

            if (!WorldManager.AddPlayerSession(session))
            {
                session.SendFatalFailPacket(packetName, "An internal error has occurred!");
                return;
            }

            // send initial profile data to client
            var profileInfo = new PacketProfileInfo
            {
                Profile = session.Player.GeneratePacketProfile()
            };

            session.Send(profileInfo);

            if (packetName == "FirstConnect")
            {
                // only send game asset information on initial login
                session.SendString(AssetManager.ScrollTemplateCache);
                session.SendString(AssetManager.AvatarPartTemplateCache);

                // TODO: send other assets here...
                // session.SendString("{\"type\":\"GROWTH_START_DECK\",\"msg\":\"Message\"}");
            }

            // send packets to make client request join to lobby or game
            session.SendOkPacket(packetName);

            if (session.Type != SessionType.Battle)
                session.Send(new PacketActivateGame());
        }

        private static AuthStatus GetAuthStatus(uint accountId, string passwordHash, string salt, string username, string password)
        {
            // check if password supplied matches
            if (!string.Equals(passwordHash, Sha.Hash(password + salt, true), StringComparison.OrdinalIgnoreCase))
                return AuthStatus.InvalidCredentials;

            if (WorldManager.IsPlayerOnline(username))
                return AuthStatus.AlreadySignedIn;

            // check temporary and permanent ban information
            var banResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.BanSelect, accountId);
            Contract.Assert(banResult != null);

            if (banResult.Count != 0)
            {
                uint currentTimestamp = Helper.GetUnixTime();
                uint banTimestamp = banResult.Read<uint>(0, "timestamp");

                if (banTimestamp == 0)
                    return AuthStatus.BannedPermently;

                if (banTimestamp > currentTimestamp)
                    return AuthStatus.BannedTemporary;

                if (banTimestamp <= currentTimestamp)
                    DatabaseManager.ExecutePreparedStatement(PreparedStatement.DeckDelete, accountId);
            }

            return AuthStatus.Ok;
        }

        private static string GetAuthFailStatusString(AuthStatus authStatus)
        {
            string message = "RelentlessZero: ";
            switch (authStatus)
            {
                case AuthStatus.InvalidCredentials:
                    return message += "Invalid account credentials!";
                case AuthStatus.BannedTemporary:
                    return message += "Account is temporary closed, try again later.";
                case AuthStatus.BannedPermently:
                    return message += "Account is permently closed due to abuse.";
                case AuthStatus.AlreadySignedIn:
                    return message += "Account is already signed in, try again later.";
                case AuthStatus.InternalError:
                default:
                    return message += "An internal error has occurred!";
            }
        }

        [PacketHandler("JoinLobby")]
        public static void HandleJoinLobby(object packet, Session session)
        {
            var player = session.Player;

            // send remaining profile data to client
            var profileDataInfo = new PacketProfileDataInfo
            {
                ProfileData = new PacketProfileData()
                {
                    Gold               = player.Gold,
                    Shards             = player.Shards,
                    StarterDeckVersion = 1, // use currently unknown
                    SpectatePermission = player.HasFlag(PlayerFlags.SpectateAllow) ? (session.Player.HasFlag(PlayerFlags.SpectateHideChat) ? "ALLOW" : "AllOW_CHAT") : "DISALLOW",
                    AcceptTrades       = player.HasFlag(PlayerFlags.AcceptTrades),
                    AcceptChallenges   = player.HasFlag(PlayerFlags.AcceptChallenges),
                    Rating             = player.Rating
                }
            };

            session.Send(profileDataInfo);

            // handle first login
            if (player.HasFlag(PlayerFlags.FirstLogin))
            {
                foreach (var scrollTemplate in AssetManager.ScrollTemplateStore)
                    player.Scrolls.Add(new ScrollInstance(scrollTemplate, player.Id));

                // default avatar pieces
                player.Avatar.SetAvatar(player.Id, 33, 10, 41, 4, 15);
                player.Avatar.SaveAvatar();

                player.RemoveFlag(PlayerFlags.FirstLogin);
            }
            else
            {
                // load avatar information
                var avatarResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.AvatarSelect, player.Id);
                Contract.Assert(avatarResult != null);

                if (avatarResult.Count == 0)
                {
                    session.SendFatalFailPacket("JoinLobby", "Failed to lookup account avatar information!");
                    return;
                }

                player.Avatar.SetAvatar(player.Id, avatarResult.Read<ushort>(0, "head"), avatarResult.Read<ushort>(0, "body"),
                    avatarResult.Read<ushort>(0, "leg"), avatarResult.Read<ushort>(0, "armBack"), avatarResult.Read<ushort>(0, "armFront"));
            }

            player.LoadScrolls();
            player.LoadDecks();

            // TODO: friend stuff here

            session.SendOkPacket("JoinLobby");
        }
    }
}
