﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Database.Data;
using Disqord;
using Disqord.Gateway;
using Disqord.Http;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Utili.Extensions;

namespace Utili.Services
{
    public class ChannelMirroringService
    {
        private readonly ILogger<ChannelMirroringService> _logger;
        private readonly DiscordClientBase _client;

        private Dictionary<ulong, IWebhook> _webhookCache;

        public ChannelMirroringService(ILogger<ChannelMirroringService> logger, DiscordClientBase client)
        {
            _logger = logger;
            _client = client;

            _webhookCache = new Dictionary<ulong, IWebhook>();
        }

        public async Task MessageReceived(MessageReceivedEventArgs e)
        {
            try
            {
                if(!e.GuildId.HasValue || e.Message is not IUserMessage {WebhookId: null} userMessage) return;

                var row = await ChannelMirroring.GetRowAsync(e.GuildId.Value, e.ChannelId);
                var guild = _client.GetGuild(e.GuildId.Value);
                var channel = guild.GetTextChannel(row.ToChannelId);
                if(channel is null) return;

                if(!channel.BotHasPermissions(Permission.ViewChannel | Permission.ManageWebhooks)) return;
                
                IWebhook webhook;
                try
                {
                    webhook = await GetWebhookAsync(row.WebhookId);
                }
                catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.NotFound)
                {
                    webhook = null;
                }

                if (webhook is null)
                {
                    var avatar = File.OpenRead("Avatar.png");
                    webhook = await channel.CreateWebhookAsync("Utili Mirroring", x => x.Avatar = avatar);
                    avatar.Close();

                    row.WebhookId = webhook.Id;
                    await row.SaveAsync();
                }

                var username = $"{e.Message.Author} in {e.Channel.Name}";
                var avatarUrl = e.Message.Author.GetAvatarUrl();

                if (!string.IsNullOrWhiteSpace(userMessage.Content) || userMessage.Embeds.Count > 0)
                {
                    var message = new LocalWebhookMessage()
                        .WithName(username)
                        .WithAvatarUrl(avatarUrl)
                        .WithOptionalContent(userMessage.Content)
                        .WithEmbeds(userMessage.Embeds.Select(LocalEmbed.FromEmbed))
                        .WithAllowedMentions(LocalAllowedMentions.None);

                    await _client.ExecuteWebhookAsync(webhook.Id, webhook.Token, message);
                }
                    
                foreach (var attachment in userMessage.Attachments)
                {
                    var attachmentMessage = new LocalWebhookMessage()
                        .WithName(username)
                        .WithAvatarUrl(avatarUrl)
                        .WithContent(attachment.Url);
                    await _client.ExecuteWebhookAsync(webhook.Id, webhook.Token, attachmentMessage);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception thrown on message received");
            }
        }

        private async Task<IWebhook> GetWebhookAsync(ulong webhookId)
        {
            if (_webhookCache.TryGetValue(webhookId, out var cachedWebhook)) return cachedWebhook;
            try
            {
                var webhook = await _client.FetchWebhookAsync(webhookId);
                _webhookCache.TryAdd(webhookId, webhook);
                return webhook;
            }
            catch
            {
                return null;
            }
        }
    }
}
