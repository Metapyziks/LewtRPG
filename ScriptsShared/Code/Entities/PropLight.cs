using System;
using System.Collections.Generic;
using System.Text;

using OpenTK.Graphics;

using ResourceLib;

using Lewt.Shared.Entities;
using Lewt.Shared.Rendering;
using Lewt.Shared.World;

namespace Scripts.Entities
{
    [PlaceableInEditor]
    public class PropLight : Light, IDamageable
    {
        private Texture[] stTextures;

        private AnimatedSprite mySprite;
        private int mySkinID;

        private int myHealth;

        public int SkinID
        {
            get
            {
                return mySkinID;
            }
            set
            {
                mySkinID = value;
                UpdateSkin();
            }
        }

        public PropLight()
        {
            SkinID = 0;
            CollideWithEntities = true;
            SetBoundingBox(0.25, 0.125);

            myHealth = 50;
        }

        public PropLight( PropLight copy )
            : base( copy )
        {
            mySkinID = copy.SkinID;
            myHealth = copy.myHealth;

            UpdateSkin( false );
        }

        public PropLight( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {
            mySkinID = reader.ReadByte();
            myHealth = reader.ReadByte();

            UpdateSkin( false );
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            SendToClients = true;
        }

        private void UpdateSkin( bool updateLight = true )
        {
            if ( stTextures != null )
                mySprite = new AnimatedSprite( stTextures[ mySkinID ], 16, 16, 8.0, MapRenderer.CameraScale )
                {
                    UseCentreAsOrigin = true
                };

            if ( updateLight )
            {
                if ( mySkinID < 2 )
                    LightColour = new LightColour( 0.75f, 0.5f, 0.25f );
                else
                    LightColour = new LightColour( 0.67f, 0.93f, 1.0f );
                Range = 4.0;
            }
        }

        protected override void InitializeGraphics()
        {
            if ( stTextures == null )
            {
                stTextures = new Texture[]
                {
                    Res.Get<Texture>( "images_props_lightstand" ),
                    Res.Get<Texture>( "images_props_walllight" ),
                    Res.Get<Texture>( "images_props_crystals" )
                };
            }
            
            UpdateSkin( false );

            base.InitializeGraphics();
        }

        public override void EditorRender()
        {
            if ( Selected )
                SpriteRenderer.DrawRect(
                    ScreenX - (float) Width * MapRenderer.CameraScale * 8.0f,
                    ScreenY - (float) Height * MapRenderer.CameraScale * 8.0f,
                    (float) Width * MapRenderer.CameraScale * 16.0f,
                    (float) Height * MapRenderer.CameraScale * 16.0f,
                    new Color4( 0, 255, 0, 63 ) );

            Render();
        }

        public override void Render()
        {
            base.Render();

            mySprite.X = ScreenX;
            mySprite.Y = ScreenY - 8.0f * MapRenderer.CameraScale;

            mySprite.Render();
        }

        protected override void OnSave( System.IO.BinaryWriter writer, bool sendToClient )
        {
            base.OnSave( writer, sendToClient );

            writer.Write( (byte) mySkinID );
            writer.Write( (byte) myHealth );
        }
        
        public void Hurt( Entity attacker, Entity weapon, DamageType damageType, int damage )
        {
            myHealth -= damage;

            if ( myHealth <= 0 )
            {
                Remove();
                Update();
            }
        }
    }
}
