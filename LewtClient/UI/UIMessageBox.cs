using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ResourceLib;

using OpenTK;

namespace Lewt.Client.UI
{
    public class UIMessageBox : UIWindow
    {
        private UILabel myText;
        private bool myCentreText;

        public bool CentreText
        {
            get
            {
                return myCentreText;
            }
            set
            {
                myCentreText = value;
                if ( myCentreText )
                    myText.Centre();
                else
                    myText.Position = new Vector2( 4, 4 );
            }
        }

        public String Text
        {
            get
            {
                return myText.Text;
            }
            set
            {
                myText.Text = value;
                if( CentreText )
                    myText.Centre();
            }
        }

        public UIMessageBox( String message, String title, bool closeButton = true )
            : base( new Vector2( 480, 64 ) )
        {
            CanClose = closeButton;
            myCentreText = false;

            Title = title;

            myText = new UILabel( Shared.Rendering.Font.Large )
            {
                Text = message,
                Position = new Vector2( 4, 4 )
            };
            AddChild( myText );
        }
    }
}
