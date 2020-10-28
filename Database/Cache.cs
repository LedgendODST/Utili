﻿using Database.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Database
{
    /*
     * The Cache class is responsible for downloading the data from the database and
     * returning it when requested. Overall, this should reduce the average latency
     * for fetching data from the database.
     */

    internal static class Cache
    {
        public static bool Initialised = false;
        private static Timer Timer { get; set; }

        public static AutopurgeTable Autopurge { get; set; } = new AutopurgeTable();
        public static VoiceLinkTable VoiceLink { get; set; } = new VoiceLinkTable();
        public static MiscTable Misc { get; set; } = new MiscTable();

        public static void Initialise() 
        // Start the automatic cache downloads
        {
            DownloadTables();

            Timer = new Timer(5000); // The cache will be updated every 30 seconds.
            Timer.Elapsed += Timer_Elapsed;
            Timer.Start();

            Initialised = true;
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        // At regular intervals, call the download tables method.
        {
            DownloadTables();
        }

        private static void DownloadTables()
        {
            Autopurge.LoadAsync();
            VoiceLink.LoadAsync();
            Misc.LoadAsync();
        }
    }
}
