﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Data;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Utili.Extensions;

namespace Utili.Services
{
    public class VoiceLinkService
    {
        private readonly ILogger<VoiceLinkService> _logger;
        private readonly DiscordClientBase _client;

        private List<(ulong, ulong)> _channelsRequiringUpdate;

        public VoiceLinkService(ILogger<VoiceLinkService> logger, DiscordClientBase client)
        {
            _logger = logger;
            _client = client;

            _channelsRequiringUpdate = new List<(ulong, ulong)>();
        }

        public async Task VoiceStateUpdated(VoiceStateUpdatedEventArgs e)
        {
            try
            {
                var row = await VoiceLink.GetRowAsync(e.GuildId);
                if (!row.Enabled) return;
                lock (_channelsRequiringUpdate)
                {
                    if (e.NewVoiceState?.ChannelId is not null &&
                        !row.ExcludedChannels.Contains(e.NewVoiceState.ChannelId.Value))
                        _channelsRequiringUpdate.Add((e.GuildId, e.NewVoiceState.ChannelId.Value));
                    if (e.OldVoiceState?.ChannelId is not null &&
                        !row.ExcludedChannels.Contains(e.OldVoiceState.ChannelId.Value))
                        _channelsRequiringUpdate.Add((e.GuildId, e.OldVoiceState.ChannelId.Value));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown on voice state updated");
            }
        }

        public void Start()
        {
            _ = UpdateLinkedChannelsAsync();
        }

        private async Task UpdateLinkedChannelsAsync()
        {
            while (true)
            {
                try
                {
                    List<(ulong, ulong)> channelsToUpdate;

                    lock (_channelsRequiringUpdate)
                    {
                        channelsToUpdate = _channelsRequiringUpdate.Distinct().ToList();
                        _channelsRequiringUpdate.Clear();
                    }

                    var tasks = channelsToUpdate.Select(x => UpdateLinkedChannelAsync(x.Item1, x.Item2)).ToList();
                    await Task.WhenAll(tasks);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception thrown starting channel updates");
                }

                await Task.Delay(250);
            }
        }

        private Task UpdateLinkedChannelAsync(ulong guildId, ulong channelId)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var guild = _client.GetGuild(guildId);
                    var voiceChannel = guild.GetVoiceChannel(channelId);
                    if (voiceChannel is null)
                    {
                        await CloseLinkedChannelAsync(guild, 
                            await VoiceLink.GetRowAsync(guild.Id), 
                            await VoiceLink.GetChannelRowAsync(guild.Id, channelId));
                        return;
                    }
                    
                    var category = voiceChannel.CategoryId.HasValue ? guild.GetCategoryChannel(voiceChannel.CategoryId.Value) : null;

                    if(!voiceChannel.BotHasPermissions(Permission.ViewChannel)) return;
                    if (category is not null && !category.BotHasPermissions(Permission.ViewChannel | Permission.ManageChannels | Permission.ManageRoles)) return;
                    if (category is null && !guild.BotHasPermissions(Permission.ViewChannel | Permission.ManageChannels | Permission.ManageRoles)) return;

                    var voiceStates = guild.GetVoiceStates().Where(x => x.Value.ChannelId == voiceChannel.Id).Select(x => x.Value).ToList();
                    var connectedUsers = guild.Members.Values.Where(x => voiceStates.Any(y => y.MemberId == x.Id)).ToList();

                    var channelRow = await VoiceLink.GetChannelRowAsync(guild.Id, voiceChannel.Id);
                    var metaRow = await VoiceLink.GetRowAsync(guild.Id);

                    ITextChannel textChannel = guild.GetTextChannel(channelRow.TextChannelId);

                    if (connectedUsers.All(x => x.IsBot))
                    {
                        await CloseLinkedChannelAsync(guild, metaRow, channelRow);
                        return;
                    }

                    if (textChannel is null)
                    {
                        textChannel = await guild.CreateTextChannelAsync($"{metaRow.Prefix.Value}{voiceChannel.Name}", x =>
                        {
                            if (voiceChannel.CategoryId.HasValue) x.CategoryId = voiceChannel.CategoryId.Value;
                            x.Topic = $"Users in {voiceChannel.Name} have access - Created by Utili";
                            x.Overwrites = new List<LocalOverwrite>
                            {
                                LocalOverwrite.Member(_client.CurrentUser.Id, new OverwritePermissions().Allow(Permission.ViewChannel)),
                                LocalOverwrite.Role(guildId, new OverwritePermissions().Deny(Permission.ViewChannel)) // @everyone
                            };
                        }, new DefaultRestRequestOptions{Reason = "Voice Link"});

                        channelRow.TextChannelId = textChannel.Id;
                        await VoiceLink.SaveChannelRowAsync(channelRow);
                    }
                    else
                    {
                        if(!textChannel.BotHasPermissions(Permission.ViewChannel | Permission.ManageChannels | Permission.ManageRoles)) return;
                    }

                    var overwrites = textChannel.Overwrites.Select(x => new LocalOverwrite(x.TargetId, x.TargetType, x.Permissions)).ToList();
                    var overwritesChanged = false;

                    overwrites.RemoveAll(x =>
                    {
                        if (x.TargetType == OverwriteTargetType.Member && x.TargetId != _client.CurrentUser.Id)
                        {
                            IMember member = guild.GetMember(x.TargetId);
                            if (member is null || voiceStates.All(y => y.MemberId != member.Id) || voiceStates.First(y => y.MemberId == member.Id).ChannelId == voiceChannel.Id)
                            {
                                overwritesChanged = true;
                                return true;
                            }
                        }
                        return false;
                    });

                    foreach (var member in connectedUsers)
                    {
                        if (!overwrites.Any(x => x.TargetId == member.Id && x.TargetType == OverwriteTargetType.Member))
                        {
                            overwritesChanged = true;
                            overwrites.Add(LocalOverwrite.Member(member.Id, new OverwritePermissions().Allow(Permission.ViewChannel)));
                        }
                    }

                    var everyoneOverwrite = overwrites.FirstOrDefault(x => x.TargetId == guildId && x.TargetType == OverwriteTargetType.Role);
                    if (everyoneOverwrite is null || everyoneOverwrite.Permissions.Denied.ViewChannel)
                    {
                        overwritesChanged = true;
                        overwrites.Remove(everyoneOverwrite);
                        overwrites.Add(new LocalOverwrite(guildId, OverwriteTargetType.Role, new OverwritePermissions().Deny(Permission.ViewChannel)));
                    }

                    if (overwritesChanged)
                    {
                        await textChannel.ModifyAsync(x => x.Overwrites = new Optional<IEnumerable<LocalOverwrite>>(overwrites), new DefaultRestRequestOptions{Reason = "Voice Link"});
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Exception thrown updating linked channel {guildId}/{channelId}");
                }
            });
        }

        private static async Task CloseLinkedChannelAsync(IGuild guild, VoiceLinkRow metaRow, VoiceLinkChannelRow channelRow)
        {
            var textChannel = guild.GetTextChannel(channelRow.TextChannelId);
            if (textChannel is null || !textChannel.BotHasPermissions(Permission.ViewChannel | Permission.ManageChannels)) return;
            
            if (metaRow.DeleteChannels)
            {
                await textChannel.DeleteAsync(new DefaultRestRequestOptions{Reason = "Voice Link"});
                channelRow.TextChannelId = 0;
                await VoiceLink.SaveChannelRowAsync(channelRow);
            }
            else
            {
                // Remove all permission overwrites except @everyone
                var overwrites = textChannel.Overwrites.Select(x => new LocalOverwrite(x.TargetId, x.TargetType, x.Permissions)).ToList();
                overwrites.RemoveAll(x => x.TargetId != guild.Id);
                await textChannel.ModifyAsync(x => x.Overwrites = new Optional<IEnumerable<LocalOverwrite>>(overwrites), new DefaultRestRequestOptions {Reason = "Voice Link"});
            }
        }
    }
}