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
using RelentlessZero.Network;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Threading;

namespace RelentlessZero.Command
{
    public enum CommandResult
    {
        Ok,
        NoCommand,
        NoInvokeFromConsole,
        NoPermission,
        InvalidArguments
    }

    public static class CommandManager
    {
        private static ConcurrentDictionary<string, CommandHandlerAttribute> commandInfoStore;

        delegate void CommandHandler(Session session, params string[] args);
        private static ConcurrentDictionary<string, CommandHandler> commandHandlerStore;

        public static void Initialise()
        {
            var startTime = DateTime.Now;

            commandHandlerStore = new ConcurrentDictionary<string, CommandHandler>(StringComparer.InvariantCultureIgnoreCase);
            commandInfoStore = new ConcurrentDictionary<string, CommandHandlerAttribute>(StringComparer.InvariantCultureIgnoreCase);

            uint handlerCount = 0u;
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (var methodInfo in type.GetMethods())
                {
                    foreach (var commandHandlerAttribute in methodInfo.GetCustomAttributes<CommandHandlerAttribute>())
                    {
                        commandHandlerStore[commandHandlerAttribute.Command] = (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), methodInfo);
                        commandInfoStore[commandHandlerAttribute.Command] = commandHandlerAttribute;
                        handlerCount++;
                    }
                }
            }

            LogManager.Write("Command Manager", $"Initialised {handlerCount} command handler(s) in {(DateTime.Now - startTime).Milliseconds} millisecond(s).");

            StartCommandThread();
        }

        private static void StartCommandThread()
        {
            var thread = new Thread(new ThreadStart(CommandThread));
            thread.Start();
            thread.IsBackground = true;
        }

        private static void CommandThread()
        {
            for (;;)
            {
                Console.Write("RelentlessZero >> ");

                string fullCommand = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(fullCommand))
                    continue;

                string command;
                string[] arguments;
                ParseCommand(fullCommand, out command, out arguments);

                var commandResult = CanInvokeCommand(null, command, arguments);
                if (commandResult != CommandResult.Ok)
                    LogManager.Write("Command Manager", CommandResultError(commandResult, command));
                else
                    HandleCommand(null, command, arguments);
            }
        }

        public static CommandResult CanInvokeCommand(Session session, string command, string[] arguments)
        {
            CommandHandlerAttribute commandInfo;
            if (!commandInfoStore.TryGetValue(command, out commandInfo))
                return CommandResult.NoCommand;

            // session is null if command is being invoked from console
            if (session == null && !commandInfo.Console)
                return CommandResult.NoInvokeFromConsole;

            if (session != null && session.Player.AdminRole < commandInfo.AdminRole)
                return CommandResult.NoPermission;

            if (commandInfo.Arguments != -1 && arguments.Length < commandInfo.Arguments)
                return CommandResult.InvalidArguments;

            return CommandResult.Ok;
        }

        public static string CommandResultError(CommandResult commandResult, string command)
        {
            switch (commandResult)
            {
                case CommandResult.NoCommand:
                    return "Can't invoke non existent command!";
                case CommandResult.NoInvokeFromConsole:
                    return $"Can't invoke {command} from the console!";
                case CommandResult.NoPermission:
                    return $"{command} requires elevated permission!";
                case CommandResult.InvalidArguments:
                    return $"Invalid amount of arguments supplied for {command}!";
                default:
                    return "An internal command error occured!";
            }
        }

        public static void ParseCommand(string fullCommand, out string command, out string[] arguments)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(fullCommand));

            // remove command identifier from commands sent from client
            fullCommand = fullCommand.TrimStart('!');

            var splitCommand = fullCommand.Split(' ');
            command   = splitCommand[0];
            arguments = new string[splitCommand.Length - 1];

            if (splitCommand.Length > 1)
                Array.Copy(splitCommand, 1, arguments, 0, splitCommand.Length - 1);
        }

        public static void HandleCommand(Session session, string command, string[] arguments)
        {
            CommandHandler commandHandler;
            Contract.Assert(commandHandlerStore.TryGetValue(command, out commandHandler));

            commandHandlerStore[command].Invoke(session, arguments);

            if (session != null)
                LogManager.Write("Command Manager", $"Player {session.Player.Id}({session.Player.Username}) invoked command {command}({string.Join(", ", arguments)}).");
        }
    }
}
