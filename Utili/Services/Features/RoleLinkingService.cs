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
    public class RoleLinkingService
    {
        private readonly ILogger<RoleLinkingService> _logger;
        private readonly DiscordClientBase _client;

        private List<RoleLinkAction> _actions = new();

        public RoleLinkingService(ILogger<RoleLinkingService> logger, DiscordClientBase client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task MemberUpdated(MemberUpdatedEventArgs e)
        {
            try
            {
                IGuild guild = _client.GetGuild(e.NewMember.GuildId);
                var rows = await RoleLinking.GetRowsAsync(guild.Id);
                if(rows.Count == 0) return;

                if (rows.Count > 2)
                {
                    var premium = await Premium.IsGuildPremiumAsync(guild.Id);
                    if (!premium) rows = rows.Take(2).ToList();
                }

                if (e.OldMember is null) throw new Exception($"Member {e.MemberId} was not cached in guild {e.NewMember.GuildId}");
                var oldRoles = e.OldMember.RoleIds.Select(x => x.RawValue).ToList();
                var newRoles = e.NewMember.RoleIds.Select(x => x.RawValue).ToList();

                var addedRoles = newRoles.Where(x => oldRoles.All(y => y != x)).ToList();
                var removedRoles = oldRoles.Where(x => newRoles.All(y => y != x)).ToList();

                List<ulong> rolesToAdd;
                List<ulong> rolesToRemove; 
                    
                lock (_actions)
                {
                    var actionsPerformedByBot = _actions.Where(x => x.GuildId == guild.Id && x.UserId == e.NewMember.Id).ToList();
                    foreach (var action in actionsPerformedByBot)
                    {
                        if (addedRoles.Contains(action.RoleId) && action.ActionType == RoleLinkActionType.Added)
                        {
                            addedRoles.Remove(action.RoleId);
                            _actions.Remove(action);
                        }
                        else if (removedRoles.Contains(action.RoleId) && action.ActionType == RoleLinkActionType.Removed)
                        {
                            removedRoles.Remove(action.RoleId);
                            _actions.Remove(action);
                        }
                    }

                    rolesToAdd = rows.Where(x => addedRoles.Contains(x.RoleId) && x.Mode == 0).Select(x => x.LinkedRoleId).ToList();
                    rolesToAdd.AddRange(rows.Where(x => removedRoles.Contains(x.RoleId) && x.Mode == 2).Select(x => x.LinkedRoleId));

                    rolesToRemove = rows.Where(x => addedRoles.Contains(x.RoleId) && x.Mode == 1).Select(x => x.LinkedRoleId).ToList();
                    rolesToRemove.AddRange(rows.Where(x => removedRoles.Contains(x.RoleId) && x.Mode == 3).Select(x => x.LinkedRoleId));

                    rolesToAdd.RemoveAll(x =>
                    {
                        var role = guild.GetRole(x);
                        return role is null || !role.CanBeManaged();
                    });
                    
                    rolesToRemove.RemoveAll(x =>
                    {
                        var role = guild.GetRole(x);
                        return role is null || !role.CanBeManaged();
                    });
                    
                    _actions.AddRange(rolesToAdd.Select(x => new RoleLinkAction(guild.Id, e.NewMember.Id, x, RoleLinkActionType.Added)));
                    _actions.AddRange(rolesToRemove.Select(x => new RoleLinkAction(guild.Id, e.NewMember.Id, x, RoleLinkActionType.Removed)));
                }

                foreach (var roleId in rolesToAdd)
                {
                    await e.NewMember.GrantRoleAsync(roleId, new DefaultRestRequestOptions{Reason = "Role Linking"});
                    await Task.Delay(1000);
                }
                foreach (var roleId in rolesToRemove)
                {
                    await e.NewMember.RevokeRoleAsync(roleId, new DefaultRestRequestOptions{ Reason = "Role Linking" });
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown on member updated");
            }
        }

        private class RoleLinkAction
        {
            public ulong GuildId { get; }
            public ulong UserId { get; }
            public ulong RoleId { get; }
            public RoleLinkActionType ActionType { get; }

            public RoleLinkAction(ulong guildId, ulong userId, ulong roleId, RoleLinkActionType actionType)
            {
                GuildId = guildId;
                UserId = userId;
                RoleId = roleId;
                ActionType = actionType;
            }
        }

        private enum RoleLinkActionType
        {
            Added, 
            Removed
        }
    }
}
