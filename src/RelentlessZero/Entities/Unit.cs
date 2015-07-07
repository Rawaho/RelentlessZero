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

using System.Collections.Generic;

namespace RelentlessZero.Entities
{
    public abstract class Unit
    {
        public virtual bool OnAttack(Unit victim, uint damage, DamageType type, bool victimKilled) { return false; }
        public virtual bool OnDamage(Unit attacker, uint damage, DamageType type) { return false; }
        public virtual bool OnDeath(Unit attacker, uint damage, DamageType type) { return false; }
        public virtual bool OnDraw(ScrollTemplate scroll) { return false; }
        public virtual bool OnMove() { return false; }
    }
}
