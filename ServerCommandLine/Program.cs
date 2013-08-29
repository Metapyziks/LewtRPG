using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ResourceLib;

using Lewt.Server.Networking;

namespace Lewt.Server
{
    class Program
    {
        static void Main( string[] args )
        {
            Res.RegisterManager( new Lewt.Shared.Rendering.RTextureManager() );
            Res.RegisterManager( new Lewt.Shared.RScriptManager() );
            Res.RegisterManager( new Lewt.Shared.World.RChunkTemplateManager() );

            Res.LoadFromOrderFile( "Data" + System.IO.Path.DirectorySeparatorChar + "loadorder.txt" );

            Lewt.Shared.Scripts.Compile( Lewt.Shared.Scripts.Destination.Server );
            
            bool lan = false;
            int maxPlayers = 8;
            int port = GameServer.DefaultPort;
            bool passwordRequired = false;
            String password = "";
            String mapType = "";

            if ( args.Length != 0 )
            {
                for ( int i = 0; i < args.Length; ++i )
                {
                    if ( args[ i ] == "-lan" )
                        lan = true;
                    else if ( args[ i ] == "-maxplayers" )
                        maxPlayers = int.Parse( args[ ++i ] );
                    else if ( args[ i ] == "-port" )
                        port = int.Parse( args[ ++i ] );
                    else if ( args[ i ] == "-password" )
                    {
                        passwordRequired = true;
                        password = args[ ++i ];
                    }
                    else if ( args[ i ] == "-maptype" )
                        mapType = args[ ++i ];
                }

                Console.WriteLine( "Lan: " + lan.ToString() );
                Console.WriteLine( "Max Players: " + maxPlayers.ToString() );
                Console.WriteLine( "Port: " + port.ToString() );
                if( passwordRequired )
                    Console.WriteLine( "Password: " + password );
                Console.WriteLine( "Map Type: " + mapType.ToLower() );
            }
            else
            {
                Console.WriteLine( "Lan? [y/n] " );
                lan = Console.ReadKey().KeyChar.ToString().ToLower() == "y";
                Console.WriteLine();
                Console.WriteLine( "Max players? " );
                maxPlayers = int.Parse( Console.ReadLine() );
                Console.WriteLine( "Port? [default=" + GameServer.DefaultPort.ToString() + "] " );
                String portStr = Console.ReadLine();
                if ( portStr == "" )
                    port = GameServer.DefaultPort;
                else
                    port = int.Parse( portStr );
                Console.WriteLine( "Password Required? [y/n] " );
                passwordRequired = Console.ReadKey().KeyChar.ToString().ToLower() == "y";
                Console.WriteLine();
                if ( passwordRequired )
                {
                    Console.WriteLine( "Password? " );
                    password = Console.ReadLine();
                }
                Console.WriteLine( "Map type? [fort/cave/temple/mine] " );
                mapType = Console.ReadLine();
            }

            GameServer.PasswordRequired = passwordRequired;
            if ( passwordRequired )
                GameServer.Password = password;

            GameServer.ServerMessage += new ServerMessageEventHandler( ServerMessage );

            GameServer.Start( maxPlayers, port, lan );

            while ( !GameServer.Running )
                Thread.Sleep( 100 );

            Console.WriteLine( "Enter commands below" );

            while ( GameServer.Running )
            {
                String command = Console.ReadLine();
                GameServer.RunCommand( command );
            }
        }

        static void ServerMessage( object sender, ServerMessageEventArgs e )
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write( e.Message );
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
