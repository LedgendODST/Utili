﻿using System.Threading.Tasks;

namespace Database
{
    public static class Database
    {
        internal static Config Config { get; private set; }

        public static async Task InitialiseAsync(bool useCache, string defaultPrefix)
        {
            Config = new Config();
            Config.Load();
            Config.DefaultPrefix = defaultPrefix;

            Sql.SetCredentials(Config.Server, Config.Port, Config.Database, Config.Username, Config.Password);
            await Sql.PingAsync();

            if (useCache) Cache.Initialise();
            Data.Premium.Initialise();
        }
    }
}
