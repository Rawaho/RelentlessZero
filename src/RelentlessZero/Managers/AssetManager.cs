﻿/*
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
using RelentlessZero.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace RelentlessZero.Managers
{
    public static class AssetManager
    {
        public static string ScrollTemplateCache { get; set; }
        public static string AvatarPartTemplateCache { get; set; }

        // asset counters
        private static object scrollInstanceIdLock;
        private static ulong scrollInstanceId;
        private static object deckInstanceIdLock;
        private static uint deckInstanceId;

        // used to generate scroll templates
        private static List<AbilityTemplate> abilityTemplateStore;
        private static List<PassiveTemplate> passiveTemplateStore;
        private static List<TagTemplate> tagTemplateStore;

        private static Dictionary<ushort, Type> unitChildStore;

        // scroll template stores
        public static List<ScrollTemplate> ScrollTemplateStore { get; set; }

        public static List<AvatarPartTemplate> AvatarPartTemplateStore { get; set; }

        public static void LoadAssets()
        {
            InitialiseAssetCounters();

            LoadAbilityTemplates();
            LoadPassiveTemplates();
            LoadTagTemplates();

            // must be loaded after ability, passive and tag templates!
            LoadScrollTemplates();

            LoadAvatarPartTemplates();

            // cache packet data
            CacheCardTypeData();
            CacheAvatarTypeData();

            LoadUnitChildren();
        }

        private static void LoadAvatarPartTemplates()
        {
            DateTime startTime = DateTime.Now;
            AvatarPartTemplateStore = new List<AvatarPartTemplate>();

            var avatarPartResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.AvatarTemplateSelect);
            Contract.Assert(avatarPartResult != null);

            for (int i = 0; i < avatarPartResult.Count; i++)
            {
                var avatarPartTemplate = new AvatarPartTemplate();
                avatarPartTemplate.Entry    = avatarPartResult.Read<ushort>(i, "entry");
                avatarPartTemplate.Type     = avatarPartResult.Read<AvatarPartRarity>(i, "type");
                avatarPartTemplate.Part     = avatarPartResult.Read<AvatarPartName>(i, "part");
                avatarPartTemplate.Filename = avatarPartResult.Read<string>(i, "filename");
                avatarPartTemplate.Set      = avatarPartResult.Read<AvatarPartSet>(i, "set");

                if (avatarPartTemplate.Type > AvatarPartRarity.STORE)
                {
                    LogManager.Write("Asset Manager", "Failed to load avatar part template {0}, template has invalid avatar part type {1}!",
                        avatarPartTemplate.Entry, avatarPartTemplate.Type);
                    continue;
                }

                if (avatarPartTemplate.Part >= AvatarPartName.INVALID)
                {
                    LogManager.Write("Asset Manager", "Failed to load avatar part template {0}, template has invalid avatar part name {1}!",
                        avatarPartTemplate.Entry, avatarPartTemplate.Part);
                    continue;
                }

                if (avatarPartTemplate.Set > AvatarPartSet.FEMALE_1)
                {
                    LogManager.Write("Asset Manager", "Failed to load avatar part template {0}, template has invalid avatar set {1}!",
                        avatarPartTemplate.Entry, avatarPartTemplate.Set);
                    continue;
                }

                AvatarPartTemplateStore.Add(avatarPartTemplate);
            }

            LogManager.Write("Asset Manager", "Loaded {0} avatar part(s) from database {1} milliseconds(s).", AvatarPartTemplateStore.Count, (DateTime.Now - startTime).Milliseconds);
        }

        private static void CacheAvatarTypeData()
        {
            var cardTypes = new PacketAvatarTypes
            {
                Msg   = "AvatarTypes",
                Types = AvatarPartTemplateStore
            };

            AvatarPartTemplateCache = JsonConvert.SerializeObject(cardTypes);
        }

        private static void LoadAbilityTemplates()
        {
            DateTime startTime = DateTime.Now;
            abilityTemplateStore = new List<AbilityTemplate>();

            var abilityResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollAbilityTemplateSelect);
            Contract.Assert(abilityResult != null);

            for (int i = 0; i < abilityResult.Count; i++)
            {
                var abilityTemplate = new AbilityTemplate();
                abilityTemplate.Entry       = abilityResult.Read<ushort>(i, "entry");
                abilityTemplate.Id          = abilityResult.Read<string>(i, "id");
                abilityTemplate.Name        = abilityResult.Read<string>(i, "name");
                abilityTemplate.Description = abilityResult.Read<string>(i, "description");
                abilityTemplate.Resource    = abilityResult.Read<ResourceType>(i, "resource");
                abilityTemplate.Cost        = abilityResult.Read<byte>(i, "cost");

                if (abilityTemplate.Resource > ResourceType.DECAY)
                {
                    LogManager.Write("Asset Manager", "Failed to load ability template {0}, template has invalid scroll resource type {1}!",
                        abilityTemplate.Entry, abilityTemplate.Resource);
                    continue;
                }

                abilityTemplateStore.Add(abilityTemplate);
            }

            LogManager.Write("Asset Manager", "Loaded {0} abilities(s) from database {1} milliseconds(s).", abilityTemplateStore.Count, (DateTime.Now - startTime).Milliseconds);
        }

        private static void LoadPassiveTemplates()
        {
            DateTime startTime = DateTime.Now;
            passiveTemplateStore = new List<PassiveTemplate>();

            var passiveResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollPassiveTemplateSelect);
            Contract.Assert(passiveResult != null);

            for (int i = 0; i < passiveResult.Count; i++)
            {
                var passiveTemplate = new PassiveTemplate();
                passiveTemplate.Entry       = passiveResult.Read<ushort>(i, "entry");
                passiveTemplate.Name        = passiveResult.Read<string>(i, "name");
                passiveTemplate.Description = passiveResult.Read<string>(i, "description");
                passiveTemplateStore.Add(passiveTemplate);
            }

            LogManager.Write("Asset Manager", "Loaded {0} passive(s) from database {1} milliseconds(s).", passiveTemplateStore.Count, (DateTime.Now - startTime).Milliseconds);
        }

        private static void LoadTagTemplates()
        {
            DateTime startTime = DateTime.Now;
            tagTemplateStore = new List<TagTemplate>();

            var tagResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollTagTemplateSelect);
            Contract.Assert(tagResult != null);

            for (int i = 0; i < tagResult.Count; i++)
            {
                var tagTemplate = new TagTemplate();
                tagTemplate.Entry = tagResult.Read<ushort>(i, "entry");
                tagTemplate.Name  = tagResult.Read<string>(i, "name");
                tagTemplate.Type  = tagResult.Read<TagType>(i, "type");

                if (tagTemplate.Type > TagType.String)
                {
                    LogManager.Write("Asset Manager", "Failed to load tag template {0}, tag has invalid type {1}!", tagTemplate.Entry, tagTemplate.Type);
                    continue;
                }

                tagTemplateStore.Add(tagTemplate);
            }

            LogManager.Write("Asset Manager", "Loaded {0} tags(s) from database {1} milliseconds(s).", tagTemplateStore.Count, (DateTime.Now - startTime).Milliseconds);
        }

        private static void LoadScrollTemplates()
        {
            DateTime startTime = DateTime.Now;

            ScrollTemplateStore = new List<ScrollTemplate>();

            var scrollResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollTemplateSelect);
            Contract.Assert(scrollResult != null);

            for (int i = 0; i < scrollResult.Count; i++)
            {
                var scrollTemplate = new ScrollTemplate();
                scrollTemplate.Entry          = scrollResult.Read<ushort>(i, "entry");
                scrollTemplate.Name           = scrollResult.Read<string>(i, "name");
                scrollTemplate.Description    = scrollResult.Read<string>(i, "description");
                scrollTemplate.Flavor         = scrollResult.Read<string>(i, "flavor");

                var subTypeResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollTemplateSubTypeSelect, scrollTemplate.Entry);
                Contract.Assert(subTypeResult != null);

                for (int j = 0; j < subTypeResult.Count; j++)
                {
                    var subType = subTypeResult.Read<ScrollSubType>(j, "subType");
                    if (subType > ScrollSubType.Cat)
                    {
                        LogManager.Write("Asset Manager", "Failed to add subtype {0} to scroll template {1}, subtype is invalid!",
                            scrollTemplate.Entry, subType);
                        continue;
                    }

                    scrollTemplate.SubTypes.Add(subType);
                }

                // generate sub type string
                for (int j = 0; j < scrollTemplate.SubTypes.Count; j++ )
                {
                    scrollTemplate.SubTypesStr += scrollTemplate.SubTypes[j].ToString();
                    if (j + 1 < scrollTemplate.SubTypes.Count)
                        scrollTemplate.SubTypesStr += ",";
                }

                scrollTemplate.Kind           = scrollResult.Read<ScrollKind>(i, "kind");

                if (scrollTemplate.Kind > ScrollKind.STRUCTURE)
                {
                    LogManager.Write("Asset Manager", "Failed to load scroll template {0}, template has invalid scroll kind {1}!",
                        scrollTemplate.Entry, scrollTemplate.Kind);
                    continue;
                }

                scrollTemplate.Rarity         = scrollResult.Read<ScrollRarity>(i, "rarity");
                scrollTemplate.Health         = scrollResult.Read<byte>(i, "health");
                scrollTemplate.Attack         = scrollResult.Read<byte>(i, "attack");
                scrollTemplate.Cooldown       = scrollResult.Read<sbyte>(i, "cooldown");
                scrollTemplate.Resource       = scrollResult.Read<ResourceType>(i, "resource");

                if (scrollTemplate.Resource > ResourceType.DECAY)
                {
                    LogManager.Write("Asset Manager", "Failed to load scroll template {0}, template has invalid scroll resource type {1}!",
                        scrollTemplate.Entry, scrollTemplate.Resource);
                    continue;
                }

                scrollTemplate.Cost           = scrollResult.Read<byte>(i, "cost");

                // calculate cost data for client
                switch (scrollTemplate.Resource)
                {
                    case ResourceType.ORDER:
                        scrollTemplate.CostOrder = scrollTemplate.Cost;
                        break;
                    case ResourceType.ENERGY:
                        scrollTemplate.CostEnergy = scrollTemplate.Cost;
                        break;
                    case ResourceType.GROWTH:
                        scrollTemplate.CostGrowth = scrollTemplate.Cost;
                        break;
                    case ResourceType.DECAY:
                        scrollTemplate.CostDecay = scrollTemplate.Cost;
                        break;
                }

                scrollTemplate.Set            = scrollResult.Read<byte>(i, "set");
                scrollTemplate.LimitedWeight  = scrollResult.Read<float>(i, "limitedWeight");

                // link tags to scroll template
                var tagResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollTemplateTagSelect, scrollTemplate.Entry);
                Contract.Assert(tagResult != null);

                for (int j = 0; j < tagResult.Count; j++)
                {
                    ushort tagEntry = tagResult.Read<ushort>(j, "tagEntry");

                    var tagTemplate = GetTagTemplate(tagEntry);
                    if (tagTemplate == null)
                    {
                        LogManager.Write("Asset Manager", "Failed to add tag {0} to scroll template {1}, tag entry is invalid!",
                            tagEntry, scrollTemplate.Entry);
                        continue;
                    }

                    var scrollTag = new ScrollTag();
                    scrollTag.Name  = tagTemplate.Name;
                    scrollTag.Type  = tagTemplate.Type;
                    scrollTag.Value = tagResult.Read<string>(j, "value");
                    scrollTemplate.Tags.Add(scrollTag);
                }

                scrollTemplate.CardImage       = scrollResult.Read<ushort>(i, "cardImage");

                if (scrollTemplate.CardImage == 0)
                {
                    LogManager.Write("Asset Manager", "Failed to load scroll template {0}, template has invalid card image!", scrollTemplate.Entry);
                    continue;
                }

                scrollTemplate.AnimationImage  = scrollResult.Read<ushort>(i, "animationImage");
                scrollTemplate.AnimationInfo   = scrollResult.Read<string>(i, "animationInfo");
                scrollTemplate.AnimationBundle = scrollResult.Read<ushort>(i, "animationBundle");

                // link abilities to scroll template
                var abilityResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollTemplateAbilitySelect, scrollTemplate.Entry);
                Contract.Assert(abilityResult != null);

                for (int j = 0; j < abilityResult.Count; j++)
                {
                    ushort abilityEntry = abilityResult.Read<ushort>(j, "abilityEntry");

                    var abilityTemplate = GetAbilityTemplate(abilityEntry);
                    if (abilityTemplate == null)
                    {
                        LogManager.Write("Asset Manager", "Failed to add ability {0} to scroll template {1}, ability entry is invalid!",
                            abilityEntry, scrollTemplate.Entry);
                        continue;
                    }

                    scrollTemplate.Abilities.Add(abilityTemplate);
                }

                scrollTemplate.TargetArea      = scrollResult.Read<ScrollTargetArea>(i, "targetArea");

                if (scrollTemplate.TargetArea > ScrollTargetArea.ROW_FULL)
                {
                    LogManager.Write("Asset Manager", "Failed to load scroll template {0}, template has invalid target area {1}!",
                        scrollTemplate.Entry, scrollTemplate.TargetArea);
                    continue;
                }

                // link passives to scroll template
                var passiveResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollTemplatePassiveSelect, scrollTemplate.Entry);
                Contract.Assert(passiveResult != null);

                for (int j = 0; j < passiveResult.Count; j++)
                {
                    ushort passiveEntry = passiveResult.Read<ushort>(j, "passiveEntry");

                    var passiveTemplate = GetPassiveTemplate(passiveEntry);
                    if (passiveTemplate == null)
                    {
                        LogManager.Write("Asset Manager", "Failed to add passive {0} to scroll template {1}, passive entry is invalid!",
                            passiveEntry, scrollTemplate.Entry);
                        continue;
                    }

                    scrollTemplate.Passives.Add(passiveTemplate);
                }

                ScrollTemplateStore.Add(scrollTemplate);
            }

            LogManager.Write("Asset Manager", "Loaded {0} scroll(s) from database {1} milliseconds(s).", ScrollTemplateStore.Count, (DateTime.Now - startTime).Milliseconds);
        }

        private static void CacheCardTypeData()
        {
            var cardTypes = new PacketCardTypes
            {
                Msg       = "CardTypes",
                CardTypes = ScrollTemplateStore
            };

            ScrollTemplateCache = JsonConvert.SerializeObject(cardTypes);
        }

        public static AvatarPartTemplate GetAvatarPartTemplate(ushort entry) { return AvatarPartTemplateStore.SingleOrDefault(avatarPart => avatarPart.Entry == entry); }
        public static bool HasAvatarPartTemplate(ushort entry) { return GetAvatarPartTemplate(entry) != null; }

        public static ushort GetRandomAvatarPart(AvatarPartName avatarPart, AvatarPartRarity partRarity = AvatarPartRarity.COMMON, AvatarPartSet partSet = AvatarPartSet.MALE_1)
        {
            var compatibleParts = AvatarPartTemplateStore.Where(partTemplate => partTemplate.Part == avatarPart
                && partTemplate.Type == partRarity
                && partTemplate.Set == partSet);

            if (!compatibleParts.Any())
                return 0;

            return compatibleParts.ElementAt(new Random().Next(0, compatibleParts.Count())).Entry;
        }

        public static ScrollTemplate GetScrollTemplate(ushort entry) { return ScrollTemplateStore.SingleOrDefault(scroll => scroll.Entry == entry); }

        private static AbilityTemplate GetAbilityTemplate(ushort entry) { return abilityTemplateStore.SingleOrDefault(ability => ability.Entry == entry); }
        private static PassiveTemplate GetPassiveTemplate(ushort entry) { return passiveTemplateStore.SingleOrDefault(passive => passive.Entry == entry); }
        private static TagTemplate GetTagTemplate(ushort entry) { return tagTemplateStore.SingleOrDefault(tag => tag.Entry == entry); }

        private static void InitialiseAssetCounters()
        {
            var cardInstanceIdResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.ScrollMax);
            var deckinstanceIdResult = DatabaseManager.SelectPreparedStatement(PreparedStatement.DeckMax);

            scrollInstanceIdLock = new object();
            deckInstanceIdLock   = new object();

            // next available scroll instance id
            if (cardInstanceIdResult != null)
                scrollInstanceId = cardInstanceIdResult.Read<ulong>(0, "MAX(`id`)") + 1;

            // next available deck instance id
            if (deckinstanceIdResult != null)
                deckInstanceId = deckinstanceIdResult.Read<uint>(0, "MAX(`id`)") + 1;
        }

        public static ulong GetNewScrollInstanceId()
        {
            lock (scrollInstanceIdLock)
                return scrollInstanceId++;
        }

        public static uint GetNewDeckInstanceId()
        {
            lock (deckInstanceIdLock)
                return deckInstanceId++;
        }

        private static void LoadUnitChildren()
        {
            unitChildStore = new Dictionary<ushort, Type>();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                if (type.GetCustomAttributes<ScrollAttribute>().Count() == 1)
                    unitChildStore[type.GetCustomAttributes<ScrollAttribute>().First().ScrollEntry] = type;
        }

        public static Type GetUnitChild(ushort entry) { return unitChildStore.SingleOrDefault(child => child.Key == entry).Value; }
    }
}
