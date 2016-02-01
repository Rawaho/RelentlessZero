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

using RelentlessZero.Logging;
using System;
using System.Security.Cryptography;
using System.Text;

namespace RelentlessZero.Cryptography
{
    public static class Sha
    {
        private static SHA1Managed sha1Managed;
        private static SHA256Managed sha256Managed;

        static Sha()
        {
            sha1Managed = new SHA1Managed();
            sha256Managed = new SHA256Managed();
        }

        public static string Hash(string data, bool sha256 = false)
        {
            if (string.IsNullOrEmpty(data))
                return "";

            byte[] byteData = Encoding.UTF8.GetBytes(data);

            try
            {
                return BitConverter.ToString(sha256 ? sha256Managed.ComputeHash(byteData) : sha1Managed.ComputeHash(byteData)).Replace("-", "");
            }
            catch (Exception exception)
            {
                LogManager.Write("Cryptography", "Failed to hash data with {0}!", sha256 ? "SHA256" : "SHA1");
                LogManager.Write("Cryptography", "Exception: {0}", exception.Message);
                return "";
            }
        }
    }
}
