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

using Newtonsoft.Json;
using RelentlessZero.Database;
using RelentlessZero.Entities;
using RelentlessZero.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public AccountInfoAvatar Avatar { get; set; }

        public List<AccountInfoScroll> Scrolls { get; } = new List<AccountInfoScroll>();

        // cache scroll information to prevent iterating instance store
        public uint[] ScrollRarityCache = new uint[(byte)ScrollRarity.Count];
        public uint ScrollsUniqueCache { get; private set; }

        public void AddScroll(AccountInfoScroll scroll)
        {
            Contract.Requires(scroll.Template.Rarity < ScrollRarity.Count);
            ScrollRarityCache[(byte)scroll.Template.Rarity]++;

            if (!Scrolls.Any(scrollInstance => scrollInstance.Template.Entry == scroll.Template.Entry))
                ScrollsUniqueCache++;

            Scrolls.Add(scroll);
        }

        public uint GetScrollRarityCount(ScrollRarity rarity)
        {
            Contract.Requires(rarity < ScrollRarity.Count);
            return ScrollRarityCache[(byte)rarity];
        }
    }

    public struct AccountInfoAvatar
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint Id { get; }
        [JsonProperty(PropertyName = "head")]
        public ushort Head { get; }
        [JsonProperty(PropertyName = "body")]
        public ushort Body { get; }
        [JsonProperty(PropertyName = "leg")]
        public ushort Leg { get; }
        [JsonProperty(PropertyName = "armBack")]
        public ushort ArmBack { get; }
        [JsonProperty(PropertyName = "armFront")]
        public ushort ArmFront { get; }

        public AccountInfoAvatar(uint id, ushort head, ushort body, ushort leg, ushort armBack, ushort armFront)
        {
            Id       = id;
            Head     = head;
            Body     = body;
            Leg      = leg;
            ArmBack  = armBack;
            ArmFront = armFront;
        }
    }

    public struct AccountInfoScroll
    {
        // store full scroll instance instead?
        public ulong Id { get; }
        public ScrollTemplate Template { get; }

        public AccountInfoScroll(ulong id, ScrollTemplate template)
        {
            Id       = id;
            Template = template;
        }
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
            accountId        = 1u;
            accountLock      = new object();

            UpdateAccountInfo(true);
        }

        public static void UpdateAccountInfo(bool full = false)
        {
            lock (accountLock)
            {
                DateTime startTime = DateTime.Now;

                var accountInfoResult = DatabaseManager.Database.Select("SELECT `id`, `username`, `adminRole`, `gold`, `shards`, `rating` FROM `account_info`");
                Contract.Assert(accountInfoResult != null);

                uint accountCount = 0u;
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
                    accountCount++;
                }

                if (full)
                {
                    var accountAvatarResult = DatabaseManager.Database.Select("SELECT `id`, `head`, `body`, `leg`, `armBack`, `armFront` FROM `account_avatar`");
                    Contract.Assert(accountAvatarResult != null);

                    for (int i = 0; i < accountAvatarResult.Count; i++)
                    {
                        var accountInfo = accountInfoStore[accountAvatarResult.Read<uint>(i, "id")];
                        accountInfo.Avatar = new AccountInfoAvatar(accountInfo.Id, accountAvatarResult.Read<ushort>(i, "head"), accountAvatarResult.Read<ushort>(i, "body"),
                            accountAvatarResult.Read<ushort>(i, "leg"), accountAvatarResult.Read<ushort>(i, "armBack"), accountAvatarResult.Read<ushort>(i, "armFront"));
                    }

                    var accountScrollResult = DatabaseManager.Database.Select("SELECT `accountId`, `id`, `scrollEntry` FROM `scroll_instance`");
                    Contract.Assert(accountScrollResult != null);

                    for (int i = 0; i < accountScrollResult.Count; i++)
                    {
                        var scrollTemplate = AssetManager.GetScrollTemplate(accountScrollResult.Read<ushort>(i, "scrollEntry"));
                        if (scrollTemplate == null)
                            continue;

                        accountInfoStore[accountScrollResult.Read<uint>(i, "accountId")].AddScroll(new AccountInfoScroll(accountScrollResult.Read<ulong>(i, "id"), scrollTemplate));
                    }
                }

                LogManager.Write("Info Manager", $"Updated information for {accountCount} account(s) in {(DateTime.Now - startTime).Milliseconds} milliseconds(s).");
            }
        }

        public static void UpdateAvatar(Avatar avatar)
        {
            Contract.Requires(avatar.Id != 0);
            accountInfoStore[avatar.Id].Avatar = new AccountInfoAvatar(avatar.Id, avatar.Head, avatar.Body, avatar.Leg, avatar.ArmBack, avatar.ArmFront);
        }

        public static void AddScroll(ScrollInstance scroll)
        {
            Contract.Requires(scroll.AccountId != 0);
            accountInfoStore[scroll.AccountId].AddScroll(new AccountInfoScroll(scroll.Id, scroll.Scroll));
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
