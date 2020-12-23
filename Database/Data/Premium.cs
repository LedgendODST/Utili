﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Database.Data
{
    public static class Premium
    {
        public static async Task<List<PremiumRow>> GetRowsAsync(ulong? userId = null, ulong? guildId = null, int? slotId = null, bool ignoreCache = false)
        {
            List<PremiumRow> matchedRows = new List<PremiumRow>();

            if (Cache.Initialised && !ignoreCache)
            {
                matchedRows.AddRange(Cache.Premium.Rows);

                if (userId.HasValue) matchedRows.RemoveAll(x => x.UserId != userId.Value);
                if (guildId.HasValue) matchedRows.RemoveAll(x => x.GuildId != guildId.Value);
                if (slotId.HasValue) matchedRows.RemoveAll(x => x.SlotId != slotId.Value);
            }
            else
            {
                string command = "SELECT * FROM Premium WHERE TRUE";
                List<(string, object)> values = new List<(string, object)>();

                if (userId.HasValue)
                {
                    command += " AND UserId = @UserId";
                    values.Add(("UserId", userId.Value.ToString()));
                }

                if (guildId.HasValue)
                {
                    command += " AND GuildId = @GuildId";
                    values.Add(("GuildId", guildId.Value.ToString()));
                }

                if (slotId.HasValue)
                {
                    command += " AND SlotId = @SlotId";
                    values.Add(("SlotId", slotId.Value.ToString()));
                }

                MySqlDataReader reader = await Sql.ExecuteReaderAsync(command, values.ToArray());

                while (reader.Read())
                {
                    matchedRows.Add(PremiumRow.FromDatabase(
                        reader.GetInt32(0),
                        reader.GetUInt64(1),
                        reader.GetUInt64(2)));
                }

                reader.Close();
            }

            return matchedRows;
        }

        public static async Task<List<PremiumRow>> GetUserRowsAsync(ulong userId)
        {
            List<PremiumRow> rows = (await GetRowsAsync(userId)).OrderBy(x => x.SlotId).ToList();
            int amount = await Subscriptions.GetSlotCountAsync(userId);

            rows = rows.Take(amount).ToList();
            while (rows.Count < amount)
            {
                PremiumRow row = new PremiumRow(userId, 0);
                await SaveRowAsync(row);
                rows.Add(row);
            }

            return rows;
        }

        public static async Task<PremiumRow> GetUserRowAsync(ulong userId, int slotId)
        {
            List<PremiumRow> rows = await GetRowsAsync(userId, slotId: slotId);
            return rows.Count > 0 ? rows.First() : null;
        }

        public static async Task<bool> IsGuildPremiumAsync(ulong guildId)
        {
            List<PremiumRow> rows = await GetRowsAsync(guildId: guildId);
            return rows.Count > 0;
        }

        public static async Task SaveRowAsync(PremiumRow row)
        {
            if (row.New)
            {
                await Sql.ExecuteAsync(
                    "INSERT INTO Premium (UserId, GuildId) VALUES (@UserId, @GuildId);",
                    ("UserId", row.UserId),
                    ("GuildId", row.GuildId));

                row.New = false;
                row.SlotId = (await GetRowsAsync(row.UserId, row.GuildId)).OrderBy(x => x.SlotId).Last().SlotId;
                // TODO: Ensure this is always accurately setting the value

                if(Cache.Initialised) Cache.Premium.Rows.Add(row);
            }
            else
            {
                await Sql.ExecuteAsync(
                    "UPDATE Premium SET GuildId = @GuildId, UserId = @UserId WHERE SlotId = @SlotId",
                    ("UserId", row.UserId),
                    ("GuildId", row.GuildId),
                    ("SlotId", row.SlotId));

                if(Cache.Initialised) Cache.Premium.Rows[Cache.Premium.Rows.FindIndex(x => x.SlotId == row.SlotId)] = row;
            }
        }

        public static async Task DeleteRowAsync(PremiumRow row)
        {
            if(Cache.Initialised) Cache.Premium.Rows.RemoveAll(x => x.SlotId == row.SlotId);

            await Sql.ExecuteAsync(
                "DELETE FROM Premium WHERE SlotId = @SlotId;",
                ("SlotId", row.SlotId));
        }
    }

    public class PremiumTable
    {
        public List<PremiumRow> Rows { get; set; }
    }

    public class PremiumRow
    {
        public bool New { get; set; }
        public int SlotId { get; set; }
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }

        private PremiumRow()
        {

        }

        public PremiumRow(ulong userId, ulong guildId)
        {
            New = true;
            UserId = userId;
            GuildId = guildId;
        }

        public static PremiumRow FromDatabase(int slotId, ulong userId, ulong guildId)
        {
            return new PremiumRow
            {
                New = false,
                SlotId = slotId,
                UserId = userId,
                GuildId = guildId
            };
        }
    }
}