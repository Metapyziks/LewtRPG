using System;
using System.Collections.Generic;
using System.Text;

using OpenTK;

using ResourceLib;

using Lewt.Shared.Rendering;
using Lewt.Shared.Entities;
using Lewt.Shared.World;
using Lewt.Shared.Items;
using Lewt.Shared;
using Lewt.Shared.Magic;

namespace Scripts.Entities
{
    [PlaceableInEditor]
    public class Chest : Entity, IContainer
    {
        private static Texture stTex;

        private Inventory myInventory;

        protected Sprite mySprite;

        Inventory IContainer.Inventory
        {
            get
            {
                if ( IsServer && myInventory == null )
                {
                    myInventory = new Inventory( this, 12 );
                    int items = 4 + (int)(Tools.Random() * 4);
                    SpellInfo[] spellTypes = SpellInfo.GetAll();
                    ItemInfo[] loots = Loot.GetAll();
                    for (int i = 0; i < items; ++i)
                    {
                        if (Tools.Random() < 0.25)
                            myInventory.Add(new SpellOrb(spellTypes[(int)(Tools.Random() * spellTypes.Length)], Tools.Random()));
                        else if (Tools.Random() < 0.5)
                            myInventory.Add(new SpellScroll(spellTypes[(int)(Tools.Random() * spellTypes.Length)], Tools.Random()));
                        else
                            myInventory.Add(new Loot(loots[(int)(Tools.Random() * loots.Length)], Tools.Random()));
                    }

                }

                return myInventory;
            }
            set
            {
                myInventory = value;
            }
        }

        public Chest()
        {
            SetBoundingBox( 1.0, 0.5 );
            CollideWithEntities = true;
        }

        public Chest( Chest copy )
            : base( copy )
        {

        }

        public Chest( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {
            if ( !sentFromServer )
                if ( reader.ReadBoolean() )
                    myInventory = new Inventory( this, reader );
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            IsUseable = true;
            SendToClients = true;
        }

        protected override void InitializeGraphics()
        {
            if ( stTex == null )
                stTex = Res.Get<Texture>( "images_props_chest" );

            mySprite = new Sprite( stTex, MapRenderer.CameraScale )
            {
                UseCentreAsOrigin = true
            };

            base.InitializeGraphics();
        }

        protected override void OnUse( Player user )
        {
            base.OnUse( user );

            if( IsServer )
                user.StartTrading( this, false );
        }

        public override void EditorRender()
        {
            Render();
        }

        public override void Render()
        {
            base.Render();

            mySprite.X = ScreenX;
            mySprite.Y = ScreenY - ( 8.0f * MapRenderer.CameraScale );

            mySprite.Render();
        }

        protected override void OnSave( System.IO.BinaryWriter writer, bool sendToClient )
        {
            base.OnSave( writer, sendToClient );

            if ( !sendToClient )
            {
                writer.Write( myInventory != null );

                if ( myInventory != null )
                    myInventory.Save( writer );
            }
        }
    }
}
