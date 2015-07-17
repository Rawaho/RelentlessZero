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
using System.Collections.Generic;
namespace RelentlessZero.Entities
{


    public class GoldReward
    {
        [JsonProperty(PropertyName = "matchReward")]
        public uint MatchReward { get; set; }
        [JsonProperty(PropertyName = "tierMatchReward")]
        public uint TierMatchReward { get; set; }
        [JsonProperty(PropertyName = "matchCompletionReward")]
        public uint MatchCompletionReward { get; set; }
        [JsonProperty(PropertyName = "idolsDestroyedReward")]
        public uint IdolsDestroyedReward { get; set; }
        [JsonProperty(PropertyName = "betReward")]
        public uint BetReward { get; set; }
    }

    public class PlayerStats
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "idolDamage")]
        public uint IdolDamage { get; set; }
        [JsonProperty(PropertyName = "unitDamage")]
        public uint UnitDamage { get; set; }
        [JsonProperty(PropertyName = "unitsPlayed")]
        public uint UnitsPlayed { get; set; }
        [JsonProperty(PropertyName = "spellsPlayed")]
        public uint SpellsPlayed { get; set; }
        [JsonProperty(PropertyName = "enchantmentsPlayed")]
        public uint EnchantmentsPlayed { get; set; }
        [JsonProperty(PropertyName = "scrollsDrawn")]
        public uint ScrollsDrawn { get; set; }
        [JsonProperty(PropertyName = "totalMs")]
        public uint TotalMs { get; set; }
        [JsonProperty(PropertyName = "mostDamageUnit")]
        public uint MostDamageUnit { get; set; }
    }

    public class Idol
    {
        public Idol()
        {
            Hp = MaxHp = 10;
        }
        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PlayerColor Color { get; set; }
        [JsonProperty(PropertyName = "position")]
        public int position { get; set; }
        [JsonProperty(PropertyName = "hp")]
        public int Hp { get; set; }
        [JsonProperty(PropertyName = "maxHp")]
        public int MaxHp { get; set; }
    }

    public class BattleSide
    {
        // TODO : complete
        public BattleSide(uint playerId, string playerName, PlayerColor color)
        {
            Color = color;
            PlayerName = playerName;
            PlayerId = playerId;
            GoldReward = new GoldReward();
            PlayerStats = new PlayerStats();
            Idols = new Idol[5];
            for (int i = 0; i < 5; ++i)
            {
                Idol idol = new Idol();
                idol.Color = Color;
                idol.position = i;
                Idols[i] = idol;
            }
        }

        public uint PlayerId { get; set; }
        public string PlayerName { get; set; }
        public PlayerColor Color { get; set; }
        public GoldReward GoldReward { get; set; }
        public PlayerStats PlayerStats { get; set; }
        public BattleSide OpponentSide { get; set; }
        public Idol[] Idols { get; set; }
    }

    public class Battle
    {
        // TODO : complete
        public Battle()
        {
            Phase = BattlePhase.Init;
            RoundTimeSeconds = -1;
            RewardForIdolKill = 10;
        }

        public BattleSide FindSideByUsername(string username)
        {
            if (WhiteSide.PlayerName == username)
                return WhiteSide;
            else
                return BlackSide;
        }

        public uint RewardForIdolKill { get; set; }
        public BattlePhase Phase { get; set; }
        public BattleType Type { get; set; }
        public int RoundTimeSeconds { get; set; }

        public BattleSide WhiteSide { set; get; }
        public BattleSide BlackSide { set; get; }
    }
}
