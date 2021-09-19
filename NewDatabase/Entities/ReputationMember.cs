﻿using NewDatabase.Entities.Base;

namespace NewDatabase.Entities
{
    public class ReputationMember : MemberEntity
    {
        public long Reputation { get; set; }
        
        public ReputationMember(ulong guildId, ulong memberId) : base(guildId, memberId) { }
    }
}
