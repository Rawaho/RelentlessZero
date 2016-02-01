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
using Newtonsoft.Json.Converters;
using RelentlessZero.Entities;
using System;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace RelentlessZero.Network
{
    // custom serialiser for new effects packet
    public class JsonPacketNewEffectsSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var packetNewEffects = (PacketNewEffects)value;

            writer.WriteStartObject();
            writer.WritePropertyName("effects");
            writer.WriteStartArray();

            foreach (var newEffect in packetNewEffects.Effects)
            {
                var effectAttribute = newEffect.GetType().GetCustomAttribute<PacketEffectAttribute>();
                Contract.Requires(effectAttribute != null, "Defined PacketEffect doesn't have PacketEffect attribute!");

                writer.WriteStartObject();
                writer.WritePropertyName(effectAttribute.Name);
                serializer.Serialize(writer, newEffect);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("msg");
            writer.WriteValue("NewEffects");
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // new effects packets is sent by server and should never be received from client
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(PacketEffect).IsAssignableFrom(objectType);
        }
    }

    public abstract class PacketEffect { }

    [PacketEffect("EndGame")]
    public class PacketEndGameEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "winner")]
        public TileColour Winner { get; set; }
        [JsonProperty(PropertyName = "whiteStats")]
        public PlayerStats WhiteStats { get; set; }
        [JsonProperty(PropertyName = "blackStats")]
        public PlayerStats BlackStats { get; set; }
        [JsonProperty(PropertyName = "whiteGoldReward")]
        public GoldReward WhiteGoldReward { get; set; }
        [JsonProperty(PropertyName = "blackGoldReward")]
        public GoldReward BlackGoldReward { get; set; }
    }

    [PacketEffect("IdolUpdate")]
    public class PacketIdolUpdateEffect : PacketEffect
    {
        public PacketIdolUpdateEffect(Idol idol) { Idol = idol; }

        [JsonProperty(PropertyName = "idol")]
        public Idol Idol { get; set; }
    }

    [PacketEffect("MulliganDisabled")]
    public class PacketMulliganDisabledEffect : PacketEffect
    {
        public PacketMulliganDisabledEffect(TileColour colour) { Colour = colour; }

        [JsonProperty(PropertyName = "color")]
        public TileColour Colour { get; set; }
    }

    [PacketEffect("SurrenderEffect")]
    public class PacketSurrenderEffect : PacketEffect
    {
        public PacketSurrenderEffect(TileColour colour) { Colour = colour; }

        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TileColour Colour { get; set; }
    }
}
