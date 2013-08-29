using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

using Lewt.Shared.Rendering;
using Lewt.Shared.Magic;
using Lewt.Shared.Stats;
using Lewt.Shared.World;
using Lewt.Shared.Items;
using System.IO;

namespace Lewt.Shared.Entities
{
    public class StartedTradingEventArgs : EventArgs
    {
        public readonly Entity Entity;
        public readonly Inventory PlayerInventory;
        public readonly Inventory EntityInventory;
        public readonly Boolean IsMerchant;

        public StartedTradingEventArgs( Entity ent, Inventory playerInventory, Inventory entityInventory, bool merchant )
        {
            Entity = ent;
            PlayerInventory = playerInventory;
            EntityInventory = entityInventory;
            IsMerchant = merchant;
        }
    }

    public delegate void StartedTradingEventHandler( object sender, StartedTradingEventArgs e );

    public class InventoryShownEventArgs : EventArgs
    {
        public readonly Inventory Inventory;

        public InventoryShownEventArgs( Inventory playerInventory )
        {
            Inventory = playerInventory;
        }
    }

    public delegate void InventoryShownEventHandler( object sender, InventoryShownEventArgs e );

    public class Player : Human
    {
        private Light myLight;
        private Text myText;
        private String myPlayerName;
        private World.Map myMap;

        public int UnusedPoints { get; set; }

        public String PlayerName
        {
            get
            {
                return myPlayerName;
            }
            set
            {
                myPlayerName = value;
                if ( myText != null )
                    myText.String = value;
            }
        }
        
        public Player()
        {
            foreach ( CharAttribute attrib in CharAttribute.GetAll() )
                SetBaseAttributeLevel( attrib, 10 );

            Inventory.SetCapacity( 24 );
            
            int items = (int)( Tools.Random() * 4 ) + 2;

            ItemInfo[] loots = Loot.GetAll();
            
            for ( int i = 0; i < items; ++i )
                Inventory.Add( new Loot( loots[ (int)( Tools.Random() * loots.Length ) ], Tools.Random() ) );

            Inventory.Add( new SpellOrb( SpellInfo.Get( "firebolt" ), 0.1 ) );
        }
        
        public Player( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {
            UnusedPoints = reader.ReadUInt16();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            RechargeHitPoints = true;
            
            myPlayerName = "";
        }

        protected override void InitializeGraphics()
        {
            base.InitializeGraphics();

            myText = new Text( Font.Large, 1.0f )
            {
                Colour = new OpenTK.Graphics.Color4( 1.0f, 1.0f, 1.0f, 1.0f ),
                String = PlayerName
            };
        }

        public void UseItem( Item item )
        {
            item.Use( this );
        }

        public override void Resurrect() {
            base.Resurrect();

            if ( IsClient ) {
                if ( !myLight.IsRemoved ) return;

                myLight = new Light
                {
                    LightColour = new World.LightColour( 1.0f, 1.0f, 1.0f ),
                    Range = 3.0
                };

                myLight.OriginX = OriginX;
                myLight.OriginY = OriginY - 0.25;

                myLight.Map = myMap;
                myLight.Update();
            }
        }

        protected override void OnEnterMap( World.Map map )
        {
            if ( IsClient )
            {
                myLight = new Light
                {
                    LightColour = new World.LightColour( 1.0f, 1.0f, 1.0f ),
                    Range = 3.0
                };

                myLight.OriginX = OriginX;
                myLight.OriginY = OriginY - 0.25;

                myLight.Map = map;
                myLight.Update();

                myMap = map;
            }
        }

        public override void Think( double deltaTime )
        {
            base.Think( deltaTime );

            if ( !IsAlive )
                return;

            if ( IsClient )
            {
                myLight.OriginX = OriginX;
                myLight.OriginY = OriginY - 0.25;
                myLight.Update();
            }
        }

        protected override void OnDie( Entity attacker, Entity weapon, DamageType damageType )
        {
            base.OnDie( attacker, weapon, damageType );

            if ( IsClient )
            {
                myLight.Remove();
                myLight.Update();
            }
        }

        public void RenderName()
        {
            if ( IsAlive && PlayerName != "" && myText != null )
            {
                int textWidth = myText.Font.CharWidth * myText.String.Length;
                myText.Position = new Vector2( ScreenX - textWidth / 2.0f, ScreenY - 24.0f * MapRenderer.CameraScale );

                myText.Render();
            }
        }

        public Entity GetNearestUseableEntity()
        {
            List<Entity> ents = Chunk.Entities.ToList();

            foreach ( Chunk neighbour in Chunk.Neighbours )
                ents.AddRange( neighbour.Entities );

            Entity closest = null;
            double bestDist = 1.0;

            foreach ( Entity ent in ents )
            {
                if ( ent == this || !ent.IsUseable )
                    continue;

                double dist = Math.Max( Math.Abs( ent.OriginX - OriginX ), Math.Abs( ent.OriginY - OriginY ) );

                if ( dist < bestDist )
                {
                    bestDist = dist;
                    closest = ent;
                }
            }

            return closest;
        }

        public event StartedTradingEventHandler StartedTrading;

        public void StartTrading( IContainer container, bool merchant )
        {
            if ( IsServer )
            {
                MemoryStream stream = new MemoryStream();
                stream.Write( BitConverter.GetBytes( ( container as Entity ).EntityID ), 0, sizeof( UInt32 ) );
                stream.Write( BitConverter.GetBytes( merchant ), 0, sizeof( bool ) );
                BinaryWriter writer = new BinaryWriter( stream );
                Inventory.Save( writer );
                container.Inventory.Save( writer );
                SendStateUpdate( "StartTrading", stream );
                stream.Close();
            }
        }

        public event InventoryShownEventHandler InventoryShown;

        public void ShowInventory()
        {
            if ( IsServer )
            {
                MemoryStream stream = new MemoryStream();
                Inventory.Save( new BinaryWriter( stream ) );
                SendStateUpdate( "ShowInventory", stream );
                stream.Close();
            }
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

        protected override void OnRegisterNetworkedUpdateHandlers()
        {
            base.OnRegisterNetworkedUpdateHandlers();

            RegisterNetworkedUpdateHandler( "StartTrading", delegate( byte[] payload )
            {
                Entity container = Map.GetEntity( BitConverter.ToUInt32( payload, 0 ) );
                bool merchant = BitConverter.ToBoolean( payload, sizeof( UInt32 ) );

                MemoryStream stream = new MemoryStream( payload );
                stream.Position = sizeof( UInt32 ) + sizeof( bool );
                BinaryReader reader = new BinaryReader( stream );
                Inventory = new Inventory( this, reader );
                Inventory otherInv = ( container as IContainer ).Inventory = new Inventory( container, reader );
                stream.Close();

                if ( StartedTrading != null )
                    StartedTrading( this, new StartedTradingEventArgs( container, Inventory, otherInv, merchant ) );
            } );

            RegisterNetworkedUpdateHandler( "ShowInventory", delegate( byte[] payload )
            {
                MemoryStream stream = new MemoryStream( payload );
                Inventory = new Inventory( this, new BinaryReader( stream ) );
                stream.Close();

                if ( InventoryShown != null )
                    InventoryShown( this, new InventoryShownEventArgs( Inventory ) );
            } );
        }

        protected override void OnSave( System.IO.BinaryWriter writer, bool sendToClient )
        {
            base.OnSave( writer, sendToClient );

            writer.Write( (ushort) UnusedPoints );
        }
    }
}
