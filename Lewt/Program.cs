using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

using ResourceLib;

using OpenTK;

namespace Lewt
{
    class Program
    {
        [STAThread]
        static void Main( string[] args )
        {
            Res.RegisterManager( new Lewt.Shared.Rendering.RTextureManager() );
            Res.RegisterManager( new Lewt.Shared.RScriptManager() );
            Res.RegisterManager( new Lewt.Shared.World.RChunkTemplateManager() );

            LewtWindow window = new LewtWindow( 640, 512 );
            window.Run( 60.0, 60.0 );
            window.Dispose();

            if ( Lewt.Client.Networking.GameClient.Connected )
                Lewt.Client.Networking.GameClient.Disconnect();

            if ( Lewt.Server.Networking.GameServer.Running )
                Lewt.Server.Networking.GameServer.Stop();
        }
    }
}
