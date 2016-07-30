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
using System;
using System.Collections.Generic;
using System.Text;

namespace RelentlessZero.Entities
{
    // custom serialiser for deck
    public class JsonDeckSerialiser : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var deck = (Deck)value;

            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteValue(deck.Name);
            writer.WritePropertyName("resources");
            writer.WriteValue(deck.ResourceString);
            writer.WritePropertyName("valid");
            writer.WriteValue(!deck.HasFlag(DeckFlags.Invalid));
            writer.WritePropertyName("updated");
            writer.WriteValue(deck.Age);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // decks are sent by server and should never be recieved from client
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ScrollTemplate).IsAssignableFrom(objectType);
        }
    }

    // custom serialiser
    [JsonConverter(typeof(JsonDeckSerialiser))]
    public class Deck
    {
        public uint Id { get; }
        public string Name { get; }
        public ulong Timestamp { get; set; }
        public string Age { get; set; }
        public List<ResourceType> Resources { get; } = new List<ResourceType>();
        public string ResourceString { get; set; }
        public DeckFlags Flags { get; }
        public List<ScrollInstance> Scrolls { get; set; }

        private uint owner;

        public Deck(uint owner, uint id, string name, ulong timestamp, DeckFlags flags)
        {
            Id         = id;
            Name       = name;
            Timestamp  = timestamp;
            Flags      = flags;
            Scrolls    = new List<ScrollInstance>();

            this.owner = owner;

            CalculateAge();
        }

        public bool HasFlag(DeckFlags flag) { return (Flags & flag) != 0; }
        public bool HasResource(ResourceType resource) { return Resources.Contains(resource); }

        public void Save()
        {
            var transaction = new DatabaseManager.Transaction();
            transaction.AddStatement(PreparedStatement.DeckInsert, Id, owner, Name, Timestamp, Flags);
            transaction.AddStatement(PreparedStatement.DeckScrollDelete, Id);

            foreach (var scroll in Scrolls)
                transaction.AddStatement(PreparedStatement.DeckScrollInsert, Id, owner, scroll.Id);

            transaction.Commit();
        }

        public void Delete()
        {
            // foreign key will remove scroll information for deck
            DatabaseManager.ExecutePreparedStatement(PreparedStatement.DeckDelete, Id, owner);
        }

        public void CalculateAge()
        {
            Age = string.Empty;

            var span = DateTime.UtcNow - new DateTime((long)Timestamp);
            // X year(s) ago
            if (span.TotalDays >= 365)
            {
                int years = (int)span.TotalDays / 365;
                Age = string.Format("{0} year{1}", years, years > 1 ? "s" : string.Empty);
            }
            // X month(s) ago
            else if (span.TotalDays >= 60)
            {
                int months = (int)span.TotalDays / 30;
                Age = string.Format("{0} month{1}", months, months > 1 ? "s" : string.Empty);
            }
            // X week(s) ago
            else if (span.TotalDays >= 7)
            {
                int weeks = (int)span.TotalDays / 7;
                Age += string.Format("{0} week{1}", weeks, weeks > 1 ? "s" : string.Empty);
            }
            // X day(s) ago
            else if ((int)span.TotalDays >= 1)
            {
                int days = (int)span.TotalDays;
                Age += string.Format("{0} day{1}", days, days > 1 ? "s" : string.Empty);
            }
            // X hour(s) ago
            else if ((int)span.TotalHours >= 1)
            {
                int hours = (int)span.TotalHours;
                Age += string.Format("{0} hour{1}", hours, hours > 1 ? "s" : string.Empty);
            }
            // X minintes ago
            else if ((int)span.TotalMinutes >= 1)
            {
                int minutes = (int)span.TotalMinutes;
                Age += string.Format("{0} minute{1}", minutes, minutes > 1 ? "s" : string.Empty);
            }

            Age += string.Format("{0} ago", string.IsNullOrEmpty(Age) ? "moments" : string.Empty);
        }

        public void CalculateResources()
        {
            Resources.Clear();
            foreach (var card in Scrolls)
                if (!HasResource(card.Scroll.Resource))
                    Resources.Add(card.Scroll.Resource);

            // calculate resource string
            var stringBuilder = new StringBuilder();
            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                if (HasResource(resourceType))
                {
                    stringBuilder.Append(resourceType.ToString());
                    if (resourceType != ResourceType.Count)
                        stringBuilder.Append(",");
                }
            }

            ResourceString = stringBuilder.ToString();
        }
    }
}
