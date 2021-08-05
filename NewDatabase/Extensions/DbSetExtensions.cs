﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewDatabase.Entities;
using NewDatabase.Entities.Base;

namespace NewDatabase.Extensions
{
    public static class DbSetExtensions
    {
        public static Task<T> GetForGuildAsync<T>(this DbSet<T> dbSet, ulong guildId) where T : GuildEntity
        {
            return dbSet.FirstOrDefaultAsync(x => x.GuildId == guildId);
        }
        
        public static Task<List<T>> GetAllForGuildAsync<T>(this DbSet<T> dbSet, ulong guildId) where T : GuildChannelEntity
        {
            return dbSet.Where(x => x.GuildId == guildId).ToListAsync();
        }
        
        public static Task<T> GetForGuildChannelAsync<T>(this DbSet<T> dbSet, ulong guildId, ulong channelId) where T : GuildChannelEntity
        {
            return dbSet.FirstOrDefaultAsync(x => x.GuildId == guildId && x.ChannelId == channelId);
        }
        
        public static Task<T> GetForMessageAsync<T>(this DbSet<T> dbSet, ulong messageId) where T : MessageEntity
        {
            return dbSet.FirstOrDefaultAsync(x => x.MessageId == messageId);
        }
        
        public static Task<T> GetForMemberAsync<T>(this DbSet<T> dbSet, ulong guildId, ulong memberId) where T : MemberEntity
        {
            return dbSet.FirstOrDefaultAsync(x => x.GuildId == guildId && x.MemberId == memberId);
        }
        
        public static Task<List<T>> GetAllForGuildMembersAsync<T>(this DbSet<T> dbSet, ulong guildId) where T : MemberEntity
        {
            return dbSet.Where(x => x.GuildId == guildId).ToListAsync();
        }
        
        public static Task<ReputationConfiguration> GetForGuildWithEmojisAsync(this DbSet<ReputationConfiguration> dbSet, ulong guildId)
        {
            return dbSet.Include(x => x.Emojis).FirstOrDefaultAsync(x => x.GuildId == guildId);
        }
        
        public static Task<List<RoleLinkingConfiguration>> GetAllForGuildAsync(this DbSet<RoleLinkingConfiguration> dbSet, ulong guildId)
        {
            return dbSet.Where(x => x.GuildId == guildId).ToListAsync();
        }
        
        public static Task<List<PremiumSlot>> GetAllForUserAsync(this DbSet<PremiumSlot> dbSet, ulong userId)
        {
            return dbSet.Where(x => x.UserId == userId).ToListAsync();
        }
        
        public static Task<List<Subscription>> GetAllForUserAsync(this DbSet<Subscription> dbSet, ulong userId)
        {
            return dbSet.Where(x => x.UserId == userId).ToListAsync();
        }

        public static async Task<ReputationMember> UpdateMemberReputationAsync(this DbSet<ReputationMember> dbSet, ulong guildId, ulong memberId, long change)
        {
            var repMember = await dbSet.GetForMemberAsync(guildId, memberId);
            repMember.Reputation += change;
            dbSet.Update(repMember);
            return repMember;
        }
    }
}
