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
using RelentlessZero.Network;
using System;
using System.IO;

namespace RelentlessZero.Command
{
    class AccountCommands
    {
        [CommandHandler("accountcreate", AdminRole.Mojang, 3)]
        public static void HandleAccountCreate(Session session, string room, params string[] args)
        {
            string username = args[0];
            string password = args[1];

            AdminRole adminRole;
            if (!Enum.TryParse(args[2], out adminRole) || adminRole > AdminRole.Mojang)
            {
                CommandManager.Write(session, room, $"Invalid AdminRole value {args[2]}!");
                return;
            }

            if (InfoManager.GetAccountInfo(username) != null)
            {
                CommandManager.Write(session, room, $"Account {username} already exists!");
                return;
            }

            string passwordSalt = Sha.Hash(Path.GetRandomFileName()).ToLower();
            string passwordHash = Sha.Hash(password + passwordSalt, true).ToLower();
            DatabaseManager.ExecutePreparedStatement(PreparedStatement.AccountInsert, InfoManager.GetNewAccountId(), username, passwordHash, passwordSalt, adminRole);

            InfoManager.UpdateAccountInfo();

            if (InfoManager.GetAccountInfo(username) != null)
                CommandManager.Write(session, room, $"Account {username} successfully created!");
            else
                CommandManager.Write(session, room, $"Fatal error occured while creating account {username}!");
        }
    }
}
