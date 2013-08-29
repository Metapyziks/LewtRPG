using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ResourceLib;

using OpenTK;
using OpenTK.Graphics;

using Lewt.Shared;

using Lewt.Shared.Magic;
using Lewt.Shared.Rendering;

namespace Lewt.Shared.Entities
{
    public class MagicalProjectile : Projectile
    {
        private class Spark : Entity
        {
            private static Texture stTex;

            private Vector2d myVelocity;
            private AnimatedSprite mySprite;
            private Color4 myColour;

            public Spark( MagicalProjectile projectile, double maxSpeed = 1.0 )
            {
                OriginX = projectile.OriginX + Tools.Random() * 0.5 - 0.25;
                OriginY = projectile.OriginY + Tools.Random() * 0.5 - 0.25;

                double speed = Tools.Random() * maxSpeed;
                double angle = Tools.Random() * Math.PI * 2.0;

                myVelocity = new Vector2d( Math.Cos( angle ) * speed, Math.Sin( angle ) * speed );
                myColour = new Color4
                {
                    R = Math.Min( projectile.myColour.R + 0.5f * (float) Tools.Random(), 1.0f ),
                    G = Math.Min( projectile.myColour.G + 0.5f * (float) Tools.Random(), 1.0f ),
                    B = Math.Min( projectile.myColour.B + 0.5f * (float) Tools.Random(), 1.0f ),
                    A = 1.0f
                };
            }

            protected override void InitializeGraphics()
            {
                if ( stTex == null )
                    stTex = Res.Get<Texture>( "images_magic_sparks" );

                mySprite = new AnimatedSprite( stTex, 8, 8, Tools.Random() * 8.0 + 12.0, MapRenderer.CameraScale )
                {
                    UseCentreAsOrigin = true,
                    Colour = myColour
                };

                base.InitializeGraphics();
            }

            public override void Render()
            {
                base.Render();

                mySprite.X = ScreenX;
                mySprite.Y = ScreenY;

                mySprite.Render();
            }

            public override void Think( double deltaTime )
            {
                base.Think( deltaTime );

                OriginX += myVelocity.X * deltaTime;
                OriginY += myVelocity.Y * deltaTime;

                myColour.A *= 0.95f;
                mySprite.Colour = myColour;
            }
        }

        private static Texture stTex;

        protected Sprite mySprite;

        private MagicalEffect myEffect;
        private Color4 myColour;

        private List<Spark> mySparks;
        private ulong myLastSparkTime;
        private ulong myHitTime;

        private bool myHit;

        private Light myLight;

        public MagicalProjectile( Entity owner, Vector2d startPos, double angle, double speed, Color4 colour, MagicalEffect effect )
            : base( owner, startPos, new Vector2d( Math.Cos( angle ) * speed, Math.Sin( angle ) * speed ), 0 )
        {
            myEffect = effect;
            myColour = colour;

            myHit = false;

            CollideWithEntities = true;
            SetBoundingBox( -0.125, 0.125, 0.25, 0.25 );
        }

        public MagicalProjectile( MagicalProjectile copy )
            : base( copy )
        {
            myEffect = copy.myEffect;
            myColour = copy.myColour;

            myHit = copy.myHit;
            myHitTime = copy.myHitTime;
        }

        public MagicalProjectile( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {
            myColour = Tools.ReadColor4FromStream( reader );
            myEffect = MagicalEffect.Load( reader );
            myHit = reader.ReadBoolean();

            if ( myHit )
                myHitTime = reader.ReadUInt64();
        }

        protected override void OnEnterMap( World.Map map )
        {
            base.OnEnterMap( map );

            if ( IsClient )
            {
                myLight = new Light
                {
                    LightColour = new World.LightColour( myColour.R, myColour.G, myColour.B ),
                    Range = 2.0
                };

                mySparks = new List<Spark>();
                myLastSparkTime = 0;

                for ( int i = 0; i < 16; ++i )
                    mySparks.Add( new Spark( this, 2.0 ) );

                map.AddEntity( myLight );
            }
        }

        protected override void InitializeGraphics()
        {
            if ( stTex == null )
                stTex = Res.Get<Texture>( "images_magic_projectile" );

            mySprite = new Sprite( stTex, MapRenderer.CameraScale )
            {
                UseCentreAsOrigin = true,
                Colour = myColour
            };
            
            base.InitializeGraphics();
        }

        public override void Think( double deltaTime )
        {
            base.Think( deltaTime );

            if ( !Started && !myHit )
                return;

            if ( IsServer )
            {
                if ( myHit && !IsRemoved && Map.TimeTicks - myHitTime > Tools.SecondsToTicks( 1.0 ) )
                    Remove();
            }
            else
            {
                if ( Map.TimeTicks - myLastSparkTime > Tools.SecondsToTicks( 1.0 / 64.0 ) )
                {
                    if ( !myHit )
                        mySparks.Add( new Spark( this ) );

                    myLastSparkTime = Map.TimeTicks;

                    if ( mySparks.Count > ( myHit ? Math.Max( ( 1.0 - Tools.TicksToSeconds( Map.TimeTicks - myHitTime ) * 2.0 ) * 32, 0 ) : 16 ) )
                        mySparks.RemoveAt( 0 );
                }

                for( int i = mySparks.Count - 1; i >= 0; -- i )
                    mySparks[ i ].Think( deltaTime );

                if ( myHit )
                    myLight.LightColour = new World.LightColour( myLight.LightColour.R * 0.9f, myLight.LightColour.G * 0.9f, myLight.LightColour.B * 0.9f );
                
                myLight.OriginX = OriginX;
                myLight.OriginY = OriginY;

                myLight.Update();
            }
        }

        private void Hit()
        {
            Hit( Map.TimeTicks, new Vector2d( OriginX, OriginY ) );
        }

        private void Hit( ulong hitTime, Vector2d hitPos )
        {
            if ( IsServer )
            {
                Stop();

                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                stream.Write( BitConverter.GetBytes( hitTime ), 0, sizeof( UInt64 ) );
                stream.Write( BitConverter.GetBytes( OriginX ), 0, sizeof( Double ) );
                stream.Write( BitConverter.GetBytes( OriginY ), 0, sizeof( Double ) );
                SendStateUpdate( "Hit", stream );
                stream.Close();
            }

            myHitTime = hitTime;
            OriginX = hitPos.X;
            OriginY = hitPos.Y;
            myHit = true;

            if ( IsClient )
            {
                myLight.Range = 4.0;

                for ( int i = 0; i < 16; ++i )
                    mySparks.Add( new Spark( this, 2.0 ) );
            }
        }

        protected override void OnRegisterNetworkedUpdateHandlers()
        {
            base.OnRegisterNetworkedUpdateHandlers();

            RegisterNetworkedUpdateHandler( "Hit", delegate( byte[] payload )
            {
                Hit(
                    BitConverter.ToUInt64( payload, 0 ),
                    new Vector2d(
                        BitConverter.ToDouble( payload, sizeof( UInt64 ) ),
                        BitConverter.ToDouble( payload, sizeof( UInt64 ) + sizeof( Double ) )
                    )
                );
            } );
        }

        protected override void OnCollideWithEntity( Entity ent )
        {
            base.OnCollideWithEntity( ent );

            if ( IsServer && !myHit )
            {
                Hit();

                myEffect.Start( ent );
            }
        }

        protected override void OnCollideWithWall()
        {
            base.OnCollideWithWall();

            if ( IsServer && !myHit )
                Hit();
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            if ( IsClient )
            {
                myLight.Remove();
                myLight.Update();
            }
        }

        public override void Render()
        {
            base.Render();

            if ( !Started && !myHit )
                return;

            if ( !myHit )
            {
                mySprite.X = ScreenX;
                mySprite.Y = ScreenY;

                mySprite.Render();
            }

            for( int i = mySparks.Count - 1; i >= 0; -- i )
                mySparks[ i ].Render();
        }

        protected override void OnSave( System.IO.BinaryWriter writer, bool sendToClient )
        {
            base.OnSave( writer, sendToClient );

            myColour.WriteToStream( writer );
            myEffect.Save( writer );
            writer.Write( myHit );

            if ( myHit )
                writer.Write( myHitTime );
        }
    }
}
