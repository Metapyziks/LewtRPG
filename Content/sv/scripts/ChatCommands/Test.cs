using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lewt.Server.Networking;

namespace Scripts.ChatCommands
{
    public class Test
    {
        static void Initialise()
        {
            GameServer.RegisterCommand( "greet", new CommandDelegate( RunCommand ), 0 );
        }

        static String RunCommand( String[] args, ClientBase client )
        {
            return "Hello there, " + client.Nickname + "!";
        }
    }
}
