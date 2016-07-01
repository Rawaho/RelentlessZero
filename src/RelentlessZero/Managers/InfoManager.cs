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

using RelentlessZero.Database;
using RelentlessZero.Entities;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Linq;

namespace RelentlessZero.Managers
{
    public class AccountInfo
    {
        public uint Id { get; set; }
        public string Username { get; set; }
        public AdminRole AdminRole { get; set; }
        public uint Gold { get; set; }
        public uint Shards { get; set; }
        public short Rating { get; set; }

        // TODO: store more info about player...
    }

    public static class InfoManager
    {
        // stores basic information on all accounts, online and offline
        private static ConcurrentDictionary<uint, AccountInfo> accountInfoStore;

        private static uint accountId;
        private static object accountLock;

        public static void Initialise()
        {
            accountInfoStore = new ConcurrentDictionary<uint, AccountInfo>();
            accountLock      = new object();

            UpdateAccountInfo();
        }

        public static void UpdateAccountInfo()
        {
            lock (accountLock)
            {
                var accountInfoResult = DatabaseManager.Database.Select("SELECT `id`, `username`, `adminRole`, `gold`, `shards`, `rating` FROM `account_info`");
                Contract.Assert(accountInfoResult != null);

                for (int i = 0; i < accountInfoResult.Count; i++)
                {
                    var accountInfo = new AccountInfo();
                    accountInfo.Id        = accountInfoResult.Read<uint>(i, "id");
                    accountInfo.Username  = accountInfoResult.Read<string>(i, "username");
                    accountInfo.AdminRole = accountInfoResult.Read<AdminRole>(i, "adminRole");
                    accountInfo.Gold      = accountInfoResult.Read<uint>(i, "gold");
                    accountInfo.Shards    = accountInfoResult.Read<uint>(i, "shards");
                    accountInfo.Rating    = accountInfoResult.Read<short>(i, "rating");
                    accountInfoStore.AddOrUpdate(accountInfo.Id, accountInfo, (k, v) => accountInfo);

                    accountId = Math.Max(accountId, accountInfo.Id) + 1;
                }
            }
        }

        public static uint GetNewAccountId()
        {
            lock (accountLock)
                return accountId;
        }

        public static AccountInfo GetAccountInfo(uint id) { return accountInfoStore.SingleOrDefault(accountInfo => accountInfo.Value.Id == id).Value; }
        public static AccountInfo GetAccountInfo(string username) { return accountInfoStore.SingleOrDefault(accountInfo => accountInfo.Value.Username.ToLower() == username.ToLower()).Value; }
    }
}
