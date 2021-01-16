﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using static Utili.Program;
using BotlistStatsPoster;

namespace Utili
{
    internal static class Sharding
    {
        public static void Update(object sender, ElapsedEventArgs e)
        {
            _ = Database.Sharding.UpdateShardStatsAsync(_client.Shards.Count, _client.Shards.OrderBy(x => x.ShardId).First().ShardId, _client.Guilds.Count);
            _ = UpdateBotlistCountsAsync();
        }

        private static DateTime _lastPost = DateTime.Now.AddMinutes(3);
        public static async Task UpdateBotlistCountsAsync()
        {
            if(_lastPost < DateTime.Now) return;
            _lastPost = DateTime.Now.AddMinutes(3);

            int guilds = await Database.Sharding.GetGuildCountAsync();

            StatsPoster poster = new StatsPoster(_client.CurrentUser.Id, _config.BotlistTokens);
            await poster.PostGuildCountAsync(guilds);
        }
    }
}
