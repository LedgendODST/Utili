﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MySql.Data.MySqlClient;

namespace Database.Data
{
    public class Reputation
    {
        public static List<ReputationRow> GetRows(ulong? guildId = null, int? id = null, bool ignoreCache = false)
        {
            List<ReputationRow> matchedRows = new List<ReputationRow>();

            if (Cache.Initialised && !ignoreCache)
            {
                matchedRows.AddRange(Cache.Reputation.Rows);

                if (guildId.HasValue) matchedRows.RemoveAll(x => x.GuildId != guildId.Value);
                if (id.HasValue) matchedRows.RemoveAll(x => x.Id != id.Value);
            }
            else
            {
                string command = "SELECT * FROM Reputation WHERE TRUE";
                List<(string, string)> values = new List<(string, string)>();

                if (guildId.HasValue)
                {
                    command += " AND GuildId = @GuildId";
                    values.Add(("GuildId", guildId.Value.ToString()));
                }

                if (id.HasValue)
                {
                    command += " AND Id = @Id";
                    values.Add(("Id", id.Value.ToString()));
                }

                MySqlDataReader reader = Sql.GetCommand(command, values.ToArray()).ExecuteReader();

                while (reader.Read())
                {
                    matchedRows.Add(new ReputationRow(
                        reader.GetInt32(0),
                        reader.GetUInt64(1),
                        reader.GetString(2)));
                }

                reader.Close();
            }

            return matchedRows;
        }

        public static void SaveRow(ReputationRow row)
        {
            MySqlCommand command;

            if (row.Id == 0) 
            // The row is a new entry so should be inserted into the database
            {
                command = Sql.GetCommand("INSERT INTO Reputation (GuildId, Emotes) VALUES (@GuildId, @Emotes);",
                    new [] {("GuildId", row.GuildId.ToString()), 
                        ("Emotes", row.GetEmotesString())});

                command.ExecuteNonQuery();
                command.Connection.Close();

                row.Id = GetRows(row.GuildId, ignoreCache: true).First().Id;

                if(Cache.Initialised) Cache.Reputation.Rows.Add(row);
            }
            else
            // The row already exists and should be updated
            {
                command = Sql.GetCommand("UPDATE Reputation SET GuildId = @GuildId, Emotes = @Emotes WHERE Id = @Id;",
                    new [] {("Id", row.Id.ToString()),
                        ("GuildId", row.GuildId.ToString()), 
                        ("Emotes", row.GetEmotesString())});

                command.ExecuteNonQuery();
                command.Connection.Close();

                if(Cache.Initialised) Cache.Reputation.Rows[Cache.Reputation.Rows.FindIndex(x => x.Id == row.Id)] = row;
            }
        }

        public static void DeleteRow(ReputationRow row)
        {
            if(row == null) return;

            if(Cache.Initialised) Cache.Reputation.Rows.RemoveAll(x => x.Id == row.Id);

            string commandText = "DELETE FROM Reputation WHERE Id = @Id";
            MySqlCommand command = Sql.GetCommand(commandText, new[] {("Id", row.Id.ToString())});
            command.ExecuteNonQuery();
            command.Connection.Close();
        }

        public static List<ReputationUserRow> GetUserRows(ulong? guildId = null, ulong? userId = null, int? id = null)
        {
            List<ReputationUserRow> matchedRows = new List<ReputationUserRow>();

            string command = "SELECT * FROM Reputation WHERE TRUE";
            List<(string, string)> values = new List<(string, string)>();

            if (guildId.HasValue)
            {
                command += " AND GuildId = @GuildId";
                values.Add(("GuildId", guildId.Value.ToString()));
            }

            if (userId.HasValue)
            {
                command += " AND UserId = @UserId";
                values.Add(("UserId", userId.Value.ToString()));
            }

            if (id.HasValue)
            {
                command += " AND Id = @Id";
                values.Add(("Id", id.Value.ToString()));
            }

            MySqlDataReader reader = Sql.GetCommand(command, values.ToArray()).ExecuteReader();

            while (reader.Read())
            {
                matchedRows.Add(new ReputationUserRow(
                    reader.GetInt32(0),
                    reader.GetUInt64(1),
                    reader.GetUInt64(2),
                    reader.GetInt32(3)));
            }

            reader.Close();

            return matchedRows;
        }

        public static void SaveUserRow(ReputationUserRow row)
        {
            MySqlCommand command;

            if (row.Id == 0) 
            // The row is a new entry so should be inserted into the database
            {
                command = Sql.GetCommand("INSERT INTO ReputationUsers (GuildId, UserId, Reputation) VALUES (@GuildId, @UserId, @Reputation);",
                    new [] {("GuildId", row.GuildId.ToString()), 
                        ("UserId", row.UserId.ToString()),
                        ("Reputation", row.Reputation.ToString())});

                command.ExecuteNonQuery();
                command.Connection.Close();

                row.Id = GetRows(row.GuildId, ignoreCache: true).First().Id;
            }
            else
            // The row already exists and should be updated
            {
                command = Sql.GetCommand("UPDATE Reputation SET GuildId = @GuildId, UserId = @UserId, Reputation = @Reputation WHERE Id = @Id;",
                    new [] {("Id", row.Id.ToString()),
                        ("GuildId", row.GuildId.ToString()), 
                        ("UserId", row.UserId.ToString()),
                        ("Reputation", row.Reputation.ToString())});

                command.ExecuteNonQuery();
                command.Connection.Close();
            }
        }
    }

    public class ReputationTable
    {
        public List<ReputationRow> Rows { get; set; }

        public void Load()
        // Load the table from the database
        {
            List<ReputationRow> newRows = new List<ReputationRow>();

            MySqlDataReader reader = Sql.GetCommand("SELECT * FROM Reputation;").ExecuteReader();

            try
            {
                while (reader.Read())
                {
                    newRows.Add(new ReputationRow(
                        reader.GetInt32(0),
                        reader.GetUInt64(1),
                        reader.GetString(2)));
                }
            }
            catch {}

            reader.Close();

            Rows = newRows;
        }
    }

    public class ReputationRow
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public List<(IEmote, int)> Emotes { get; set; }

        public ReputationRow()
        {
            Id = 0;
        }

        public ReputationRow(int id, ulong guildId, string emotes)
        {
            Id = id;
            GuildId = guildId;

            Emotes = new List<(IEmote, int)>();

            emotes = EString.FromEncoded(emotes).Value;

            if (!string.IsNullOrEmpty(emotes))
            {
                foreach (string emoteString in emotes.Split(","))
                {
                    int value = int.Parse(emoteString.Split("///").Last());
                    if (Emote.TryParse(emoteString.Split("///").First(), out Emote emote))
                    {
                        Emotes.Add((emote, value));
                    }
                    else
                    {
                        Emotes.Add((new Emoji(emoteString), value));
                    }
                }
            }
        }

        public string GetEmotesString()
        {
            string emotesString = "";

            for (int i = 0; i < Emotes.Count; i++)
            {
                emotesString += $"{Emotes[i].Item1}///{Emotes[i].Item2}";
                if (i != Emotes.Count - 1)
                {
                    emotesString += ",";
                }
            }

            return EString.FromDecoded(emotesString).EncodedValue;
        }
    }

    public class ReputationUserRow
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int Reputation { get; set; }

        public ReputationUserRow()
        {
            Id = 0;
        }

        public ReputationUserRow(int id, ulong guildId, ulong userId, int reputation)
        {
            Id = id;
            GuildId = guildId;
            UserId = userId;
            Reputation = reputation;
        }
    }
}
