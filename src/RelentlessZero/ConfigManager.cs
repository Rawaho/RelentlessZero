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
using RelentlessZero.Entities;
using RelentlessZero.Logging;
using System;
using System.IO;

namespace RelentlessZero
{
    public class ConfigServer
    {
        public string Version { get; set; }
        public string Name { get; set; }
    }

    public class ConfigNetwork
    {
        public string Host { get; set; }
        public int LookupPort { get; set; }
        public int LobbyPort { get; set; }
        public int BattlePort { get; set; }
        public int MaxConnections { get; set; }
        public ConfigMySql MySql { get; set; }
        public ConfigAssets Assets { get; set; }
    }

    public class ConfigMySql
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ConfigAssets
    {
        public bool Enable { get; set; }
        public int Port { get; set; }
        public string Directory { get; set; }
    }

    public class ConfigDefaultRoom
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public AdminRole Permission { get; set; }
        public bool Incremental { get; set; }
        public bool AutoJoin { get; set; }
        public bool RoomList { get; set; }
    }

    public class ConfigRoom
    {
        public uint MaxRooms { get; set; }
        public uint MaxPlayersPerRoom { get; set; }
        public uint MaxRoomPasswordLength { get; set; }
        public ConfigDefaultRoom[] DefaultRooms { get; set; }
    }

    public class Config
    {
        public ConfigServer Server { get; set; }
        public ConfigNetwork Network { get; set; }
        public ConfigRoom Room { get; set; }
    }

    public static class ConfigManager
    {
        public static Config Config;

        public static void Initialise()
        {
            try
            {
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(@".\Config.json"));
            }
            catch (Exception exception)
            {
                LogManager.Write("Configuration Manager", "An exception occured while loading the configuration file!");
                LogManager.Write("Configuration Manager", "Exception: {0}", exception.Message);
                Environment.Exit(0);
            }
        }
    }
}
