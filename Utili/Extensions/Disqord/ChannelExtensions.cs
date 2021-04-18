﻿using System.Collections.Generic;
using System.Linq;
using Disqord;
using Disqord.Gateway;

namespace Utili.Extensions
{
    static class ChannelExtensions
    {
        public static bool BotHasPermissions(this IGuildChannel channel, Permission permissions)
        {
            return channel.GetGuild().GetCurrentMember().GetChannelPermissions(channel).Has(permissions);
        }

        public static IGuild GetGuild(this IGuildChannel channel)
        {
            return (channel.Client as DiscordClientBase).GetGuild(channel.GuildId);
        }
    }
}
