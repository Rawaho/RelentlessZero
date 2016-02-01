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

using System;

namespace RelentlessZero.Entities
{
    [Flags]
    public enum PlayerFlags
    {
        None             = 0x00000,
        AcceptTrades     = 0x00001,
        AcceptChallenges = 0x00002,
        SpectateAllow    = 0x00004,
        SpectateHideChat = 0x00008,
        UnlockedDecay    = 0x00010,
        UnlockedOrder    = 0x00020,
        UnlockedEnergy   = 0x00040,
        UnlockedGrowth   = 0x00080,
        BMScrollsSold    = 0x00100,
        HidePlayer       = 0x00200,
        FirstLogin       = 0x00400,
    }
}
