using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.ComponentModel;

using Lewt.Shared.World;
using Lewt.Shared.Rendering;
using Lewt.Shared.Magic;

using OpenTK;

namespace Lewt.Shared.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PlaceableInEditorAttribute : Attribute
    {
    }

    public enum DamageType
    {
        Melee = 0,
        Ranged = 1,
        Magical = 2,
        Blade = 4,
        Blunt = 8,
        Bow = 16,
        Fire = 32,
        Acid = 64,
        Freeze = 128,
        Electricity = 256
    }

    public class AppliedMagicalEffect
    {
        private ulong myStartTime;

        public readonly MagicalEffect Effect;
        public readonly Entity Entity;

        public double SecondsElapsed
        {
            get
            {
                return Tools.TicksToSeconds( Entity.Map.TimeTicks - myStartTime );
            }
        }

        public AppliedMagicalEffect( MagicalEffect effect, Entity ent )
        {
            Effect = effect;
            Entity = ent;
            myStartTime = ent.Map.TimeTicks;
        }

        public void Think()
        {
            Effect.Think( Entity, SecondsElapsed );
        }
    }

    public delegate void NetworkedUpdateHandler( byte[] payload );

    public class Entity
    {
        public static Entity Clone( Entity ent )
        {
            Type t = ent.GetType();
            ConstructorInfo c = t.GetConstructor( new Type[] { t } );
            return c.Invoke( new object[] { ent } ) as Entity;
        }

        private Chunk myChunk;
        private Map myMap;
        private BoundingBox myBB;
        private bool myRemoved;
        private double myProbability;
        private bool myGraphicsInitialized;
        private bool myCollideWithEntities;
        private UInt32 myEntityID;
        private List<KeyValuePair<String, NetworkedUpdateHandler>> myNetworkedUpdateHandlers;

        private List<AppliedMagicalEffect> myMagicalEffects;

        public bool SendToClients;
        public bool IsUseable;

        public event EventHandler Removed;

        protected bool IsServer
        {
            get
            {
                return myMap.IsServer;
            }
        }

        protected bool IsClient
        {
            get
            {
                return myMap.IsClient;
            }
        }

        [Browsable( false )]
        public UInt32 EntityID
        {
            get
            {
                return myEntityID;
            }
        }

        [Browsable( false )]
        public bool GraphicsInitialized
        {
            get
            {
                return myGraphicsInitialized;
            }
        }

        public bool Selected;

        [Browsable( false )]
        public Chunk Chunk
        {
            get
            {
                return myChunk;
            }
            set
            {
                myChunk = value;
            }
        }

        [Browsable( false )]
        public Map Map
        {
            get
            {
                return myMap;
            }
            set
            {
                myMap = value;
            }
        }

        [Browsable( false )]
        public bool IsRemoved
        {
            get
            {
                return myRemoved;
            }
        }

        [CategoryAttribute( "Position" ), DescriptionAttribute( "Horizontal position of the entity in the chunk" ), DisplayName( "X" )]
        public double OriginX
        {
            get
            {
                return myBB.OriginX;
            }
            set
            {
                myBB.OriginX = value;
            }
        }

        [CategoryAttribute( "Position" ), DescriptionAttribute( "Vertical position of the entity in the chunk" ), DisplayName( "Y" )]
        public double OriginY
        {
            get
            {
                return myBB.OriginY;
            }
            set
            {
                if ( value != myBB.OriginY && Chunk != null )
                    Chunk.UpdateEntitySorting();

                myBB.OriginY = value;
            }
        }

        [Browsable( false )]
        public Vector2d BBCentre
        {
            get
            {
                return new Vector2d( myBB.Left + myBB.Width * 0.5, myBB.Top + myBB.Height * 0.5 );
            }
        }

        [Browsable( false )]
        public double Width
        {
            get
            {
                return myBB.Width;
            }
        }

        [Browsable( false )]
        public double Height
        {
            get
            {
                return myBB.Height;
            }
        }

        [Browsable( false )]
        public float ScreenX
        {
            get
            {
                return (int) ( ( (float) OriginX - MapRenderer.CameraX ) * 16.0f ) * MapRenderer.CameraScale + MapRenderer.ScreenWidth / 2.0f;
            }
        }
        [Browsable( false )]
        public float ScreenY
        {
            get
            {
                return (int) ( ( (float) OriginY - MapRenderer.CameraY ) * 16.0f ) * MapRenderer.CameraScale + MapRenderer.ScreenHeight / 2.0f;
            }
        }

        [Browsable( false )]
        public float DrawScale
        {
            get
            {
                return MapRenderer.CameraScale;
            }
        }

        [Browsable( false )]
        public bool CollideWithEntities
        {
            get
            {
                return myCollideWithEntities;
            }
            set
            {
                myCollideWithEntities = value;
            }
        }

        [CategoryAttribute( "Misc" ), DescriptionAttribute( "String representing the entity type" )]
        public String TypeName
        {
            get
            {
                return GetType().FullName;
            }
        }

        [CategoryAttribute( "Misc" ), DescriptionAttribute( "Probability of the entity being placed during world gen" )]
        public Double Probability
        {
            get
            {
                return myProbability;
            }
            set
            {
                myProbability = value;
            }
        }
        
        public Entity()
        {
            SendToClients = false;

            myProbability = 1.0;
            SetBoundingBox( -0.5, -0.5, 1, 1 );
            myMagicalEffects = new List<AppliedMagicalEffect>();
            myEntityID = 0xFFFFFFFF;
            myNetworkedUpdateHandlers = new List<KeyValuePair<string, NetworkedUpdateHandler>>();
            OnRegisterNetworkedUpdateHandlers();

            OnInitialize();
        }

        public Entity( Entity copy )
        {
            SendToClients = copy.SendToClients;

            myProbability = copy.Probability;
            myBB = copy.myBB;
            myCollideWithEntities = copy.CollideWithEntities;
            myMagicalEffects = new List<AppliedMagicalEffect>();
            myEntityID = 0xFFFFFFFF;
            myNetworkedUpdateHandlers = new List<KeyValuePair<string, NetworkedUpdateHandler>>();
            OnRegisterNetworkedUpdateHandlers();

            OnInitialize();
        }

        public Entity( BinaryReader reader, bool sentFromServer )
        {
            SendToClients = false;

            myProbability = 1.0;
            myEntityID = reader.ReadUInt32();

            myBB = new BoundingBox( reader );
            myCollideWithEntities = reader.ReadBoolean();
            myMagicalEffects = new List<AppliedMagicalEffect>();
            myNetworkedUpdateHandlers = new List<KeyValuePair<string, NetworkedUpdateHandler>>();
            OnRegisterNetworkedUpdateHandlers();

            OnInitialize();
        }

        internal void SetID( UInt32 id )
        {
            myEntityID = id;
        }

        public void Use( Player user )
        {
            if ( IsServer && IsUseable )
            {
                SendStateUpdate( "Use", BitConverter.GetBytes( user.EntityID ) );

                OnUse( user );
            }
        }

        protected virtual void OnUse( Player user )
        {

        }

        public void AddMagicalEffect( MagicalEffect effect )
        {
            myMagicalEffects.Add( new AppliedMagicalEffect( effect, this ) );
        }

        public void RemoveMagicalEffect( MagicalEffect effect )
        {
            for ( int i = myMagicalEffects.Count - 1; i >= 0; --i )
                if ( myMagicalEffects[ i ].Effect == effect )
                    myMagicalEffects.RemoveAt( i );
        }

        public virtual void Think( double deltaTime )
        {
            if ( !myGraphicsInitialized )
                InitializeGraphics();

            for ( int i = myMagicalEffects.Count - 1; i >= 0; --i )
                myMagicalEffects[ i ].Think();
        }

        protected void SetBoundingBox( double width, double height )
        {
            double oldOriginX = myBB.OriginX;
            double oldOriginY = myBB.OriginY;

            myBB = new BoundingBox( -width / 2.0, -height / 2.0, width, height );
            myBB.OriginX = oldOriginX;
            myBB.OriginY = oldOriginY;
        }

        protected void SetBoundingBox( double offsetX, double offsetY, double width, double height )
        {
            double oldOriginX = myBB.OriginX;
            double oldOriginY = myBB.OriginY;

            myBB = new BoundingBox( offsetX, offsetY, width, height );
            myBB.OriginX = oldOriginX;
            myBB.OriginY = oldOriginY;
        }

        public virtual bool ShouldCollide( Entity ent )
        {
            return CollideWithEntities && ent.CollideWithEntities;
        }

        public bool MoveHorizontal( double dist )
        {
            if ( dist == 0 )
                return false;

            bool collided = false;
            Entity collidedEnt = null;

            if ( CollideWithEntities )
            {
                double thresholdA = ( dist < 0 ? myBB.Left : myBB.Right ) + dist;
                double thresholdB = ( dist > 0 ? myBB.Left : myBB.Right ) + dist;

                List<Chunk> chunks = Chunk.Neighbours.ToList();
                chunks.Add( Chunk );

                for ( int i = 0; i < chunks.Count; ++i )
                    if ( chunks[ i ] != null )
                        foreach ( Entity ent in chunks[ i ].Entities )
                        {
                            if ( ent == this || ent.myBB.Top >= myBB.Bottom || ent.myBB.Bottom <= myBB.Top || !ShouldCollide( ent ) || !ent.ShouldCollide( this ) )
                                continue;

                            double edgeA = dist < 0 ? ent.myBB.Right : ent.myBB.Left;
                            double edgeB = dist > 0 ? ent.myBB.Right : ent.myBB.Left;

                            if ( edgeA < thresholdA == dist < 0 || edgeB > thresholdB == dist < 0 )
                                continue;

                            thresholdA = edgeA;
                            collidedEnt = ent;
                            collided = true;
                        }

                if ( dist < 0 )
                    dist = thresholdA - myBB.Left;
                else
                    dist = thresholdA - myBB.Right;
            }

            if ( dist < 0 )
            {
                for ( int x = (int) Math.Floor( myBB.Left ); x > Math.Floor( myBB.Left + dist ); --x )
                {
                    for ( int y = (int) Math.Floor( myBB.Top ); y <= Math.Floor( myBB.Bottom - 1.0 / 256.0 ); ++y )
                    {
                        if ( Map.GetTile( x - 1, y ).IsSolid )
                        {
                            myBB.Left = x;
                            OnCollideWithWall();
                            return true;
                        }
                    }
                }
            }
            else
            {
                for ( int x = (int) Math.Ceiling( myBB.Right ); x < Math.Ceiling( myBB.Right + dist ); ++x )
                {
                    for ( int y = (int) Math.Floor( myBB.Top ); y <= Math.Floor( myBB.Bottom - 1.0 / 256.0 ); ++y )
                    {
                        if ( Map.GetTile( x, y ).IsSolid )
                        {
                            myBB.Right = x;
                            OnCollideWithWall();
                            return true;
                        }
                    }
                }
            }

            OriginX += dist;

            if ( collided )
                OnCollideWithEntity( collidedEnt );

            return collided;
        }

        public bool MoveVertical( double dist )
        {
            if ( dist == 0 )
                return false;

            bool collided = false;
            Entity collidedEnt = null;

            if ( CollideWithEntities )
            {
                double thresholdA = ( dist < 0 ? myBB.Top : myBB.Bottom ) + dist;
                double thresholdB = ( dist > 0 ? myBB.Top : myBB.Bottom ) + dist;

                List<Chunk> chunks = Chunk.Neighbours.ToList();
                chunks.Add( Chunk );

                for ( int i = 0; i < chunks.Count; ++i )
                    if( chunks[ i ] != null )
                        foreach ( Entity ent in chunks[ i ].Entities )
                        {
                            if ( ent == this || ent.myBB.Left >= myBB.Right || ent.myBB.Right <= myBB.Left || !ShouldCollide( ent ) || !ent.ShouldCollide( this ) )
                                continue;

                            double edgeA = dist < 0 ? ent.myBB.Bottom : ent.myBB.Top;
                            double edgeB = dist > 0 ? ent.myBB.Bottom : ent.myBB.Top;

                            if ( edgeA < thresholdA == dist < 0 || edgeB > thresholdB == dist < 0 )
                                continue;

                            thresholdA = edgeA;
                            collidedEnt = ent;
                            collided = true;
                        }

                if ( dist < 0 )
                    dist = thresholdA - myBB.Top;
                else
                    dist = thresholdA - myBB.Bottom;
            }

            if ( dist < 0 )
            {
                for ( int y = (int) Math.Floor( myBB.Top ); y > Math.Floor( myBB.Top + dist ); --y )
                {
                    for ( int x = (int) Math.Floor( myBB.Left ); x <= Math.Floor( myBB.Right - 1.0 / 256.0 ); ++x )
                    {
                        if ( Map.GetTile( x, y - 1 ).IsSolid )
                        {
                            myBB.Top = y;
                            OnCollideWithWall();
                            return true;
                        }
                    }
                }
            }
            else
            {
                for ( int y = (int) Math.Ceiling( myBB.Bottom ); y < Math.Ceiling( myBB.Bottom + dist ); ++y )
                {
                    for ( int x = (int) Math.Floor( myBB.Left ); x <= Math.Floor( myBB.Right - 1.0 / 256.0 ); ++x )
                    {
                        if ( Map.GetTile( x, y ).IsSolid )
                        {
                            myBB.Bottom = y;
                            OnCollideWithWall();
                            return true;
                        }
                    }
                }
            }

            OriginY += dist;

            if ( collided )
                OnCollideWithEntity( collidedEnt );
            
            return collided;
        }

        protected virtual void OnCollideWithWall()
        {

        }

        protected virtual void OnCollideWithEntity( Entity ent )
        {

        }

        protected virtual void OnInitialize()
        {

        }

        public void EnterMap( Map map )
        {
            myRemoved = false;

            OnEnterMap( map );
        }

        protected virtual void OnEnterMap( Map map )
        {

        }

        protected virtual void InitializeGraphics()
        {
            myGraphicsInitialized = true;
        }

        public virtual void Render()
        {
            if ( !myGraphicsInitialized )
                InitializeGraphics();
        }

        public virtual void EditorRender()
        {
            if ( !myGraphicsInitialized )
                InitializeGraphics();
        }

        public void Remove()
        {
            OnRemove();
            if ( Removed != null )
                Removed( this, new EventArgs() );
            myRemoved = true;
        }

        protected virtual void OnRemove()
        {

        }

        public virtual void PostWorldInitialize( bool editor = false )
        {

        }

        public bool IsIntersecting( Vector2d pos )
        {
            return myBB.IsIntersecting( pos );
        }

        public bool IsIntersecting( Entity ent )
        {
            if ( ent == this )
                return true;

            return myBB.IsIntersecting( ent.myBB );
        }

        public void Save( BinaryWriter writer, bool sendToClient )
        {
            writer.Write( GetType().FullName );
            writer.Write( EntityID );
            OnSave( writer, sendToClient );
        }

        protected virtual void OnSave( BinaryWriter writer, bool sendToClient )
        {
            myBB.WriteToStream( writer );
            writer.Write( myCollideWithEntities );
        }

        public byte[] ToByteArray( bool sendToClient )
        {
            MemoryStream memStream = new MemoryStream();
            Save( new BinaryWriter( memStream ), sendToClient );
            byte[] data = new byte[ memStream.Length ];
            memStream.Position = 0;
            memStream.Read( data, 0, data.Length );

            return data;
        }

        public static Entity Load( BinaryReader reader, bool sentFromServer )
        {
            Entity ent = null;
            String typeName = reader.ReadString();

            Exception innerException = new Exception();
            
            try
            {
                Type t = Assembly.GetAssembly( typeof( Entity ) ).GetType( typeName )
                    ?? Scripts.GetType( typeName );
                ConstructorInfo c = t.GetConstructor( new Type[] { typeof( BinaryReader ), typeof( bool ) } );
                ent = c.Invoke( new object[] { reader, sentFromServer } ) as Entity;
            }
            catch( Exception e )
            {
                innerException = e;
            }

            if ( ent == null )
                throw new Exception( "Entity of type '" + typeName + "' could not be created!", innerException );

            return ent;
        }

        protected void RegisterNetworkedUpdateHandler( String name, NetworkedUpdateHandler handler )
        {
            myNetworkedUpdateHandlers.Add( new KeyValuePair<string, NetworkedUpdateHandler>( name, handler ) );
        }

        protected virtual void OnRegisterNetworkedUpdateHandlers()
        {
            RegisterNetworkedUpdateHandler( "SetPosition", delegate( byte[] payload )
            {
                OriginX = BitConverter.ToDouble( payload, 0 );
                OriginY = BitConverter.ToDouble( payload, sizeof( double ) );
            } );

            RegisterNetworkedUpdateHandler( "Use", delegate( byte[] payload )
            {
                OnUse( Map.GetEntity( BitConverter.ToUInt32( payload, 0 ) ) as Player );
            } );
        }

        public void SendPositionUpdate()
        {
            MemoryStream stream = new MemoryStream();
            stream.Write( BitConverter.GetBytes( OriginX ), 0, sizeof( double ) );
            stream.Write( BitConverter.GetBytes( OriginY ), 0, sizeof( double ) );
            SendStateUpdate( "SetPosition", stream );
            stream.Close();
        }

        protected void SendStateUpdate( String type )
        {
            byte[] data = new byte[ 1 ];
            data[ 0 ] = (byte) myNetworkedUpdateHandlers.IndexOf( myNetworkedUpdateHandlers.Find( x => ( x.Key == type ) ) );
            SendStateUpdate( data );
        }

        protected void SendStateUpdate( String type, Stream stream )
        {
            byte[] data = new byte[ stream.Length + 1 ];
            data[ 0 ] = (byte) myNetworkedUpdateHandlers.IndexOf( myNetworkedUpdateHandlers.Find( x => ( x.Key == type ) ) );
            stream.Position = 0;
            stream.Read( data, 1, (int) stream.Length );
            SendStateUpdate( data );
        }

        protected void SendStateUpdate( String type, byte[] payload )
        {
            byte[] data = new byte[ payload.Length + 1 ];
            data[ 0 ] = (byte) myNetworkedUpdateHandlers.IndexOf( myNetworkedUpdateHandlers.Find( x => ( x.Key == type ) ) );
            Array.Copy( payload, 0, data, 1, payload.Length );
            SendStateUpdate( data );
        }

        private void SendStateUpdate( byte[] data )
        {
            if ( SendToClients && Map != null && IsServer && Map.EntityUpdated != null )
                Map.EntityUpdated( this, data );
        }

        public void ReceiveStateUpdate( byte[] data )
        {
            byte type = data[ 0 ];
            byte[] payload = new byte[ data.Length - 1 ];
            Array.Copy( data, 1, payload, 0, payload.Length );

            if ( myNetworkedUpdateHandlers.Count <= type )
                throw new Exception( "Unknown entity update packet received (ID:" + type + ")" );
            else
            {
                myNetworkedUpdateHandlers[ type ].Value.Invoke( payload );
            }
        }
    }
}
