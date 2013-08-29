using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lewt.Client.UI;
using Lewt.Shared.Entities;
using OpenTK;
using Lewt.Shared.Rendering;
using ResourceLib;
using Lewt.Shared.Magic;
using Lewt.Shared;

namespace Lewt
{
    enum StatBarType
    {
        HitPoints,
        ManaLevel
    }

    class StatBar : UIObject
    {
        private Player myPlayer;

        private Sprite myTopSprite;
        private Sprite myEmptySprite;
        private Sprite myMidSprite;
        private Sprite myFilledSprite;
        private Sprite myBottomSprite;

        private Sprite myBackMarkerRingSprite;
        private Sprite myFrontMarkerRingSprite;

        private double myMarkerRatio;

        private double myOldRatio;
        private Vector2 myOldPosition;

        public readonly StatBarType StatBarType;

        public int Value
        {
            get
            {
                switch ( StatBarType )
                {
                    case StatBarType.HitPoints:
                        return myPlayer.HitPoints;
                    case Lewt.StatBarType.ManaLevel:
                        return myPlayer.ManaLevel;
                    default:
                        return 0;
                }
            }
        }

        public int MaximumValue
        {
            get
            {
                switch ( StatBarType )
                {
                    case StatBarType.HitPoints:
                        return myPlayer.MaxHitPoints;
                    case Lewt.StatBarType.ManaLevel:
                        return myPlayer.MaxManaLevel;
                    default:
                        return 0;
                }
            }
        }

        public double Ratio
        {
            get
            {
                if ( MaximumValue == 0 )
                    return 0.0;
                else
                    return (double) Value / (double) MaximumValue;
            }
        }

        public double MarkerRatio
        {
            get
            {
                return myMarkerRatio;
            }
            set
            {
                myMarkerRatio = ( value >= 0.0 && value <= 1.0 ) ? value : -1.0;
                UpdateMarkerRing();
            }
        }

        public StatBar( Player player, StatBarType type, float height )
            : base( new Vector2( 16.0f, height ) )
        {
            myPlayer = player;
            StatBarType = type;

            Texture tex = Res.Get<Texture>( "images_gui_stattubes" );

            float left = ( type == Lewt.StatBarType.HitPoints ) ? 0.0f : 16.0f;

            myTopSprite = new Sprite( tex )
            {
                SubrectLeft = left,
                SubrectWidth = 16.0f,
                SubrectHeight = 8.0f
            };

            myEmptySprite = new Sprite( tex )
            {
                SubrectLeft = left,
                SubrectTop = 8.0f,
                SubrectWidth = 16.0f,
                SubrectHeight = 4.0f
            };

            myMidSprite = new Sprite( tex )
            {
                SubrectLeft = left,
                SubrectTop = 12.0f,
                SubrectWidth = 16.0f,
                SubrectHeight = 4.0f
            };

            myFilledSprite = new Sprite( tex )
            {
                SubrectLeft = left,
                SubrectTop = 16.0f,
                SubrectWidth = 16.0f,
                SubrectHeight = 8.0f
            };

            myBottomSprite = new Sprite( tex )
            {
                SubrectLeft = left,
                SubrectTop = 24.0f,
                SubrectWidth = 16.0f,
                SubrectHeight = 8.0f
            };

            tex = Res.Get<Texture>( "images_gui_statmarkerring" );

            myBackMarkerRingSprite = new Sprite( tex )
            {
                SubrectHeight = 8.0f
            };

            myFrontMarkerRingSprite = new Sprite( tex )
            {
                SubrectTop = 8.0f,
                SubrectHeight = 8.0f
            };

            UpdateSprites();

            MarkerRatio = -1.0;
        }

        private void UpdateSprites()
        {
            double newRatio = myOldRatio + ( Ratio - myOldRatio ) * 0.25;
            if ( Math.Abs( newRatio - Ratio ) < 2.0 / Height )
                newRatio = Ratio;

            float totalHeight = Height - 20.0f;
            float filledHeight = totalHeight * (float) newRatio;
            float emptyHeight = totalHeight - filledHeight;

            myFilledSprite.Height = filledHeight;
            myEmptySprite.Height = emptyHeight;

            myTopSprite.Position = Position;
            myEmptySprite.Position = Position + new Vector2( 0.0f, 8.0f );
            myMidSprite.Position = Position + new Vector2( 0.0f, 8.0f + emptyHeight );
            myFilledSprite.Position = Position + new Vector2( 0.0f, 12.0f + emptyHeight );
            myBottomSprite.Position = Position + new Vector2( 0.0f, 12.0f + emptyHeight + filledHeight );

            myOldRatio = newRatio;
            myOldPosition = Position;
        }

        private void UpdateMarkerRing()
        {
            if ( MarkerRatio == -1.0 )
                return;

            float totalHeight = Height - 20.0f;
            float ringPos = Height - totalHeight * (float) MarkerRatio - 12.0f;

            myBackMarkerRingSprite.Position = myFrontMarkerRingSprite.Position =
                Position + new Vector2( 0.0f, ringPos );
        }

        protected override void OnRender( Vector2 renderPosition = new Vector2() )
        {
            base.OnRender( renderPosition );

            if ( myOldRatio != Ratio || myOldPosition != Position )
                UpdateSprites();

            if( StatBarType == Lewt.StatBarType.ManaLevel )
                MarkerRatio = ( myPlayer.EquippedSpell != null && myPlayer.OrbEquipped ) ?
                    (double) myPlayer.EquippedSpell.ManaCost / (double) MaximumValue : -1.0;

            if ( MarkerRatio != -1.0 )
                myBackMarkerRingSprite.Render();

            myTopSprite.Render();
            if ( myOldRatio < 1.0 )
                myEmptySprite.Render();
            myMidSprite.Render();
            if ( myOldRatio > 0.0 )
                myFilledSprite.Render();
            myBottomSprite.Render();

            if ( MarkerRatio != -1.0 )
                myFrontMarkerRingSprite.Render();
        }
    }
}
