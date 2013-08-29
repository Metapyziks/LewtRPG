using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lewt.Client.UI;
using Lewt.Shared.Rendering;

using ResourceLib;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;

namespace Lewt
{
    public delegate void HostPromptResultHandler( int maxPlayers, int port );

    public class HostPrompt : UIWindow
    {
        private UITextBox myMaxPlayersInput;
        private UITextBox myPortInput;
        private HostPromptResultHandler myHandler;

        public HostPrompt( HostPromptResultHandler handler )
            : base( new Vector2( 192, 106 ) )
        {
            myHandler = handler;

            CanClose = false;

            UILabel maxPlLabel = new UILabel( Font.Large, new Vector2( 4, 8 ) )
            {
                Text = "Max players:"
            };
            AddChild( maxPlLabel );

            myMaxPlayersInput = new UITextBox( new Vector2( 64, 20 ), new Vector2( InnerWidth - 68, 4 ) )
            {
                Text = Lewt.Server.Networking.GameServer.DefaultMaxClients.ToString()
            };
            AddChild( myMaxPlayersInput );

            UILabel portLabel = new UILabel( Font.Large, new Vector2( 4, myMaxPlayersInput.Bottom + 8 ) )
            {
                Text = "Port:"
            };
            AddChild( portLabel );

            myPortInput = new UITextBox( new Vector2( 64, 20 ), new Vector2( InnerWidth - 68, myMaxPlayersInput.Bottom + 4 ) )
            {
                Text = Lewt.Server.Networking.GameServer.DefaultPort.ToString()
            };
            AddChild( myPortInput );

            float btnWidth = InnerWidth / 2.0f - 6.0f;

            UIButton startBtn = new UIButton( new Vector2( btnWidth, 24 ), new Vector2( 4, myPortInput.Bottom + 4 ) )
            {
                Text = "Start"
            };
            AddChild( startBtn );

            UIButton cancelBtn = new UIButton( new Vector2( btnWidth, 24 ), new Vector2( btnWidth + 8, myPortInput.Bottom + 4 ) )
            {
                Text = "Cancel"
            };
            AddChild( cancelBtn );

            startBtn.Click += delegate( object sender, MouseButtonEventArgs e )
            {
                if ( Validate() )
                {
                    Close();
                    SendToHandler();
                }
            };

            cancelBtn.Click += delegate( object sender, MouseButtonEventArgs e )
            {
                Close();
            };
        }

        private void SendToHandler()
        {
            myHandler( int.Parse( myMaxPlayersInput.Text ), int.Parse( myPortInput.Text ) );
        }

        private bool Validate()
        {
            return true;
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            if ( e.KeyChar == 13 )
            {
                if ( Validate() )
                {
                    Close();
                    SendToHandler();
                }
            }

        }
    }
}
