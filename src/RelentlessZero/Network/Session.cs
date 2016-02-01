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
using RelentlessZero.Managers;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace RelentlessZero.Network
{
    public enum SessionType
    {
        None   = 0x0,
        Lookup = 0x1,
        Lobby  = 0x2,
        Battle = 0x4
    }

    public class Session
    {
        public SessionType Type { get; set; }
        public Socket Socket { get; set; }
        public string IpAddress { get; set; }
        public Player Player { get; set; }

        private byte[] buffer;

        public Session(Socket socket, SessionType type)
        {
            Socket    = socket;
            Type      = type;
            IpAddress = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            buffer    = new byte[8192];
            
            OnConnect();
        }

        public void OnConnect()
        {
            // send initial server information packet on client connect
            var packetServerInfo = new PacketServerInfo()
            {
                Version  = ConfigManager.Config.Server.Version,
                AssetUrl = "http://download.scrolls.com/assets/",
                NewsUrl  = "http://scrolls.com/news",
                Roles    = "LOOKUP,LOBBY,GAME,RESOURCE"
            };

            Send(packetServerInfo);

            Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, null);
        }

        private void OnDisconnect()
        {
            if (Player != null)
                Player.OnDisconnect();

            WorldManager.RemovePlayerSession(this);

            Socket.Close();
        }

        private void OnReceive(IAsyncResult result)
        {
            try
            {
                int dataLength = Socket.EndReceive(result);
                if (dataLength != 0)
                {
                    // split multiple json packets
                    string[] jsonArray = Regex.Split(Encoding.UTF8.GetString(buffer, 0, dataLength), "}{");
                    for (int i = 0; i < jsonArray.Length; i++)
                    {
                        jsonArray[i] = !jsonArray[i].StartsWith("{") ? "{" + jsonArray[i] : jsonArray[i];
                        jsonArray[i] = !jsonArray[i].EndsWith("}") ? jsonArray[i] + "}" : jsonArray[i];

                        PacketManager.HandleRawPacket(this, jsonArray[i]);
                    }

                    Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, null);
                }
                else
                    OnDisconnect();
            }
            catch (Exception exception)
            {
                LogManager.Write("Session", "Exception: {0}", exception.Message);
                OnDisconnect();
            }
        }

        public void Send(object packet)
        {
            if (packet == null)
                return;

            if (!PacketManager.HandleOutgoingPacket(ref packet, this))
                return;

            try
            {
                byte[] outBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet));
                Socket.Send(outBuffer, 0, outBuffer.Length, SocketFlags.None);
            }
            catch (Exception exception)
            {
                LogManager.Write("Session", "An exception occured while serialising an outgoing packet!");
                LogManager.Write("Session", "Exception: {0}", exception.Message);
            }
        }

        public void SendString(string packet)
        {
            try
            {
                byte[] outBuffer = Encoding.UTF8.GetBytes(packet);
                Socket.Send(outBuffer, 0, outBuffer.Length, SocketFlags.None);
            }
            catch (Exception exception)
            {
                LogManager.Write("Session", "An exception occured while serialising an outgoing packet!");
                LogManager.Write("Session", "Exception: {0}", exception.Message);
            }
        }

        public void SendOkPacket(string origin)
        {
            var ok = new PacketOk()
            {
                Origin = origin
            };

            Send(ok);
        }

        public void SendFailPacket(string origin, string message)
        {
            var fail = new PacketFail()
            {
                Origin = origin,
                Info   = message,
            };

            Send(fail);
        }

        public void SendFatalFailPacket(string origin, string message)
        {
            var fatalFail = new PacketFatalFail()
            {
                Origin = origin,
                Info   = message,
            };

            Send(fatalFail);
        }
    }
}
