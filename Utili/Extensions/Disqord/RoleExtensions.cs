﻿using System.Collections.Generic;
using System.Linq;
using Disqord;
using Disqord.Gateway;

namespace Utili.Extensions
{
    static class RoleExtensions
    {
        public static bool CanBeManaged(this IRole role)
        {
            IGuild guild = (role.Client as DiscordClientBase).GetGuild(role.GuildId);
            IMember bot = guild.GetCurrentMember();
            IEnumerable<CachedRole> roles = bot.GetRoles().Values;

            // The higher the position the higher the role in the hierarchy

            return guild.BotHasPermissions(Permission.ManageRoles) && 
                   roles.Any(x => x.Position > role.Position);
        }
    }
}
