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

using Newtonsoft.Json;
using RelentlessZero.Logging;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace RelentlessZero.Network
{
    public static class PacketManager
    {
        private static ConcurrentDictionary<string, PacketInfo> clientToServerPackets;
        private static ConcurrentDictionary<Type, PacketInfo> serverToClientPackets;

        private static ConcurrentDictionary<string, PacketHandler> packetHandlers;
        delegate void PacketHandler(object packet, Session session);

        private const uint MAX_INCOMING_PACKET_SIZE = 8192u;

        public class PacketInfo
        {
            public string Name { get; set; }
            public PacketDirection Direction { get; set; }
            public SessionType SessionType { get; set; }
            public bool AuthRequired { get; set; }
            public Type Type { get; set; }
        }

        public static void Initialise()
        {
            DefinePackets();
            DefineHandlers();
        }

        private static void DefinePackets()
        {
            clientToServerPackets = new ConcurrentDictionary<string, PacketInfo>();
            serverToClientPackets = new ConcurrentDictionary<Type, PacketInfo>();

            uint packetCount = 0u;
            DateTime startTime = DateTime.Now;

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (var packetAttribute in type.GetCustomAttributes<PacketAttribute>())
                {
                    var packetInfo = new PacketInfo()
                    {
                        Name         = packetAttribute.Name,
                        Direction    = packetAttribute.Direction,
                        SessionType  = packetAttribute.SessionType,
                        AuthRequired = packetAttribute.AuthRequired,
                        Type         = type
                    };

                    if (packetAttribute.HasDirection(PacketDirection.ClientToServer))
                        if (!clientToServerPackets.ContainsKey(packetAttribute.Name))
                            clientToServerPackets[packetAttribute.Name] = packetInfo;

                    if (packetAttribute.HasDirection(PacketDirection.ServerToClient))
                        if (!serverToClientPackets.ContainsKey(type))
                            serverToClientPackets[type] = packetInfo;

                    packetCount++;
                }
            }

            LogManager.Write("Packet Manager", "Initialised {0} packet(s) in {1} milliseconds(s).", packetCount, (DateTime.Now - startTime).Milliseconds);
        }

        private static void DefineHandlers()
        {
            packetHandlers = new ConcurrentDictionary<string, PacketHandler>();

            uint handlerCount = 0u;
            DateTime startTime = DateTime.Now;

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (MethodInfo methodInfo in type.GetMethods())
                {
                    foreach (var packetHandlerAttribute in methodInfo.GetCustomAttributes<PacketHandlerAttribute>())
                    {
                        packetHandlers[packetHandlerAttribute.Name] = (PacketHandler)Delegate.CreateDelegate(typeof(PacketHandler), methodInfo);
                        handlerCount++;
                    }
                }
            }

            LogManager.Write("Packet Manager", "Initialised {0} packet handler(s) in {1} millisecond(s).", handlerCount, (DateTime.Now - startTime).Milliseconds);
        }

        // outgoing packets are checked here before being converted to JSON and sent to the client
        public static bool HandleOutgoingPacket(ref object packet, Session session)
        {
            string sessionString = session.Player != null ? session.Player.Username : session.IpAddress;
            Type type = packet.GetType();

            PacketInfo packetInfo;
            if (!serverToClientPackets.TryGetValue(type, out packetInfo))
            {
                LogManager.Write("Packet Manager", "Invalid outgoing packet type {0}!", type.Name);
                return false;
            }

            if (!HasSessionType(session.Type, packetInfo.SessionType))
            {
                LogManager.Write("Packet Manager", "Failed to send packet {0}, {1} has invalid session type {2}!",
                    packetInfo.Name, sessionString, session.Type.ToString());
                return false;
            }

            if (session.Player == null && packetInfo.AuthRequired)
            {
                LogManager.Write("Packet Manager", "Failed to send packet {0}, {1} isn't authenticated!", packetInfo.Name, sessionString);
                return false;
            }

            var packetHeader = (PacketHeader)packet;
            packetHeader.Msg = packetInfo.Name;

            #if DEBUG
            if (packetHeader.Msg != "Ping")
                LogManager.Write("Packet Manager", "DEBUG: Sent packet {0} to {1}.", packetHeader.Msg, sessionString);
            #endif

            return true;
        }

        // raw JSON packets are checked here before invoking it's handler
        public static bool HandleRawPacket(Session session, string json)
        {
            DateTime startTime = DateTime.Now;
            string sessionString = session.Player != null ? session.Player.Username : session.IpAddress;

            if (json.Length > MAX_INCOMING_PACKET_SIZE)
            {
                LogManager.Write("Packet Manager", "Received malformed packet (too large: {0} > {1}) from {2}!",
                    json.Length, MAX_INCOMING_PACKET_SIZE, sessionString);
                return false;
            }

            PacketHeader packetHeader;
            try
            {
                packetHeader = JsonConvert.DeserializeObject<PacketHeader>(json);
            }
            catch (Exception exception)
            {
                LogManager.Write("Packet Manager", "An exception occurred while deserialising incoming data (header) from {0}!", sessionString);
                LogManager.Write("Packet Manager", "Exception: {0}", exception.Message);
                return false;
            }

            if (string.IsNullOrEmpty(packetHeader.Msg))
            {
                LogManager.Write("Packet Manager", "Received malformed packet (missing opcode) from {0}!", sessionString);
                return false;
            }

            #region Debug
            /*if (packetHeader.msg != "Ping")
                LogManager.Write("Packet Manager", "DEBUG: Received packet {0} from {1}.", packetHeader.msg, sessionString);*/
            #endregion

            if (!clientToServerPackets.ContainsKey(packetHeader.Msg))
            {
                LogManager.Write("Packet Manager", "Received unknown packet {0} from {1}!", packetHeader.Msg, sessionString);
                return false;
            }

            if (!packetHandlers.ContainsKey(packetHeader.Msg))
            {
                LogManager.Write("Packet Manager", "Received unhandled packet {0} from {1}.", packetHeader.Msg, sessionString);
                return false;
            }

            PacketHandler packetHandler;
            if (!packetHandlers.TryGetValue(packetHeader.Msg, out packetHandler))
                return false;

            PacketInfo packetInfo;
            if (!clientToServerPackets.TryGetValue(packetHeader.Msg, out packetInfo))
                return false;

            if (!HasSessionType(session.Type, packetInfo.SessionType))
            {
                LogManager.Write("Packet Manager", "Failed to handle packet {0}, {1} has invalid session type {1}!",
                    packetHeader.Msg, sessionString, session.Type.ToString());
                return false;
            }

            if (session.Player == null && packetInfo.AuthRequired)
            {
                LogManager.Write("Packet Manager", "Failed to handle packet {0}, {1} isn't authenticated!", packetHeader.Msg, sessionString);
                return false;
            }

            object packet;
            try
            {
                packet = JsonConvert.DeserializeObject(json, packetInfo.Type);
            }
            catch (Exception exception)
            {
                LogManager.Write("Packet Manager", "An exception occurred while deserialising incoming data (body) from {0}!", sessionString);
                LogManager.Write("Packet Manager", "Exception: {0}", exception.Message);
                return false;
            }

            packetHandler.Invoke(packet, session);

            #region Debug
            if (packetHeader.Msg != "Ping")
                LogManager.Write("Packet Manager", "DEBUG: Handled packet {0} to {1} millisecond(s).", packetHeader.Msg, (DateTime.Now - startTime).Milliseconds);
            #endregion

            return true;
        }

        private static bool HasSessionType(SessionType session, SessionType packetSession)
        {
            return (packetSession & session) != 0;
        }
    }
}
