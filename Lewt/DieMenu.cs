using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Shared.Entities;
using Lewt.Shared.Rendering;
using Lewt.Client.UI;
using Lewt.Client.Networking;

using OpenTK;

namespace Lewt
{
    class DieMenu : UIWindow
    {
        private Player myPlayer;
        public DieMenu( Player player ) {
            myPlayer = player;

            var label = new UILabel( Font.Large, new Vector2( 4, 4 ) )
            {
                Text = "You are dead"
            };
            AddChild( label );
            var button = new UIButton( new Vector2( 100, 20 ), new Vector2( 4, 24 ) )
            {
                Text = "Resurrect",
                CentreText = true
            };
            AddChild( button );
            button.Click += new MouseButtonEventHandler( button_Click );

            Width = 108 + PaddingLeft + PaddingRight;
            Height = 48 + PaddingTop + PaddingBottom;
        }

        void button_Click( object sender, OpenTK.Input.MouseButtonEventArgs e ) {
            GameClient.SendPlayerResurrect();
            Close();
        }
    }
}
