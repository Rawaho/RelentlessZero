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

namespace RelentlessZero.Entities
{
    public class AvatarPartTemplate
    {
        [JsonProperty(PropertyName = "id")]
        public ushort Entry { get; set; }
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AvatarPartRarity Type { get; set; }
        [JsonProperty(PropertyName = "part")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AvatarPartName Part { get; set; }
        [JsonProperty(PropertyName = "filename")]
        public string Filename { get; set; }
        [JsonProperty(PropertyName = "set")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AvatarPartSet Set { get; set; }
    }
}
