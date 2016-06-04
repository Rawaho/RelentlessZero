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

using RelentlessZero.Network;

namespace RelentlessZero.Entities
{
    [Scroll(232, TileSearchType.TileSelf)]
    public class AgingKnight : Unit
    {
        public AgingKnight(BattleSide owner, ScrollInstance scrollInstance, byte positionX, byte positionY, PacketEffectWriter newEffects = null)
            : base(owner, scrollInstance, positionX, positionY, newEffects) { }

        public override void OnAttack(Unit victim, uint damage, DamageType type, bool victimKilled) { MaxCooldown++; }
    }
}
