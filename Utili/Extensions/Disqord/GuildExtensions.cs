﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;

namespace Utili.Extensions
{
    static class GuildExtensions
    {
        public static CachedTextChannel GetTextChannel(this IGuild guild, Snowflake channelId)
        {
            return guild.GetChannel(channelId) as CachedTextChannel;
        }

        public static CachedVoiceChannel GetVoiceChannel(this IGuild guild, Snowflake channelId)
        {
            return guild.GetChannel(channelId) as CachedVoiceChannel;
        }

        public static CachedCategoryChannel GetCategoryChannel(this IGuild guild, Snowflake channelId)
        {
            return guild.GetChannel(channelId) as CachedCategoryChannel;
        }

        public static IRole GetRole(this IGuild guild, Snowflake roleId)
        {
            return guild.Roles.Values.FirstOrDefault(x => x.Id == roleId);
        }

        public static IMember GetCurrentMember(this IGuild guild)
        {
            return guild.GetMember((guild.Client as DiscordClientBase).CurrentUser.Id);
        }

        public static bool BotHasPermissions(this IGuild guild, Permission permissions)
        {
            return guild.GetCurrentMember().GetGuildPermissions().Has(permissions);
        }

        public static Task<IReadOnlyList<IMember>> FetchAllMembersAsync(this IGuild guild)
        {
            return (guild.Client as DiscordClientBase).FetchAllMembersAsync(guild.Id);
        }
    }
}
