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

using RelentlessZero.Entities;
using System;
using System.Collections.Generic;

namespace RelentlessZero
{
    public static class Helper
    {
        public static bool RandomBool() { return new Random().NextDouble() >= 0.5f; }
        public static TileColour RandomColour() { return RandomBool() ? TileColour.black : TileColour.white; }

        public static uint GetUnixTime() { return (uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds; }

        public static void Shuffle<T>(this IList<T> list)
        {
            if (list.Count == 0)
                return;

            var random = new Random();

            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int r = i + (int)(random.NextDouble() * (n - i));
                T value = list[r];
                list[r] = list[i];
                list[i] = value;
            }
        }

        public static T Pop<T>(this IList<T> list)
        {
            if (list.Count == 0)
                return default(T);

            T popped = list[0];
            list.RemoveAt(0);
            return popped;
        }
    }
}
