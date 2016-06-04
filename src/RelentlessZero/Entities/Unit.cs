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
    public class Unit
    {
        public BattleSide Owner { get; }
        public ScrollTemplate Template { get; }

        public byte PositionX { get; set; }
        public byte PositionY { get; set; }

        public uint Health { get; set; }
        public uint MaxHealth { get; set; }
        public uint Attack { get; set; }
        public uint MaxAttack { get; set; }
        public int Cooldown { get; set; }
        public int MaxCooldown { get; set; }

        public Unit(BattleSide owner, ScrollInstance scrollInstance, byte positionX, byte positionY, PacketEffectWriter newEffects = null)
        {
            Owner     = owner;
            Template  = scrollInstance.Scroll;
            PositionX = positionX;
            PositionY = positionY;

            SetHealth(Template.Health, true);
            SetAttack(Template.Attack, true);
            SetCooldown(Template.Cooldown, true);

            if (newEffects != null)
            {
                var summonUnit = new PacketSummonUnitEffect()
                {
                    Scroll = scrollInstance,
                    Target = new EffectTarget(owner.Colour, PositionX, PositionY),
                };

                newEffects.AddEffect(summonUnit);
            }
        }

        public void SetHealth(uint health, bool setMax = false, PacketEffectWriter newEffects = null)
        {
            Health = health;
            if (setMax)
                MaxHealth = health;

            if (newEffects != null)
                StatsUpdate(newEffects);

            if (health == 0)
            {
                // TODO...
            }
        }

        public void SetAttack(uint attack, bool setMax = false, PacketEffectWriter newEffects = null)
        {
            Attack = attack;
            if (setMax)
                MaxAttack = attack;

            if (newEffects != null)
                StatsUpdate(newEffects);
        }

        public void SetCooldown(int cooldown, bool setMax = false, PacketEffectWriter newEffects = null)
        {
            Cooldown = cooldown;
            if (setMax)
                MaxCooldown = cooldown;

            if (newEffects != null)
                StatsUpdate(newEffects);
        }

        public void StatsUpdate(PacketEffectWriter newEffects)
        {
            var statsUpdate = new PacketStatsUpdateEffect()
            {
                Target   = new EffectTarget(Owner.Colour, PositionX, PositionY),
                Health   = Health,
                Attack   = Attack,
                Cooldown = Cooldown
            };

            newEffects.AddEffect(statsUpdate);
        }

        public virtual void OnAttack(Unit victim, uint damage, DamageType type, bool victimKilled) { }
        public virtual void OnDamage(Unit attacker, uint damage, DamageType type) { }
        public virtual void OnDeath(Unit attacker, uint damage, DamageType type) { }
        public virtual void OnDraw(ScrollTemplate scroll) { }
        public virtual void OnMove() { }
        public virtual void OnSummon() { }
    }
}
