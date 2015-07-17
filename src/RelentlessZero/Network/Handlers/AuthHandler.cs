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

using RelentlessZero.Cryptography;
using RelentlessZero.Database;
using RelentlessZero.Entities;
using RelentlessZero.Logging;
using RelentlessZero.Managers;
using System;

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

            // decrypt login information
            string username = Rsa.Decrypt(firstConnect.Username);
            string password = Rsa.Decrypt(firstConnect.Password);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                LogManager.Write("Authentication", "Session {0} sent invalid account credentials (empty or whitespace)!", session.IpAddress);
                session.SendFatalFailPacket(packetName, "An internal error has occurred!");
                return;
            }

            // handle initial profile data from database
            SqlResult accountResult = DatabaseManager.Database.Select("SELECT id, username, password, salt, adminRole, flags FROM account_info WHERE username = ?", username);
            if (accountResult == null || accountResult.Count > 1)
            {
                LogManager.Write("Authentication", "Failed to lookup account {0} requested by session {1}!", username, session.IpAddress);
                session.SendFatalFailPacket(packetName, "Internal Error: Failed to lookup account information!");
                return;
            }


            AuthStatus authStatus = AuthStatus.InvalidCredentials;
            if (accountResult.Count == 1)
            {
                uint accountId = accountResult.Read<uint>(0, "id");

                authStatus = GetAuthStatus(accountId, accountResult.Read<string>(0, "password"), accountResult.Read<string>(0, "salt"), username, password);
                if (authStatus == AuthStatus.Ok)
                {
                    AdminRole adminRole = accountResult.Read<AdminRole>(0, "adminRole");
                    string usernameDatabase = accountResult.Read<string>(0, "username");

                    // avatar loading
                    Avatar playerAvatar = new Avatar();
                    playerAvatar.ProfileId = accountId;
                    SqlResult avatarResult = DatabaseManager.Database.Select("SELECT head, body, leg, armBack, armFront FROM account_avatar WHERE id = ?", accountId);
                    if (avatarResult.Count == 1)
                    {
                        playerAvatar.Head = avatarResult.Read<int>(0, "head");
                        playerAvatar.Body = avatarResult.Read<int>(0, "body");
                        playerAvatar.Leg = avatarResult.Read<int>(0, "leg");
                        playerAvatar.ArmBack = avatarResult.Read<int>(0, "armBack");
                        playerAvatar.ArmFront = avatarResult.Read<int>(0, "armFront");
                    }
                    else
                    {
                        LogManager.Write("HandleConnect", "user {0} has no avatar data!", username);
                    }

                    session.Player = new Player()
                    {
                        Id        = accountId,
                        Session   = session,
                        Username  = usernameDatabase,
                        AdminRole = adminRole,
                        Flags     = accountResult.Read<PlayerFlags>(0, "flags"),
                        Avatar    = playerAvatar
                    };

                    // send initial profile data to client
                    var profileInfo = new PacketProfileInfo
                    {
                        Profile = new PacketProfile
                        {
                            Id          = accountId,
                            Name        = usernameDatabase,
                            AdminRole   = adminRole,
                            FeatureType = "PREMIUM"
                        }
                    };

                    session.Send(profileInfo);

                    if (packetName == "FirstConnect")
                    {
                        session.SendString(AssetManager.ScrollTemplateCache);

                        // TODO: send other assets here...
                        // session.SendString("{\"type\":\"GROWTH_START_DECK\",\"msg\":\"Message\"}");
                    }

                    // authentication was successful, send packets to move client to main menu
                    session.SendOkPacket(packetName);
                    session.Send(new PacketActivateGame());
                }
                else
                    session.SendFailPacket(packetName, GetAuthFailStatusString(authStatus));
            }
            else
                session.SendFailPacket(packetName, GetAuthFailStatusString(authStatus));
        }

        private static AuthStatus GetAuthStatus(uint accountId, string passwordHash, string salt, string username, string password)
        {
            // check if password supplied matches
            if (!string.Equals(passwordHash, Sha.Hash(password + salt, true), StringComparison.OrdinalIgnoreCase))
                return AuthStatus.InvalidCredentials;

            if (WorldManager.IsPlayerOnline(username))
                return AuthStatus.AlreadySignedIn;

            // check temporary and permanent ban information
            SqlResult banResult = DatabaseManager.Database.Select("SELECT id, timestamp FROM account_ban WHERE id = ?", accountId);
            if (banResult == null)
                return AuthStatus.InternalError;

            if (banResult.Count == 1)
            {
                uint currentTimestamp = (uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                uint banTimestamp = banResult.Read<uint>(0, "timestamp");

                if (banTimestamp == 0)
                    return AuthStatus.BannedPermently;

                if (banTimestamp > currentTimestamp)
                    return AuthStatus.BannedTemporary;

                if (banTimestamp <= currentTimestamp)
                    DatabaseManager.Database.Execute("DELETE FROM account_ban WHERE id = ?", accountId);
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
            // handle remaining profile data from database
            SqlResult accountResult = DatabaseManager.Database.Select("SELECT gold, shards, rating FROM account_info WHERE username = ?", session.Player.Username);
            if (accountResult == null || accountResult.Count != 1)
            {
                session.SendFatalFailPacket("JoinLobby", "Failed to lookup account information!");
                return;
            }

            if (!WorldManager.AddPlayerSession(session))
            {
                // will only fail if 'JoinLobby' is sent before 'Connect', which shouldn't occur normally?
                session.SendFatalFailPacket("JoinLobby", "Failed to add player session!");
                return;
            }

            var player = session.Player;
            player.Gold   = accountResult.Read<uint>(0, "gold");
            player.Shards = accountResult.Read<uint>(0, "shards");
            player.Rating = accountResult.Read<ushort>(0, "rating");

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
                player.RemoveFlag(PlayerFlags.FirstLogin);
                AssetManager.GiveAllScrolls(player);
            }

            // TODO: friend stuff here

            session.SendOkPacket("JoinLobby");
        }
    }
}
