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

using RelentlessZero.Cryptography;
using RelentlessZero.Database;
using RelentlessZero.Network;
using RelentlessZero.Logging;
using RelentlessZero.Managers;
using System;
using System.Threading;

namespace RelentlessZero
{
    class RelentlessZero
    {
        static void Main(string[] args)
        {
            ConfigManager.Initialise();
            Console.Title = "RelentlessZero - Server Version: " + ConfigManager.Config.Server.Version;
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            DatabaseManager.Database.Initialise(ConfigManager.Config.Network.MySql.Host, ConfigManager.Config.Network.MySql.Port,
                ConfigManager.Config.Network.MySql.Username, ConfigManager.Config.Network.MySql.Password, ConfigManager.Config.Network.MySql.Database);

            AssetManager.LoadAssets();
            LobbyManager.Initailse();

            if (ConnectManager.Initialise())
                PacketManager.Initialise();

            WorldManager.StartWorldUpdate();
        }
    }
}
