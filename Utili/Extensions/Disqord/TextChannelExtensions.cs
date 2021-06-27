﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using Utili.Utils;

namespace Utili.Extensions
{
    public static class TextChannelExtensions
    {
        public static async Task<IUserMessage> SendInfoAsync(this ITextChannel channel, string title, string content = null)
        {
            var message = new LocalMessage()
                .AddEmbed(MessageUtils.CreateEmbed(EmbedType.Info, title, content));

            return await channel.SendMessageAsync(message);
        }

        public static async Task<IUserMessage> SendSuccessAsync(this ITextChannel channel, string title, string content = null)
        {
            var message = new LocalMessage()
                .AddEmbed(MessageUtils.CreateEmbed(EmbedType.Success, title, content));

            return await channel.SendMessageAsync(message);
        }

        public static async Task<IUserMessage> SendFailureAsync(this ITextChannel channel, string title, string content = null, bool supportLink = true)
        {
            var message = new LocalMessage()
                .AddEmbed(MessageUtils.CreateEmbed(EmbedType.Failure, title, content));

            return await channel.SendMessageAsync(message);
        }

        public static async Task<IUserMessage> SendEmbedAsync(this ITextChannel channel, LocalEmbed embed)
        {
            var message = new LocalMessage()
                .AddEmbed(embed);

            return await channel.SendMessageAsync(message);
        }

        public static async Task<IWebhook> FetchWebhookAsync(this ITextChannel channel, Snowflake webhookId)
        {
            IEnumerable<IWebhook> webhooks = await channel.FetchWebhooksAsync();
            return webhooks.FirstOrDefault(x => x.Id == webhookId);
        }
    }
}
