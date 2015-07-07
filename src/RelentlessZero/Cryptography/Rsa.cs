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

using RelentlessZero.Logging;
using System;
using System.Security.Cryptography;
using System.Text;

namespace RelentlessZero.Cryptography
{
    public static class Rsa
    {
        private static string privateKey;
        private static RSACryptoServiceProvider cryptographyProvider;

        static Rsa()
        {
            // for testing purposes only, it's recommended that you change the public, private key combination
            privateKey = "<RSAKeyValue><Modulus>znUM9M9Eu1oOXjpQ0TT3lNFqa2ybh/18yCPZNlYG+7mtd8xsyl63Ckx/iXCZOHA5FnLyvAN5qHPGh1U8IUY3E0IY9+t5T5/B+ZyHCMjcTehoK8CEfvDLAWIqBXequQ4BlVu1uojrUZtCYXoWaN4Lzg7CHO9e63iLPDvXzA1YZWM=</Modulus><Exponent>AQAB</Exponent><P>6SXAp4FySGb/WPdEA7j3PTpuqxv+xbavxSTdWwrp+7YBMegbWLT4eb07oSkV9tB7B9Va4zdYPPMVQv8G7Qsgjw==</P><Q>4rGS44J5q7ceN/FukMILWgants/QVOLeOWSwxL50y98mKJHvi2l0ee6bCGJpLzw2tLtm1eEPOsA+gpvE5l8v7Q==</Q><DP>6Kcppn2AA0v0h1zHXEeZQFos0UbwZ2+z2rC8yvsLHokyjBCoKU79nM3+0QVky5SjEOARACfcP4X6s4415RlzdQ==</DP><DQ>KF+n4Y0jTknWv+/n2TA2BVqaFBz+5Z9iJKaiNOgWtRiRGziiiRnG/NBaHCwqv5bhx154+i6TH8uNVoWXk8/wIQ==</DQ><InverseQ>R8Lb9Ry7fJzA5BnuqaJMeQVamfqbahse5zZfl/yQ9w/kk8xShKloJBzSAraF4sPT2ly/zCXYuT7PyWiAJuX7ag==</InverseQ><D>m//1+kVjOe2iTbDiwDG1oc1bDDDUlL0FPHVZ+6SlQi/a01q0GXXX+QA0njVmkS669CuHHmnB03cn27qb5MiZc6hXkU0RZm6bMfnVem251p9LWOpRlQvT8cylw4ezz/LGm5hy8RUlfODk758EV/Wf3lZ81N7+CWBZxF0TWshrDYE=</D></RSAKeyValue>";

            cryptographyProvider = new RSACryptoServiceProvider();
            cryptographyProvider.FromXmlString(privateKey);
        }

        public static string Decrypt(string data)
        {
            if (string.IsNullOrEmpty(data))
                return "";

            try
            {
                return Encoding.UTF8.GetString(cryptographyProvider.Decrypt(Convert.FromBase64String(data), false));
            }
            catch (Exception exception)
            {
                LogManager.Write("Cryptography", "Failed to decrypt RSA data!");
                LogManager.Write("Cryptography", "Exception: {0}", exception.Message);
                return "";
            }
        }
    }
}
