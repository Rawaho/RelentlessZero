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
using System.Collections.Generic;

namespace RelentlessZero.Entities
{
    // custom serialiser for scroll tags
    public class JsonTagSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (var scrollTag in (List<ScrollTag>)value)
            {
                writer.WritePropertyName(scrollTag.Name);

                switch (scrollTag.Type)
                {
                    case TagType.Bool:
                        writer.WriteValue(Convert.ToBoolean(scrollTag.Value));
                        break;
                    case TagType.Float:
                        writer.WriteValue(Convert.ToSingle(scrollTag.Value));
                        break;
                    case TagType.Int:
                        writer.WriteValue(Convert.ToInt32(scrollTag.Value));
                        break;
                    default:
                        writer.WriteValue(scrollTag.Value);
                        break;
                }
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // scroll tags are sent by server and should never be recieved from client
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(List<ScrollTag>).IsAssignableFrom(objectType);
        }
    }

    // custom serialiser for scoll abilities
    public class JsonAbilitySerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            foreach (var abilityTemplate in (List<AbilityTemplate>)value)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("id");
                writer.WriteValue(abilityTemplate.Id);
                writer.WritePropertyName("name");
                writer.WriteValue(abilityTemplate.Name);
                writer.WritePropertyName("description");
                writer.WriteValue(abilityTemplate.Description);
                writer.WritePropertyName("cost");
                writer.WriteStartObject();

                foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
                {
                    if (resource == ResourceType.NONE || resource == ResourceType.CARDS)
                        continue;

                    writer.WritePropertyName(resource.ToString().ToUpper());
                    writer.WriteValue(resource == abilityTemplate.Resource ? abilityTemplate.Cost : 0);
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // scroll abilities are sent by server and should never be recieved from client
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(List<AbilityTemplate>).IsAssignableFrom(objectType);
        }
    }

    // custom serialiser
    public class AbilityTemplate
    {
        public ushort Entry { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ResourceType Resource { get; set; }
        public byte Cost { get; set; }
    }

    public class PassiveTemplate
    {
        [JsonIgnoreAttribute()]
        public ushort Entry { get; set; }
        [JsonProperty(PropertyName = "displayName")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }

    // not sent to client
    public class TagTemplate
    {
        public ushort Entry { get; set; }
        public string Name { get; set; }
        public TagType Type { get; set; }
    }

    // custom serialiser
    public class ScrollTag
    {
        public string Name { get; set; }
        public TagType Type { get; set; }
        public string Value { get; set; }
    }

    public class ScrollTemplate
    {
        [JsonProperty(PropertyName = "id")]
        public ushort Entry { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "flavor")]
        public string Flavor { get; set; }
        [JsonProperty(PropertyName = "subTypesStr")]
        public string SubTypesStr { get; set; }
        [JsonProperty(PropertyName = "subTypes", ItemConverterType = typeof(StringEnumConverter))]
        public List<ScrollSubType> SubTypes { get; set; }
        [JsonProperty(PropertyName = "kind")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScrollKind Kind { get; set; }
        [JsonProperty(PropertyName = "rarity")]
        public byte Rarity { get; set; }
        [JsonProperty(PropertyName = "hp")]
        public byte Health { get; set; }
        [JsonProperty(PropertyName = "ap")]
        public byte Attack { get; set; }
        [JsonProperty(PropertyName = "ac")]
        public sbyte Cooldown { get; set; }
        [JsonIgnoreAttribute()]
        public ResourceType Resource { get; set; }
        [JsonIgnoreAttribute()]
        public byte Cost { get; set; }
        [JsonProperty(PropertyName = "costDecay")]
        public byte CostDecay { get; set; }
        [JsonProperty(PropertyName = "costOrder")]
        public byte CostOrder { get; set; }
        [JsonProperty(PropertyName = "costGrowth")]
        public byte CostGrowth { get; set; }
        [JsonProperty(PropertyName = "costEnergy")]
        public byte CostEnergy { get; set; }
        [JsonProperty(PropertyName = "set")]
        public byte Set { get; set; }
        [JsonProperty(PropertyName = "limitedWeight")]
        public float LimitedWeight { get; set; }
        [JsonProperty(PropertyName = "tags")]
        [JsonConverter(typeof(JsonTagSerializer))]
        public List<ScrollTag> Tags { get; set; }
        [JsonProperty(PropertyName = "cardImage")]
        public ushort CardImage { get; set; }
        [JsonProperty(PropertyName = "animationPreviewImage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ushort AnimationImage { get; set; }
        [JsonProperty(PropertyName = "animationPreviewInfo", NullValueHandling = NullValueHandling.Ignore)]
        public string AnimationInfo { get; set; }
        [JsonProperty(PropertyName = "animationBundle", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ushort AnimationBundle { get; set; }
        [JsonProperty(PropertyName = "abilities")]
        [JsonConverter(typeof(JsonAbilitySerializer))]
        public List<AbilityTemplate> Abilities { get; set; }
        [JsonProperty(PropertyName = "targetArea", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScrollTargetArea TargetArea { get; set; }
        [JsonProperty(PropertyName = "passiveRules")]
        public List<PassiveTemplate> Passives { get; set; }
        [JsonProperty(PropertyName = "available")]
        public bool Available { get; set; }

        public ScrollTemplate()
        {
            // default values
            SubTypesStr = "";
            CostDecay   = 0;
            CostOrder   = 0;
            CostGrowth  = 0;
            CostEnergy  = 0;
            TargetArea  = ScrollTargetArea.NONE;
            Available   = true;

            // data stores
            SubTypes    = new List<ScrollSubType>();
            Tags        = new List<ScrollTag>();
            Abilities   = new List<AbilityTemplate>();
            Passives    = new List<PassiveTemplate>();
        }
    }
}
