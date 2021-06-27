﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Database.Data;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.Extensions.Configuration;

namespace Utili.Services
{
    internal class PrefixProvider : IPrefixProvider
    {
        private IConfiguration _config;

        public PrefixProvider(IConfiguration config)
        {
            _config = config;
        }

        public async ValueTask<IEnumerable<IPrefix>> GetPrefixesAsync(IGatewayUserMessage message)
        {
            if (!message.GuildId.HasValue)
            {
                return new IPrefix[]
                {
                    new StringPrefix(_config.GetValue<string>("defaultPrefix")),
                    new MentionPrefix((message.Client as DiscordClientBase).CurrentUser.Id)
                };
            }

            await Task.Yield();
            var row = await Core.GetRowAsync(message.GuildId.Value);
            return new IPrefix[]
            {
                new StringPrefix(row.Prefix.Value),
                new MentionPrefix((message.Client as DiscordClientBase).CurrentUser.Id)
            };
        }
    }
}
