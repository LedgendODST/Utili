﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;

namespace Utili.Utils
{
    static class Constants
    {
        public static IEmoji CheckmarkEmoji { get; } = new LocalCustomEmoji(833728004299030578, "checkmark");
        public static IEmoji CrossEmoji { get; } = new LocalCustomEmoji(833728037530370078, "cross");
    }
}
