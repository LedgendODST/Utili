﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Database.Data
{
    public static class Subscriptions
    {
        public static async Task<List<SubscriptionsRow>> GetRowsAsync(string subscriptionId = null, ulong? userId = null, bool onlyValid = false)
        {
            List<SubscriptionsRow> matchedRows = new List<SubscriptionsRow>();

            string command = "SELECT * FROM Subscriptions WHERE TRUE";
            List<(string, object)> values = new List<(string, object)>();

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                command += " AND SubscriptionId = @SubscriptionId";
                values.Add(("SubscriptionId", subscriptionId));
            }

            if (userId.HasValue)
            {
                command += " AND UserId = @UserId";
                values.Add(("UserId", userId.Value));
            }

            if (onlyValid)
            {
                command += " AND (Status=0 OR Status=1 OR Status=6)";
            }

            MySqlDataReader reader = await Sql.ExecuteReaderAsync(command, values.ToArray());

            while (reader.Read())
            {
                matchedRows.Add(SubscriptionsRow.FromDatabase(
                    reader.GetString(0),
                    reader.GetUInt64(1),
                    reader.GetDateTime(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4)));
            }

            reader.Close();

            return matchedRows;
        }

        public static async Task<SubscriptionsRow> GetRowAsync(string subscriptionId)
        {
            List<SubscriptionsRow> rows = await GetRowsAsync(subscriptionId);
            return rows.Count > 0 ? rows.First() : new SubscriptionsRow(subscriptionId);
        }

        public static async Task<int> GetSlotCountAsync(ulong userId)
        {
            MySqlDataReader reader = await Sql.ExecuteReaderAsync(
                "SELECT SUM(Slots) FROM Subscriptions WHERE UserId = @UserId AND (Status=0 OR Status=1 OR Status=6);",
                ("UserId", userId));

            int slots = 0;

            reader.Read();
            try { slots = reader.GetInt32(0); } catch { }
            reader.Close();
            return slots;
        }

        public static async Task SaveRowAsync(SubscriptionsRow row)
        {
            if (row.New)
            {
                await Sql.ExecuteAsync("INSERT INTO Subscriptions (SubscriptionId, UserId, EndsAt, Slots, Status) VALUES (@SubscriptionId, @UserId, @EndsAt, @Slots, @Status);",
                    ("SubscriptionId", row.SubscriptionId),
                    ("UserId", row.UserId),
                    ("EndsAt", row.EndsAt),
                    ("Slots", row.Slots),
                    ("Status", (int)row.Status));

                row.New = false;
            }
            else
            {
                await Sql.ExecuteAsync(
                    "UPDATE Subscriptions SET UserId = @UserId, EndsAt = @EndsAt, Slots = @Slots, Status = @Status WHERE SubscriptionId = @SubscriptionId;",
                    ("SubscriptionId", row.SubscriptionId),
                    ("UserId", row.UserId.ToString()),
                    ("EndsAt", row.EndsAt),
                    ("Slots", row.Slots.ToString()),
                    ("Status", (int)row.Status));
            }
        }

        public static async Task DeleteRowAsync(SubscriptionsRow row)
        {
            if(row is null) return;

            await Sql.ExecuteAsync(
                "DELETE FROM Subscriptions WHERE SubscriptionId = @SubscriptionId;",
                ("SubscriptionId", row.SubscriptionId));
        }
    }

    public class SubscriptionsRow : IRow
    {
        public bool New { get; set; }
        public string SubscriptionId { get; set; }
        public DateTime EndsAt { get; set; }
        public ulong UserId { get; set; }
        public int Slots { get; set; }
        public SubscriptionStatus Status { get; set; }
        public bool IsValid => Status == SubscriptionStatus.Active ||  Status == SubscriptionStatus.PastDue || Status == SubscriptionStatus.Trialing;

        private SubscriptionsRow()
        {

        }

        public SubscriptionsRow(string subscriptionId)
        {
            New = true;
            SubscriptionId = subscriptionId;
        }

        public static SubscriptionsRow FromDatabase(string subscriptionId, ulong userId, DateTime endsAt, int slots, int status)
        {
            return new SubscriptionsRow
            {
                New = false,
                SubscriptionId = subscriptionId,
                UserId = userId,
                EndsAt = endsAt,
                Slots = slots,
                Status = (SubscriptionStatus)status
            };
        }

        public async Task SaveAsync()
        {
            await Subscriptions.SaveRowAsync(this);
        }

        public async Task DeleteAsync()
        {
            await Subscriptions.DeleteRowAsync(this);
        }
    }

    public enum SubscriptionStatus
    {
        Active = 0,                 // Subscription is active                                           Valid
        PastDue = 1,                // Subscription renewal was unsuccessful                            Valid
        Unpaid = 2,                 // Not used                                                         Invalid
        Canceled = 3,               // Subscription has been canceled                                   Invalid
        Incomplete = 4,             // Subscription's initial payment failed                            Invalid
        IncompleteExpired = 5,      // Subscription's initial payment has failed for 23 hours           Invalid
        Trialing = 6                // Subscription is a trial period                                   Valid
    }
}
