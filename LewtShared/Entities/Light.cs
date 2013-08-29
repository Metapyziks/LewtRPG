using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using OpenTK;
using OpenTK.Graphics;

using Lewt.Shared.Rendering;
using Lewt.Shared.World;

using ResourceLib;

namespace Lewt.Shared.Entities
{
    [PlaceableInEditor]
    public class Light : Entity
    {
        private static Texture stTex;
        
        private Sprite mySprite;
        private LightColour myLightColour;
        private double myRange = 4;

        [Browsable( false )]
        public LightColour LightColour
        {
            get
            {
                return myLightColour;
            }
            set
            {
                myLightColour = value;
            }
        }

        [CategoryAttribute( "Light" ), DescriptionAttribute( "Colour of the light" ), DisplayName( "Light Colour" )]
        public String LightColourString
        {
            get
            {
                return LightColour.R.ToString( "F" ) + " " + LightColour.G.ToString( "F" ) + " " + LightColour.B.ToString( "F" );
            }
            set
            {
                string[] split = value.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
                float r, g, b;
                try
                {
                    r = float.Parse( split[ 0 ] );
                }
                catch
                {
                    r = 0;
                }
                try
                {
                    g = float.Parse( split[ 1 ] );
                }
                catch
                {
                    g = 0;
                }
                try
                {
                    b = float.Parse( split[ 2 ] );
                }
                catch
                {
                    b = 0;
                }

                LightColour = new LightColour( r, g, b );
            }
        }

        [CategoryAttribute( "Light" ), DescriptionAttribute( "How many tiles the light reaches" )]
        public double Range
        {
            get
            {
                return myRange;
            }
            set
            {
                myRange = value;
            }
        }

        [Browsable( false )]
        public int MinimumX
        {
            get
            {
                return (int) Math.Floor( OriginX - myRange );
            }
        }

        [Browsable( false )]
        public int MinimumY
        {
            get
            {
                return (int) Math.Floor( OriginY - myRange );
            }
        }

        [Browsable( false )]
        public int MaximumX
        {
            get
            {
                return (int) Math.Ceiling( OriginX + myRange );
            }
        }

        [Browsable( false )]
        public int MaximumY
        {
            get
            {
                return (int) Math.Ceiling( OriginY + myRange );
            }
        }
        
        public Light()
        {
            LightColour = new LightColour( 1.0f, 1.0f, 1.0f );

            SetBoundingBox( 12.0 / 16.0, 16.0 / 16.0 );
        }
        
        public Light( Light copy )
            : base( copy )
        {
            LightColour = copy.LightColour;
            Range = copy.Range;
        }

        public Light( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {
            LightColour = new LightColour( reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() );
            myRange = reader.ReadDouble();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            SendToClients = true;
        }

        public virtual LightColour GetLightBrightness( Map map, double x, double y )
        {
            Vector2d origin = new Vector2d( OriginX, OriginY );
            Vector2d dest = new Vector2d( x, y );

            Vector2d diff = dest - origin;

            if ( diff.Length > myRange )
                return LightColour.Default;

            RayTraceResult res = RayTrace.Trace( map, origin, dest, false );

            float brightness = 1.0f;

            foreach ( TileCrossInfo info in res.CrossedTiles )
            {
                brightness -= (float) ( ( info.IsSolid ? myRange : 1.0 ) / myRange ) * (float) info.Duration;

                if ( brightness <= 0 )
                    return LightColour.Default;
            }

            return new LightColour( LightColour.R * brightness, LightColour.G * brightness, LightColour.B * brightness );
        }

        protected override void InitializeGraphics()
        {
            if( stTex == null )
                stTex = Res.Get<Texture>( "images_gui_lightbulb" );

            mySprite = new Sprite( stTex, MapRenderer.CameraScale )
            {
                UseCentreAsOrigin = true
            };

            base.InitializeGraphics();
        }

        public override void EditorRender()
        {
            base.EditorRender();

            if ( Selected )
                SpriteRenderer.DrawRect(
                    ScreenX - (float) Width * MapRenderer.CameraScale * 8.0f,
                    ScreenY - (float) Height * MapRenderer.CameraScale * 8.0f,
                    (float) Width * MapRenderer.CameraScale * 16.0f,
                    (float) Height * MapRenderer.CameraScale * 16.0f,
                    new Color4( 0, 255, 0, 63 ) );

            mySprite.X = ScreenX;
            mySprite.Y = ScreenY;
            mySprite.Colour = LightColour.ToColor4();
            
            mySprite.Render();
        }

        public void Update()
        {
            if( Map != null )
                Map.UpdateLight( this, IsRemoved );
        }

        public override void PostWorldInitialize( bool editor = false )
        {
            Update();
        }

        protected override void OnSave( System.IO.BinaryWriter writer, bool sendToClient )
        {
            base.OnSave( writer, sendToClient );

            writer.Write( LightColour.R );
            writer.Write( LightColour.G );
            writer.Write( LightColour.B );

            writer.Write( myRange );
        }
    }
}
