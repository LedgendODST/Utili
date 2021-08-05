﻿using System.Threading.Tasks;
using Database.Data;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using NewDatabase;
using NewDatabase.Entities;
using NewDatabase.Extensions;
using Qmmands;
using Utili.Implementations;
using Utili.Services;

namespace Utili.Commands.Features
{
    [Group("Notice", "Notices")]
    public class NoticesCommands : DiscordGuildModuleBase
    {
        private readonly DatabaseContext _dbContext;
        
        public NoticesCommands(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        [Command("Preview", "Send")]
        [RequireBotChannelPermissions(Permission.SendMessages | Permission.EmbedLinks | Permission.AttachFiles)]
        public async Task Preview(
            [RequireAuthorParameterChannelPermissions(Permission.ViewChannel | Permission.ReadMessageHistory)]
            ITextChannel channel = null)
        {
            channel ??= Context.Channel;
            var config = await _dbContext.NoticeConfigurations.GetForGuildChannelAsync(Context.GuildId, channel.Id);
            await Context.Channel.SendMessageAsync(NoticesService.GetNotice(config));
        }
    }
}
