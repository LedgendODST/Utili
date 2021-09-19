﻿using NewDatabase.Entities.Base;

namespace NewDatabase.Entities
{
    public class MessagePinningConfiguration : GuildEntity
    {
        public ulong PinChannelId { get; set; }
        public bool PinMessages { get; set; }

        public MessagePinningConfiguration(ulong guildId) : base(guildId) { }
    }
}
