﻿using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Utili.Features;

namespace Utili.Services
{
    public class BotService : DiscordClientService
    {
        private readonly ILogger<BotService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly CommunityService _community;
        private readonly GuildCountService _guildCount;
        private readonly MemberCacheService _memberCache;
        
        private readonly AutopurgeService _autopurge;
        private readonly ChannelMirroringService _channelMirroring;
        private readonly InactiveRoleService _inactiveRole;
        private readonly JoinMessageService _joinMessage;
        private readonly JoinRolesService _joinRoles;
        private readonly MessageFilterService _messageFilter;
        private readonly MessageLogsService _messageLogs;
        private readonly NoticesService _notices;
        private readonly ReputationService _reputation;
        private readonly RoleLinkingService _roleLinking;
        private readonly RolePersistService _rolePersist;
        private readonly VoiceLinkService _voiceLink;
        private readonly VoiceRolesService _voiceRoles;
        private readonly VoteChannelsService _voteChannels;
        
        public BotService(
            
            ILogger<BotService> logger,
            DiscordClientBase client,
            IServiceScopeFactory scopeFactory,
            
            CommunityService community,
            GuildCountService guildCount,
            MemberCacheService memberCache,
            
            AutopurgeService autopurge,
            ChannelMirroringService channelMirroring,
            InactiveRoleService inactiveRole,
            JoinMessageService joinMessage,
            JoinRolesService joinRoles,
            MessageFilterService messageFilter,
            MessageLogsService messageLogs,
            NoticesService notices,
            ReputationService reputation,
            RoleLinkingService roleLinking,
            RolePersistService rolePersist,
            VoiceLinkService voiceLink,
            VoiceRolesService voiceRoles,
            VoteChannelsService voteChannels)
        
            : base(logger, client)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            
            _community = community;
            _guildCount = guildCount;
            _memberCache = memberCache;
            
            _autopurge = autopurge;
            _channelMirroring = channelMirroring;
            _inactiveRole = inactiveRole;
            _joinMessage = joinMessage;
            _joinRoles = joinRoles;
            _messageFilter = messageFilter;
            _messageLogs = messageLogs;
            _notices = notices;
            _reputation = reputation;
            _roleLinking = roleLinking;
            _rolePersist = rolePersist;
            _voiceLink = voiceLink;
            _voiceRoles = voiceRoles;
            _voteChannels = voteChannels;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Client.WaitUntilReadyAsync(cancellationToken);

            _memberCache.Start();
            _autopurge.Start();
            _inactiveRole.Start();
            _joinRoles.Start();
            _notices.Start();
            _voiceLink.Start();
            _voiceRoles.Start();
            _guildCount.Start();

            _logger.LogInformation("Services started");
        }

        protected override async ValueTask OnReady(ReadyEventArgs e)
        {
            _ = _memberCache.Ready(e);
        }

        protected override async ValueTask OnGuildAvailable(GuildAvailableEventArgs e)
        {
            _ = _community.GuildAvailable(e);
        }

        protected override async ValueTask OnMessageReceived(MessageReceivedEventArgs e)
        {
            using var scope = _scopeFactory.CreateScope();
            
            await _messageLogs.MessageReceived(scope, e);
            if(await _messageFilter.MessageReceived(scope, e)) return;
            _ = _notices.MessageReceived(scope, e);
            _ = _voteChannels.MessageReceived(scope, e);
            _ = _channelMirroring.MessageReceived(scope, e);
            _ = _autopurge.MessageReceived(scope, e);
            _ = _inactiveRole.MessageReceived(scope, e);
        }

        protected override async ValueTask OnMessageUpdated(MessageUpdatedEventArgs e)
        {
            _ = _messageLogs.MessageUpdated(e);
            _ = _autopurge.MessageUpdated(e);
        }

        protected override async ValueTask OnMessageDeleted(MessageDeletedEventArgs e)
        {
            _ = _messageLogs.MessageDeleted(e);
            _ = _autopurge.MessageDeleted(e);
        }
    
        protected override async ValueTask OnMessagesDeleted(MessagesDeletedEventArgs e)
        {
            _ = _messageLogs.MessagesDeleted(e);
            _ = _autopurge.MessagesDeleted(e);
        }

        protected override async ValueTask OnReactionAdded(ReactionAddedEventArgs e)
        {
            _ = _reputation.ReactionAdded(e);
        }
        
        protected override async ValueTask OnReactionRemoved(ReactionRemovedEventArgs e)
        {
            _ = _reputation.ReactionRemoved(e);
        }

        protected override async ValueTask OnVoiceStateUpdated(VoiceStateUpdatedEventArgs e)
        {
            _ = _voiceLink.VoiceStateUpdated(e);
            _ = _voiceRoles.VoiceStateUpdated(e);
            _ = _inactiveRole.VoiceStateUpdated(e);
        }

        protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs e)
        {
            _ = _joinMessage.MemberJoined(e);
            await _rolePersist.MemberJoined(e);
            await _joinRoles.MemberJoined(e);
        }

        protected override async ValueTask OnMemberUpdated(MemberUpdatedEventArgs e)
        {
            await _joinRoles.MemberUpdated(e);
            await _roleLinking.MemberUpdated(e);
        }

        protected override async ValueTask OnMemberLeft(MemberLeftEventArgs e)
        {
            var member = e.User is IMember user ? user : null;
            await _rolePersist.MemberLeft(e, member);
        }
    }
}
