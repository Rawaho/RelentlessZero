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
using RelentlessZero.Managers;
using System;

namespace RelentlessZero.Entities
{
    // custom serialiser for scroll instances
    public class JsonScrollInstanceSerialiser : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var scrollInstance = (ScrollInstance)value;

            writer.WriteStartObject();
            writer.WritePropertyName("id");
            writer.WriteValue(scrollInstance.Id);
            writer.WritePropertyName("typeId");
            writer.WriteValue(scrollInstance.Scroll.Entry);
            writer.WritePropertyName("tradable");
            writer.WriteValue(scrollInstance.Tradable);
            writer.WritePropertyName("isToken");
            writer.WriteValue(false);               // TODO
            writer.WritePropertyName("level");
            writer.WriteValue(scrollInstance.Level);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // scroll instances are sent by server and should never be recieved from client
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType) { return typeof(ScrollTemplate).IsAssignableFrom(objectType); }
    }

    public class ScrollStats
    {
        [JsonProperty(PropertyName = "damage")]
        public uint Damage { get; set; }
        [JsonProperty(PropertyName = "destroyed")]
        public uint Destroyed { get; set; }
        [JsonProperty(PropertyName = "heal")]
        public uint Heal { get; set; }
        [JsonProperty(PropertyName = "idolKills")]
        public uint IdolKills { get; set; }
        [JsonProperty(PropertyName = "played")]
        public uint Played { get; set; }
        [JsonProperty(PropertyName = "sacrificed")]
        public uint Sacrificed { get; set; }
        [JsonProperty(PropertyName = "totalGames")]
        public uint TotalGames { get; set; }
        [JsonProperty(PropertyName = "unitKills")]
        public uint UnitKills { get; set; }
        [JsonProperty(PropertyName = "wins")]
        public uint Wins { get; set; }
    }

    // custom serialiser
    [JsonConverter(typeof(JsonScrollInstanceSerialiser))]
    public class ScrollInstance
    {
        public const uint MaxLevel = 2u;

        public ulong Id { get; }
        public uint AccountId { get; private set; }
        public ScrollTemplate Scroll { get; }
        public byte Level { get; }
        public long Timestamp { get; }
        public ScrollStats Stats { get; } = new ScrollStats();
        public bool Tradable { get; }
        public bool SaveNeeded { get; private set; }

        public ScrollInstance(ScrollTemplate scrollTemplate, uint owner = 0, bool instantSave = false)
        {
            Id         = AssetManager.GetNewScrollInstanceId();
            Scroll     = scrollTemplate;
            Timestamp  = DateTime.UtcNow.Ticks;
            AccountId  = owner;
            SaveNeeded = true;

            if (instantSave)
                Save();

            if (AccountId != 0)
                InfoManager.AddScroll(this);
        }

        public ScrollInstance(ulong id, ScrollTemplate scrollTemplate, byte level, long timestamp, bool isTradable, uint owner)
        {
            Id        = id;
            Scroll    = scrollTemplate;
            Timestamp = timestamp;
            Tradable  = isTradable;
            AccountId = owner;
        }

        public void Save()
        {
            // only save player owned scrolls that have been modified
            if (!SaveNeeded || AccountId == 0)
                return;

            SaveNeeded = false;

            string query = "INSERT INTO `scroll_instance` (`id`, `accountId`, `scrollEntry`, `level`, `timestamp`, `damage`, `destroyed`, `heal`, " +
                "`idolKills`, `played`, `sacrificed`, `totalGames`, `unitKills`, `wins`, `tradable`) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?) " +
                "ON DUPLICATE KEY UPDATE `id` = VALUES(`id`), `accountId` = VALUES(`accountId`), `scrollEntry` = VALUES(`scrollEntry`), " +
                "`level` = VALUES(`level`), `timestamp` = VALUES(`timestamp`), `damage` = VALUES(`damage`), `destroyed` = VALUES (`destroyed`), " +
                "`heal` = VALUES(`heal`), `idolKills` = VALUES(`idolKills`), `played` = VALUES(`played`), `sacrificed` = VALUES (`sacrificed`), " +
                "`totalGames` = VALUES(`totalGames`), `unitKills` = VALUES(`unitKills`), `wins` = VALUES(`wins`), `tradable` = VALUES(`tradable`);";

            DatabaseManager.Database.Execute(query, Id, AccountId, Scroll.Entry, Level, Timestamp, Stats.Damage, Stats.Destroyed, Stats.Heal,
                Stats.IdolKills, Stats.Played, Stats.Sacrificed, Stats.TotalGames, Stats.UnitKills, Stats.Wins, Tradable);
        }
    }
}
