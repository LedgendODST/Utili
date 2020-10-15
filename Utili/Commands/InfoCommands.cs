﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Utili.Program;
using static Utili.MessageSender;

namespace Utili.Commands
{
    public class InfoCommands : ModuleBase<SocketCommandContext>
    {
        [Command("Hello"), Cooldown(2.5), Permission(Perm.BotOwner)]
        public async Task Hello(string arg)
        {
            await SendSuccessAsync(Context.Channel, "Hello", arg);
        }
    }
}
