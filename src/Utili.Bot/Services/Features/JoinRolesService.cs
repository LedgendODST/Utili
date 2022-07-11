﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Gateway;
using Disqord.Http;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Utili.Database.Entities;
using Utili.Database.Extensions;
using Utili.Bot.Extensions;
using Utili.Bot.Utils;

namespace Utili.Bot.Services
{
    public class JoinRolesService
    {
        private readonly ILogger<JoinRolesService> _logger;
        private readonly DiscordClientBase _client;
        private readonly IServiceScopeFactory _scopeFactory;

        private Scheduler<(Snowflake, Snowflake)> _roleGrantScheduler;

        public JoinRolesService(ILogger<JoinRolesService> logger, DiscordClientBase client, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _client = client;
            _scopeFactory = scopeFactory;
            _roleGrantScheduler = new Scheduler<(Snowflake, Snowflake)>();
            _roleGrantScheduler.Callback += OnRoleGrantSchedulerCallback;
        }

        public void Start()
        {
            _ = ScheduleAllRoleGrants();
            _roleGrantScheduler.Start();
        }

        public async Task MemberJoined(IServiceScope scope, MemberJoinedEventArgs e, bool rolePersistAddedRoles)
        {
            try
            {
                var db = scope.GetDbContext();
                var config = await db.JoinRolesConfigurations.GetForGuildAsync(e.GuildId);

                if(config is null ||
                   config.CancelOnRolePersist && rolePersistAddedRoles ||
                   config.JoinRoles.Count == 0) return;

                var memberRecord = await db.JoinRolesPendingMembers.GetForMemberAsync(e.GuildId, e.MemberId);
                if (memberRecord is not null)
                {
                    db.JoinRolesPendingMembers.Remove(memberRecord);
                    await db.SaveChangesAsync();
                }

                // Must wait for membership screening or 10 minute delay
                if (config.WaitForVerification)
                {
                    var pending = e.Member.IsPending;

                    var newAccount = e.Member.CreatedAt().UtcDateTime >= DateTime.UtcNow - TimeSpan.FromMinutes(5);
                    var fiveMinuteDelay = newAccount && e.Guild.VerificationLevel >= GuildVerificationLevel.Medium;
                    var tenMinuteDelay = e.Guild.VerificationLevel >= GuildVerificationLevel.High;

                    var delay = tenMinuteDelay
                        ? TimeSpan.FromMinutes(10)
                        : fiveMinuteDelay
                            ? TimeSpan.FromMinutes(5)
                            : TimeSpan.Zero;

                    // If already verified (adding roles also verifies immediately)
                    if ((!pending && delay == TimeSpan.Zero) || rolePersistAddedRoles)
                    {
                        await GrantRolesAsync(e.GuildId, e.MemberId, config.JoinRoles);
                        return;
                    }

                    // If pending but no delay
                    if (pending && delay == TimeSpan.Zero)
                    {
                        memberRecord = new JoinRolesPendingMember(e.GuildId, e.MemberId)
                        {
                            IsPending = true,
                            ScheduledFor = DateTime.MinValue
                        };

                        // Roles will be added when MemberUpdated fires to signal that the member is no longer pending
                        db.JoinRolesPendingMembers.Add(memberRecord);
                        await db.SaveChangesAsync();
                        return;
                    }

                    // If pending and delay
                    if (pending && delay != TimeSpan.Zero)
                    {
                        memberRecord = new JoinRolesPendingMember(e.GuildId, e.MemberId)
                        {
                            IsPending = true,
                            ScheduledFor = DateTime.UtcNow + delay
                        };

                        // Granting will be scheduled when MemberUpdated fires to signal that the member is no longer pending
                        db.JoinRolesPendingMembers.Add(memberRecord);
                        await db.SaveChangesAsync();
                        return;
                    }

                    // If delay but not pending
                    if (!pending && delay != TimeSpan.Zero)
                    {
                        memberRecord = new JoinRolesPendingMember(e.GuildId, e.MemberId)
                        {
                            IsPending = false,
                            ScheduledFor = DateTime.UtcNow + delay
                        };

                        db.JoinRolesPendingMembers.Add(memberRecord);
                        await db.SaveChangesAsync();

                        // Create timer now
                        await ScheduleRoleGrantAsync(e.GuildId, e.MemberId, memberRecord.ScheduledFor);
                    }
                }
                else
                {
                    await GrantRolesAsync(e.GuildId, e.MemberId, config.JoinRoles);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown on member joined");
            }
        }

        public async Task MemberUpdated(IServiceScope scope, MemberUpdatedEventArgs e)
        {
            try
            {
                var guildId = e.NewMember.GuildId;

                var db = scope.GetDbContext();

                var memberRecord = await db.JoinRolesPendingMembers.GetForMemberAsync(guildId, e.MemberId);
                if (memberRecord is null) return;

                var config = await db.JoinRolesConfigurations.GetForGuildAsync(guildId);
                if (config is null) return;

                // If member has been granted a role (bypassing all verification)
                if (e.NewMember.RoleIds.Any())
                {
                    db.JoinRolesPendingMembers.Remove(memberRecord);
                    await db.SaveChangesAsync();

                    await GrantRolesAsync(guildId, e.MemberId, config.JoinRoles);
                    return;
                }

                // If member is no longer pending
                if (memberRecord.IsPending && !e.NewMember.IsPending)
                {
                    // If there is no delay or the delay has already expired
                    if (memberRecord.ScheduledFor <= DateTime.UtcNow)
                    {
                        db.JoinRolesPendingMembers.Remove(memberRecord);
                        await db.SaveChangesAsync();

                        await GrantRolesAsync(guildId, e.MemberId, config.JoinRoles);
                        return;
                    }

                    // Start the delay timer now
                    await ScheduleRoleGrantAsync(guildId, e.MemberId, memberRecord.ScheduledFor);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown on member updated");
            }
        }

        public async Task MemberLeft(IServiceScope scope, MemberLeftEventArgs e)
        {
            try
            {
                var db = scope.GetDbContext();

                var memberRecord = await db.JoinRolesPendingMembers.GetForMemberAsync(e.GuildId, e.MemberId);
                if (memberRecord is null) return;

                await _roleGrantScheduler.CancelAsync((e.GuildId, e.MemberId));
                db.JoinRolesPendingMembers.Remove(memberRecord);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown on member left");
            }
        }

        private async Task ScheduleAllRoleGrants()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.GetDbContext();
                var memberRecords = await db.JoinRolesPendingMembers.ToListAsync();
                var configs = await db.JoinRolesConfigurations.ToListAsync();

                foreach (var memberRecord in memberRecords)
                {
                    var guild = _client.GetGuild(memberRecord.GuildId);
                    if (guild is null) continue;

                    IMember member;

                    try
                    {
                        member = await _client.FetchMemberAsync(memberRecord.GuildId, memberRecord.MemberId);
                    }
                    catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.NotFound)
                    {
                        db.JoinRolesPendingMembers.Remove(memberRecord);
                        await db.SaveChangesAsync();
                        continue;
                    }

                    var config = configs.FirstOrDefault(x => x.GuildId == memberRecord.GuildId);
                    if (config is null || config.JoinRoles.Count == 0)
                    {
                        db.JoinRolesPendingMembers.Remove(memberRecord);
                        await db.SaveChangesAsync();
                        continue;
                    }

                    // If now verified
                    if ((!member.IsPending && memberRecord.ScheduledFor <= DateTime.UtcNow) || member.RoleIds.Any())
                    {
                        db.JoinRolesPendingMembers.Remove(memberRecord);
                        await db.SaveChangesAsync();
                        await GrantRolesAsync(memberRecord.GuildId, memberRecord.MemberId, config.JoinRoles);
                        continue;
                    }

                    // If not pending but still delayed
                    if (!member.IsPending)
                    {
                        await ScheduleRoleGrantAsync(memberRecord.GuildId, memberRecord.MemberId, memberRecord.ScheduledFor);
                    }

                    // If none of the above:
                    // Granting will be scheduled when MemberUpdated fires to signal that the member is no longer pending
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown on scheduling all role grants");
            }
        }

        private async Task ScheduleRoleGrantAsync(Snowflake guildId, Snowflake memberId, DateTime scheduleFor)
        {
            await _roleGrantScheduler.CancelAsync((guildId, memberId));
            await _roleGrantScheduler.ScheduleAsync((guildId, memberId), scheduleFor);
        }

        private async Task OnRoleGrantSchedulerCallback((Snowflake, Snowflake) key)
        {
            var (guildId, memberId) = key;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.GetDbContext();
                var config = await db.JoinRolesConfigurations.GetForGuildAsync(guildId);

                var memberRecord = await db.JoinRolesPendingMembers.GetForMemberAsync(guildId, memberId);
                db.JoinRolesPendingMembers.Remove(memberRecord);
                await db.SaveChangesAsync();

                await GrantRolesAsync(guildId, memberId, config.JoinRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown on role grant scheduler callback");
            }
        }

        private async Task GrantRolesAsync(Snowflake guildId, Snowflake memberId, List<ulong> roleIds)
        {
            var guild = _client.GetGuild(guildId);

            roleIds.RemoveAll(x => !guild.GetRole(x).CanBeManaged());
            roleIds = roleIds.Distinct().ToList();

            foreach (var roleId in roleIds)
                await _client.GrantRoleAsync(guildId, memberId, roleId, new DefaultRestRequestOptions { Reason = "Join Roles" });
        }
    }
}