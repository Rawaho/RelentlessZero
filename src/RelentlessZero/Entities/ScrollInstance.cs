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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // scroll instances are sent by server and should never be recieved from client
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ScrollTemplate).IsAssignableFrom(objectType);
        }
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
        public const uint MaxLevel = 2;

        public ulong Id { get; set; }
        public ScrollTemplate Scroll { get; set; }
        public byte Level { get; set; }
        public long Timestamp { get; set; }
        public ScrollStats Stats { get; set; }
        public bool Tradable { get; set; }
        public bool SaveNeeded { get; set; }

        public ScrollInstance(ScrollTemplate scrollTemplate)
        {
            Scroll = scrollTemplate;
            Stats  = new ScrollStats();
        }
    }
}
