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
using RelentlessZero.Managers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RelentlessZero.Network
{
    public class ConnectionListener
    {
        public bool IsListening { get; set; }
        public TcpListener TcpListener { get; set; }
        public SessionType SessionType { get; set; }

        public ConnectionListener(int port, SessionType sessionType)
        {
            TcpListener = new TcpListener(IPAddress.Any, port);
            TcpListener.Start();

            SessionType = sessionType;
            IsListening = true;
        }
    }

    public static class ConnectManager
    {
        private static List<ConnectionListener> listenerList;

        public static bool Initialise()
        {
            listenerList = new List<ConnectionListener>();

            try
            {
                listenerList.Add(new ConnectionListener(ConfigManager.Config.Network.LookupPort, SessionType.Lookup));
                listenerList.Add(new ConnectionListener(ConfigManager.Config.Network.LobbyPort, SessionType.Lobby));
                listenerList.Add(new ConnectionListener(ConfigManager.Config.Network.BattlePort, SessionType.Battle));

                new Thread(AcceptConnection).Start();

                LogManager.Write("Connect Manager", "Listening for connections on ports, {0}, {1} and {2}.",
                    ConfigManager.Config.Network.LookupPort, ConfigManager.Config.Network.LobbyPort, ConfigManager.Config.Network.BattlePort);

                return true;
            }
            catch (Exception exception)
            {
                LogManager.Write("Connect Manager", "An exception occured while initialising TCP listener!");
                LogManager.Write("Connect Manager", "Exception: {0}", exception.Message);
                return false;
            }
        }

        private static async void AcceptConnection()
        {
            for(;;)
            {
                foreach (var listener in listenerList)
                {
                    Thread.Sleep(1);
                    if (listener.TcpListener.Pending())
                        new Session(await listener.TcpListener.AcceptSocketAsync(), listener.SessionType);
                }
            }
        }
    }
}
