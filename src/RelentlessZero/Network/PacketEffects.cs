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
using RelentlessZero.Managers;
using System;
using System.Collections.Generic;
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

    public class PacketEffectWriter
    {
        private Dictionary<uint, PacketNewEffects> newEffects;

        public PacketEffectWriter(params uint[] ids)
        {
            newEffects = new Dictionary<uint, PacketNewEffects>();

            // only build new effects for players
            foreach (uint id in ids)
                if (id != 0)
                    newEffects.Add(id, new PacketNewEffects());
        }

        public void AddEffect(PacketEffect effect)
        {
            if (effect == null)
                return;

            foreach (var newEffect in newEffects)
                newEffect.Value.Effects.Add(effect);
        }

        public void AddEffect(PacketEffect effect, uint id)
        {
            if (effect == null)
                return;

            if (newEffects.ContainsKey(id))
                newEffects[id].Effects.Add(effect);
        }

        public void Send()
        {
            foreach (var newEffect in newEffects)
                if (newEffect.Value.Effects.Count != 0)
                    WorldManager.Send(newEffect.Value, newEffect.Key);
        }
    }

    public abstract class PacketEffect { }

    [PacketEffect("CardSacrificed")]
    public class PacketCardSacrificedEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        public TileColour Colour { get; }
        [JsonProperty(PropertyName = "resource")]
        public ResourceType Resource { get; }

        public PacketCardSacrificedEffect(TileColour colour, ResourceType resource)
        {
            Colour   = colour;
            Resource = resource;
        }
    }

    [PacketEffect("CardStackUpdate")]
    public class PacketCardStackUpdateEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        public TileColour Colour { get; }
        [JsonProperty(PropertyName = "librarySize")]
        public int LibrarySize { get; }
        [JsonProperty(PropertyName = "graveyardSize")]
        public int GraveyardSize { get; }

        public PacketCardStackUpdateEffect(TileColour colour, int librarySize, int graveyardSize)
        {
            Colour        = colour;
            LibrarySize   = librarySize;
            GraveyardSize = graveyardSize;
        }
    }

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

    [PacketEffect("HandUpdate")]
    public class PacketHandUpdateEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "maxScrollsForCycle")]
        public uint SacrificeLimit { get; set; }
        [JsonProperty(PropertyName = "cards")]
        public List<ScrollInstance> Scrolls { get; set; }
    }

    [PacketEffect("IdolUpdate")]
    public class PacketIdolUpdateEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "idol")]
        public Idol Idol { get; }

        public PacketIdolUpdateEffect(Idol idol) { Idol = idol; }
    }

    [PacketEffect("MulliganDisabled")]
    public class PacketMulliganDisabledEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        public TileColour Colour { get; }

        public PacketMulliganDisabledEffect(TileColour colour) { Colour = colour; }
    }

    [PacketEffect("ResourcesUpdate")]
    public class PacketResourcesUpdateEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "blackAssets")]
        PacketSideAssets BlackAssets { get; }
        [JsonProperty(PropertyName = "whiteAssets")]
        PacketSideAssets WhiteAssets { get; }

        public PacketResourcesUpdateEffect(PacketSideAssets blackAssets, PacketSideAssets whiteAssets)
        {
            BlackAssets = blackAssets;
            WhiteAssets = whiteAssets;
        }
    }

    [PacketEffect("SurrenderEffect")]
    public class PacketSurrenderEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TileColour Colour { get; }

        public PacketSurrenderEffect(TileColour colour) { Colour = colour; }
    }

    [PacketEffect("TurnBegin")]
    public class PacketTurnBeginEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        public TileColour Colour { get; }
        [JsonProperty(PropertyName = "turn")]
        public uint Turn { get; }
        [JsonProperty(PropertyName = "secondsLeft")]
        public int SecondsLeft { get; }

        public PacketTurnBeginEffect(TileColour colour, uint turn, int secondsLeft)
        {
            Colour      = colour;
            Turn        = turn;
            SecondsLeft = secondsLeft;
        }
    }
}
