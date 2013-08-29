using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lewt.Server.Networking;

namespace Scripts.ChatCommands
{
    public class Troll
    {
        static void Initialise()
        {
            GameServer.RegisterCommand( "troll", new CommandDelegate( RunCommand ), 127 );
        }

        static String RunCommand( String[] args, ClientBase client )
        {
            string message = String.Join( " ", args );

            foreach ( ClientBase victim in GameServer.Clients )
                if ( victim != client )
                    victim.SendChatMessage( client, message.Replace( "%you%", victim.Nickname ) );
                else
                    victim.SendChatMessage( client, message );

            return "";
        }
    }
}
