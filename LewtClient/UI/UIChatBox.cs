using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

using ResourceLib;

using Lewt.Shared.Rendering;

namespace Lewt.Client.UI
{
    public class UIChatBox : UIObject
    {
        private struct ChatMessage
        {
            public String Message;
            public DateTime SentTime;

            public double Age
            {
                get
                {
                    return ( DateTime.Now - SentTime ).TotalSeconds;
                }
            }

            public ChatMessage( String message )
            {
                Message = message;
                SentTime = DateTime.Now;
            }
        }

        private UILabel myChatDisplay;
        private UISprite myCurrentMessageBack;
        private UILabel myCurrentMessageDisplay;
        private Queue<ChatMessage> myChatMessages;
        private String myCurrentChatMessage;

        public double MessageDisplayTime;

        public UIChatBox( float width )
            : this( 256, new Vector2() )
        {

        }

        public UIChatBox( float width, Vector2 position )
            : base( new Vector2( width, 24 ), position )
        {
            MessageDisplayTime = 10.0;

            myChatMessages = new Queue<ChatMessage>();

            Font font = Font.Large;

            myChatDisplay = new UILabel( font, 1.0f )
            {
                Colour = OpenTK.Graphics.Color4.White
            };
            AddChild( myChatDisplay );

            myCurrentMessageBack = new UISprite( new Sprite( width - 8, font.CharHeight + 8, new OpenTK.Graphics.Color4( 0, 0, 0, 191 ) ) )
            {
                Position = new Vector2( 4, 4 ),
                IsVisible = false
            };
            AddChild( myCurrentMessageBack );

            myCurrentMessageDisplay = new UILabel( font, 1.0f )
            {
                Colour = OpenTK.Graphics.Color4.White,
                Position = new Vector2( 8, 8 ),
                IsVisible = false
            };
            AddChild( myCurrentMessageDisplay );
        }

        public void StartTyping()
        {
            Focus();
            myCurrentChatMessage = "";
            myCurrentMessageDisplay.Text = "> ";
        }

        public void AddMessage( String message )
        {
            myChatMessages.Enqueue( new ChatMessage( message ) );
            UpdateChatDisplay();
        }

        private void UpdateChatDisplay()
        {
            myChatDisplay.Text = "";

            foreach ( ChatMessage message in myChatMessages )
                myChatDisplay.Text += message.Message;

            if ( IsFocused )
                myChatDisplay.Position = new Vector2( 8, myChatDisplay.Font.CharHeight - myChatDisplay.Height );
            else
                myChatDisplay.Position = new Vector2( 8, myChatDisplay.Font.CharHeight * 2 + 8 - myChatDisplay.Height );
        }

        protected override void OnFocus()
        {
            myCurrentMessageBack.IsVisible = true;
            myCurrentMessageDisplay.IsVisible = true;

            UpdateChatDisplay();
        }

        protected override void OnUnFocus()
        {
            myCurrentMessageBack.IsVisible = false;
            myCurrentMessageDisplay.IsVisible = false;

            UpdateChatDisplay();
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            if ( e.KeyChar == 27 )
                UnFocus();
            else if ( e.KeyChar == 13 )
            {
                Lewt.Client.Networking.GameClient.ChatMessage( myCurrentChatMessage );
                UnFocus();
            }
            else if ( e.KeyChar == 8 )
            {
                if ( myCurrentChatMessage.Length > 0 )
                    myCurrentChatMessage = myCurrentChatMessage.Substring( 0, myCurrentChatMessage.Length - 1 );
            }
            else
                myCurrentChatMessage += e.KeyChar;

            myCurrentMessageDisplay.Text = "> " + myCurrentChatMessage;
        }

        protected override void OnRender( Vector2 renderPosition = new Vector2() )
        {
            bool updateChat = false;

            while ( myChatMessages.Count != 0 && myChatMessages.Peek().Age >= MessageDisplayTime )
            {
                myChatMessages.Dequeue();
                updateChat = true;
            }

            if ( updateChat )
                UpdateChatDisplay();

        }
    }
}
