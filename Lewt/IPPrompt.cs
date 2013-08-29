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
    public delegate void IPPromptResultHandler( string address, int port );

    public class IPPrompt : UIWindow
    {
        private UITextBox myTextBox;
        private IPPromptResultHandler myHandler;
        private UIMessageBox myMsg;

        public String Result
        {
            get
            {
                return myTextBox.Text;
            }
        }

        public IPPrompt( IPPromptResultHandler handler )
            : base( new Vector2( 192, 128 ) )
        {
            myHandler = handler;

            CanClose = false;

            UILabel label = new UILabel( Font.Large, new Vector2( 4, 4 ) )
            {
                Text = "Please enter the IP\naddress of the server\nyou wish to join"
            };
            AddChild( label );

            myTextBox = new UITextBox( new Vector2( InnerWidth - 8, 20 ), new Vector2( 4, label.Bottom + 12 ) )
            {
                Text = "127.0.0.1"
            };
            AddChild( myTextBox );

            float btnWidth = InnerWidth / 2.0f - 6.0f;

            UIButton joinBtn = new UIButton( new Vector2( btnWidth, 24 ), new Vector2( 4, myTextBox.Bottom + 4 ) )
            {
                Text = "Join"
            };
            AddChild( joinBtn );

            UIButton cancelBtn = new UIButton( new Vector2( btnWidth, 24 ), new Vector2( btnWidth + 8, myTextBox.Bottom + 4 ) )
            {
                Text = "Cancel"
            };
            AddChild( cancelBtn );

            joinBtn.Click += delegate( object sender, MouseButtonEventArgs e )
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
            String address = Result.Split( ':' )[ 0 ];
            int port = Lewt.Client.Networking.GameClient.DefaultPort;
            if ( address.Length != Result.Length )
                port = int.Parse( Result.Split( ':' )[ 1 ] );

            myHandler( address, port );
        }

        private bool Validate()
        {
            try
            {
                System.Net.IPAddress.Parse( Result );
            }
            catch
            {
                Disable();

                if ( myMsg != null )
                    Parent.RemoveChild( myMsg );

                myMsg = new UIMessageBox( "Not a valid IP Address", "Error", true )
                {
                    Size = new Vector2( 192, 48 )
                };
                Parent.AddChild( myMsg );
                myMsg.Centre();

                myMsg.Closed += delegate( object sender, EventArgs e2 )
                {
                    myMsg = null;
                    Enable();
                };
                return false;
            }

            return true;
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            if( e.KeyChar == 13 )
            {
                if ( Validate() )
                {
                    Close();
                    SendToHandler();
                }
            }

        }

        public void FocusOnInput()
        {
            Focus();
            myTextBox.Focus();
        }
    }
}
