﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Database.Data
{
    public static class VoiceLink
    {
        public static async Task<List<VoiceLinkRow>> GetRowsAsync(ulong? guildId = null, bool ignoreCache = false)
        {
            List<VoiceLinkRow> matchedRows = new();

            if (Cache.Initialised && !ignoreCache)
            {
                matchedRows.AddRange(Cache.VoiceLink);

                if (guildId.HasValue) matchedRows.RemoveAll(x => x.GuildId != guildId.Value);
            }
            else
            {
                string command = "SELECT * FROM VoiceLink WHERE TRUE";
                List<(string, object)> values = new();

                if (guildId.HasValue)
                {
                    command += " AND GuildId = @GuildId";
                    values.Add(("GuildId", guildId.Value));
                }

                MySqlDataReader reader = await Sql.ExecuteReaderAsync(command, values.ToArray());

                while (reader.Read())
                {
                    matchedRows.Add(VoiceLinkRow.FromDatabase(
                        reader.GetUInt64(0),
                        reader.GetBoolean(1),
                        reader.GetBoolean(2),
                        reader.GetString(3),
                        reader.GetString(4)));
                }

                reader.Close();
            }

            return matchedRows;
        }

        public static async Task<VoiceLinkRow> GetRowAsync(ulong guildId)
        {
            List<VoiceLinkRow> rows = await GetRowsAsync(guildId);
            return rows.Count > 0 ? rows.First() : new VoiceLinkRow(guildId);
        }

        public static async Task SaveRowAsync(VoiceLinkRow row)
        {
            if (row.New)
            {
                await Sql.ExecuteAsync(
                    "INSERT INTO VoiceLink (GuildId, Enabled, DeleteChannels, Prefix, ExcludedChannels) VALUES (@GuildId, @Enabled, @DeleteChannels, @Prefix, @ExcludedChannels );",
                    ("GuildId", row.GuildId),
                    ("Enabled", row.Enabled),
                    ("DeleteChannels", row.DeleteChannels),
                    ("Prefix", row.Prefix.EncodedValue),
                    ("ExcludedChannels", row.GetExcludedChannelsString()));

                row.New = false;
                if(Cache.Initialised) Cache.VoiceLink.Add(row);
            }
            else
            {
                await Sql.ExecuteAsync(
                    "UPDATE VoiceLink SET Enabled = @Enabled, DeleteChannels = @DeleteChannels, Prefix = @Prefix, ExcludedChannels = @ExcludedChannels WHERE GuildId = @GuildId;",
                    ("GuildId", row.GuildId),
                    ("Enabled", row.Enabled),
                    ("DeleteChannels", row.DeleteChannels),
                    ("Prefix", row.Prefix.EncodedValue),
                    ("ExcludedChannels", row.GetExcludedChannelsString()));

                if(Cache.Initialised) Cache.VoiceLink[Cache.VoiceLink.FindIndex(x => x.GuildId == row.GuildId)] = row;
            }
        }

        public static async Task DeleteRowAsync(VoiceLinkRow row)
        {
            if(Cache.Initialised) Cache.VoiceLink.RemoveAll(x => x.GuildId == row.GuildId);

            await Sql.ExecuteAsync(
                "DELETE FROM VoiceLink WHERE GuildId = @GuildId",
                ("GuildId", row.GuildId));
        }

        public static async Task<List<VoiceLinkChannelRow>> GetChannelRowsAsync(ulong? guildId = null, ulong? voiceChannelId = null, bool ignoreCache = false)
        {
            List<VoiceLinkChannelRow> matchedRows = new();

            if (Cache.Initialised && !ignoreCache)
            {
                matchedRows.AddRange(Cache.VoiceLinkChannels);

                if (guildId.HasValue) matchedRows.RemoveAll(x => x.GuildId != guildId.Value);
                if (voiceChannelId.HasValue) matchedRows.RemoveAll(x => x.VoiceChannelId != voiceChannelId.Value);
            }
            else
            {
                string command = "SELECT * FROM VoiceLinkChannels WHERE TRUE";
                List<(string, object)> values = new();

                if (guildId.HasValue)
                {
                    command += " AND GuildId = @GuildId";
                    values.Add(("GuildId", guildId.Value));
                }

                if (voiceChannelId.HasValue)
                {
                    command += " AND VoiceChannelId = @VoiceChannelId";
                    values.Add(("VoiceChannelId", voiceChannelId.Value));
                }

                MySqlDataReader reader = await Sql.ExecuteReaderAsync(command, values.ToArray());

                while (reader.Read())
                {
                    matchedRows.Add(VoiceLinkChannelRow.FromDatabase(
                        reader.GetUInt64(0),
                        reader.GetUInt64(1),
                        reader.GetUInt64(2)));
                }

                reader.Close();
            }

            return matchedRows;
        }

        public static async Task<VoiceLinkChannelRow> GetChannelRowAsync(ulong guildId, ulong voiceChannelId)
        {
            List<VoiceLinkChannelRow> rows = await GetChannelRowsAsync(guildId, voiceChannelId);
            return rows.Count > 0 ? rows.First() : new VoiceLinkChannelRow(guildId, voiceChannelId);
        }

        public static async Task SaveChannelRowAsync(VoiceLinkChannelRow row)
        {
            if (row.New)
            {
                await Sql.ExecuteAsync(
                    "INSERT INTO VoiceLinkChannels (GuildId, TextChannelId, VoiceChannelId) VALUES (@GuildId, @TextChannelId, @VoiceChannelId);",
                    ("GuildId", row.GuildId), 
                    ("TextChannelId", row.TextChannelId),
                    ("VoiceChannelId", row.VoiceChannelId));

                row.New = false;
                if(Cache.Initialised) Cache.VoiceLinkChannels.Add(row);
            }
            else
            {
                await Sql.ExecuteAsync(
                    "UPDATE VoiceLinkChannels SET GuildId = @GuildId, TextChannelId = @TextChannelId, VoiceChannelId = @VoiceChannelId WHERE @GuildId = @GuildId AND VoiceChannelId = @VoiceChannelId",
                    ("GuildId", row.GuildId), 
                    ("TextChannelId", row.TextChannelId),
                    ("VoiceChannelId", row.VoiceChannelId));

                if(Cache.Initialised) Cache.VoiceLinkChannels[Cache.VoiceLinkChannels.FindIndex(x => x.GuildId == row.GuildId && x.VoiceChannelId == row.VoiceChannelId)] = row;
            }
        }

        public static async Task DeleteChannelRowAsync(VoiceLinkChannelRow row)
        {
            if(Cache.Initialised) Cache.VoiceLinkChannels.RemoveAll(x => x.GuildId == row.GuildId && x.VoiceChannelId == row.VoiceChannelId);

            await Sql.ExecuteAsync(
                "DELETE FROM VoiceLinkChannels WHERE GuildId = @GuildId AND VoiceChannelId = @VoiceChannelId",
                ("GuildId", row.GuildId),
                ("VoiceChannelId", row.VoiceChannelId));
        }
    }

    public class VoiceLinkRow : IRow
    {
        public bool New { get; set; }
        public ulong GuildId { get; set; }
        public bool Enabled { get; set; }
        public bool DeleteChannels { get; set; }
        public EString Prefix { get; set; }
        public List<ulong> ExcludedChannels { get; set; }

        private VoiceLinkRow()
        {

        }

        public VoiceLinkRow(ulong guildId)
        {
            New = true;
            GuildId = guildId;
            Enabled = false;
            DeleteChannels = true;
            Prefix = EString.FromDecoded("vc-");
            ExcludedChannels = new List<ulong>();
        }

        public static VoiceLinkRow FromDatabase(ulong guildId, bool enabled, bool deleteChannels, string prefix, string excludedChannels)
        {
            VoiceLinkRow row = new()
            {
                New = false,
                GuildId = guildId,
                Enabled = enabled,
                DeleteChannels = deleteChannels,
                Prefix = EString.FromEncoded(prefix),
                ExcludedChannels = new List<ulong>()
            };

            if (!string.IsNullOrEmpty(excludedChannels))
            {
                foreach (string excludedChannel in excludedChannels.Split(","))
                {
                    if (ulong.TryParse(excludedChannel, out ulong channelId))
                    {
                        row.ExcludedChannels.Add(channelId);
                    }
                }
            }

            return row;
        }

        public string GetExcludedChannelsString()
        {
            string excludedChannelsString = "";

            for (int i = 0; i < ExcludedChannels.Count; i++)
            {
                ulong excludedChannelId = ExcludedChannels[i];
                excludedChannelsString += excludedChannelId.ToString();
                if (i != ExcludedChannels.Count - 1)
                {
                    excludedChannelsString += ",";
                }
            }

            return excludedChannelsString;
        }

        public async Task SaveAsync()
        {
            await VoiceLink.SaveRowAsync(this);
        }

        public async Task DeleteAsync()
        {
            await VoiceLink.DeleteRowAsync(this);
        }
    }

    public class VoiceLinkChannelRow : IRow
    {
        public bool New { get; set; }
        public ulong GuildId { get; set; }
        public ulong TextChannelId { get; set; }
        public ulong VoiceChannelId { get; set; }

        private VoiceLinkChannelRow()
        {
            
        }

        public VoiceLinkChannelRow(ulong guildId, ulong voiceChannelId)
        {
            New = true;
            GuildId = guildId;
            VoiceChannelId = voiceChannelId;
        }

        public static VoiceLinkChannelRow FromDatabase(ulong guildId, ulong textChannelId, ulong voiceChannelId)
        {
            return new()
            {
                New = false,
                GuildId = guildId,
                TextChannelId = textChannelId,
                VoiceChannelId = voiceChannelId
            };
        }

        public async Task SaveAsync()
        {
            await VoiceLink.SaveChannelRowAsync(this);
        }

        public async Task DeleteAsync()
        {
            await VoiceLink.DeleteChannelRowAsync(this);
        }
    }
}
