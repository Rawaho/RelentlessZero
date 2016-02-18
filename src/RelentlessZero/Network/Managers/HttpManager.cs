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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;

namespace RelentlessZero.Network
{
    public static class HttpManager
    {
        private const int bufferSize = 16384;

        private static HttpListener listener;
        private static string assetDirectory;

        private static Dictionary<string, string> directoryExtensions = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { @"\img",  ".png" },
            { @"\anim", ".zip" }
        };

        private static Dictionary<string, string> mimeTypes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {".png", "image/png"},
            {".zip", "application/zip"}
        };

        public static void Initialise()
        {
            if (!ConfigManager.Config.Network.Assets.Enable)
                return;

            assetDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ConfigManager.Config.Network.Assets.Directory);

            if (!Directory.Exists(assetDirectory))
            {
                LogManager.Write("HTTP Manager", "Failed to start HTTP listener, asset directory doesn't exist!");
                return;
            }

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(string.Format($"http://*:{ConfigManager.Config.Network.Assets.Port}/"));
                listener.Start();
                listener.BeginGetContext(OnConnection, null);
            }
            catch (Exception exception)
            {
                LogManager.Write("HTTP Manager", "An exception occured while initialising HTTP listener!");
                LogManager.Write("HTTP Manager", "Exception: {0}", exception.Message);
                return;
            }

            LogManager.Write("HTTP Manager", $"Listening for HTTP connections on port {ConfigManager.Config.Network.Assets.Port}.");
            return;
        }

        private static void SendResponse(HttpListenerContext context, HttpStatusCode status)
        {
            context.Response.StatusCode = (int)status;

            if (status == HttpStatusCode.OK)
                context.Response.OutputStream.Flush();

            context.Response.OutputStream.Close();
        }

        private static void OnConnection(IAsyncResult ar)
        {
            var context = listener.EndGetContext(ar);
            listener.BeginGetContext(OnConnection, null);

            string filename  = Path.Combine(assetDirectory, context.Request.Url.AbsolutePath.Substring(1));
            string directory = Path.GetDirectoryName(context.Request.Url.AbsolutePath);

            // client requests assets without an extension, add exstension by path
            if (directoryExtensions.ContainsKey(directory))
                filename += directoryExtensions[directory];

            if (!File.Exists(filename))
            {
                SendResponse(context, HttpStatusCode.NotFound);
                return;
            }

            try
            {
                var steam = new FileStream(filename, FileMode.Open);

                string mimeType;
                context.Response.ContentType     = mimeTypes.TryGetValue(Path.GetExtension(filename), out mimeType) ? mimeType : "application/octet-stream";
                context.Response.ContentLength64 = steam.Length;

                byte[] buffer = new byte[bufferSize];

                int byteCount = 0;
                while ((byteCount = steam.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, byteCount);

                steam.Close();

                SendResponse(context, HttpStatusCode.OK);
            }
            catch
            {
                SendResponse(context, HttpStatusCode.InternalServerError);
                return;
            }
        }
    }
}
