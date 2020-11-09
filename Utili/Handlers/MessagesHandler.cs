﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Data;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Program;
using static Utili.MessageSender;

namespace Utili.Handlers
{
    internal class MessagesHandler
    {
        public static async Task MessageReceived(SocketMessage partialMessage)
        {
            _ = Task.Run(async () =>
            {
                SocketUserMessage message = partialMessage as SocketUserMessage;
                SocketTextChannel channel = message.Channel as SocketTextChannel;
                SocketGuild guild = channel.Guild;

                SocketCommandContext context = new SocketCommandContext(_client.GetShardFor(guild), message);

                if (!context.User.IsBot && !string.IsNullOrEmpty(context.Message.Content))
                {
                    string prefix = Misc.GetPrefix(guild.Id);

                    int argPos = 0;
                    if (context.Message.HasStringPrefix(prefix, ref argPos) ||
                        context.Message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                    {
                        IResult result = await _commands.ExecuteAsync(context, argPos, null);

                        // TODO: Log commands

                        if (!result.IsSuccess)
                        {
                            string errorReason = GetCommandErrorReason(result);

                            if (!string.IsNullOrEmpty(errorReason))
                            {
                                await SendFailureAsync(context.Channel, "Error", errorReason);
                            }
                        }
                    }
                }

                // High priority
                try { await _messageLogs.MessageReceived(context); } catch {}
                try { await _messageFilter.MessageReceived(context); } catch {}

                // Low priority
                _ = _voteChannels.MessageReceived(context);
                _ = _inactiveRole.UpdateUserAsync(context.Guild, context.User as SocketGuildUser);

            });
        }

        public static async Task MessageEdited(Cacheable<IMessage, ulong> partialMessage, SocketMessage message, ISocketMessageChannel channel)
        {
            _ = Task.Run(async () =>
            {
                if (message.Author.IsBot || channel is SocketDMChannel) return;

                SocketTextChannel guildChannel = channel as SocketTextChannel;

                SocketCommandContext context = new SocketCommandContext(Helper.GetShardForGuild(guildChannel.Guild), message as SocketUserMessage);

                _ = _messageLogs.MessageEdited(context);
            });
        }

        public static async Task MessageDeleted(Cacheable<IMessage, ulong> partialMessage, ISocketMessageChannel channel)
        {
            _ = Task.Run(async () =>
            {
                if (channel is SocketDMChannel) return;

                SocketTextChannel guildChannel = channel as SocketTextChannel;

                _ = _messageLogs.MessageDeleted(guildChannel.Guild, guildChannel, partialMessage.Id);
            });
        }

        public static async Task MessagesBulkDeleted(IReadOnlyCollection<Cacheable<IMessage, ulong>> messageIds, ISocketMessageChannel channel)
        {
            _ = Task.Run(async () =>
            {
                if (channel is SocketDMChannel) return;

                SocketTextChannel guildChannel = channel as SocketTextChannel;

                _ = _messageLogs.MessagesBulkDeleted(guildChannel.Guild, guildChannel,
                    messageIds.Select(x => x.Id).ToList());
            });
        }

        public static string GetCommandErrorReason(IResult result)
        {
            return result.Error switch
            {
                CommandError.BadArgCount => "Invalid amount of command arguments\nTry wrapping arguments with speech marks",
                CommandError.ObjectNotFound => "Failed to interpret a command argument (Object not found)\nTry wrapping arguments with speech marks",
                CommandError.MultipleMatches => "Failed to interpret a command argument (Multiple matches)\nTry wrapping arguments with speech marks",
                CommandError.UnmetPrecondition => "Invalid command preconditions",
                CommandError.Exception => "An error occured while trying to execute the command",
                _ => null,
            };
        }
    }
}
