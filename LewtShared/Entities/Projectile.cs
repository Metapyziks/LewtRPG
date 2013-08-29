using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lewt.Shared.World;
using Lewt.Shared.Entities;

using OpenTK;

namespace Lewt.Shared.Entities
{
    public class Projectile : Entity
    {
        private Vector2d myStartPos;
        private ulong myStartTime;
        private bool myStarted;
        private double myDistanceTravelled;
        private UInt32 myOwnerID;

        public double Range;
        public Entity Owner;
        public Vector2d Velocity;

        public bool Started
        {
            get
            {
                return myStarted;
            }
        }

        public Projectile( Entity owner, Vector2d startPos, Vector2d velocity, double range = 0 )
        {
            myStartTime = 0;

            Owner = owner;

            OriginX = startPos.X;
            OriginY = startPos.Y;

            myDistanceTravelled = 0;

            Velocity = velocity;

            Range = range;
        }

        public Projectile( Projectile copy )
            : base( copy )
        {
            myStartPos = copy.myStartPos;
            myStartTime = copy.myStartTime;
            myStarted = copy.myStarted;
            Owner = copy.Owner;
            Range = copy.Range;
            Velocity = copy.Velocity;
            myDistanceTravelled = copy.myDistanceTravelled;
        }

        public Projectile( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {
            myOwnerID = reader.ReadUInt32();
            myStarted = reader.ReadBoolean();
            if ( myStarted )
            {
                myStartTime = reader.ReadUInt64();
                myStartPos = new Vector2d( reader.ReadDouble(), reader.ReadDouble() );
            }
            Velocity = new Vector2d( reader.ReadDouble(), reader.ReadDouble() );
            Range = reader.ReadDouble();
            myDistanceTravelled = reader.ReadDouble();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            SendToClients = true;
        }

        public void Start()
        {
            Start( Map.TimeTicks, new Vector2d( OriginX, OriginY ) );
        }

        public void Start( UInt64 startTime, Vector2d startPos )
        {
            myStartPos = startPos;
            myStartTime = startTime;

            myStarted = true;

            if ( IsServer )
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                stream.Write( BitConverter.GetBytes( myStartTime ), 0, sizeof( UInt64 ) );
                stream.Write( BitConverter.GetBytes( myStartPos.X ), 0, sizeof( Double ) );
                stream.Write( BitConverter.GetBytes( myStartPos.Y ), 0, sizeof( Double ) );
                SendStateUpdate( "ProjStart", stream );
                stream.Close();
            }
        }

        public void Stop()
        {
            Stop( new Vector2d( OriginX, OriginY ) );
        }

        public void Stop( Vector2d stopPos )
        {
            myStarted = false;

            OriginX = stopPos.X;
            OriginY = stopPos.Y;

            if ( IsServer )
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                stream.Write( BitConverter.GetBytes( myStartPos.X ), 0, sizeof( Double ) );
                stream.Write( BitConverter.GetBytes( myStartPos.Y ), 0, sizeof( Double ) );
                SendStateUpdate( "ProjStop", stream );
                stream.Close();
            }
        }

        protected override void OnEnterMap( Map map )
        {
            base.OnEnterMap( map );

            if ( Owner == null )
                Owner = map.GetEntity( myOwnerID );
        }

        public override bool ShouldCollide( Entity ent )
        {
            if ( ent == Owner || ent is Projectile )
                return false;

            return myStarted && base.ShouldCollide( ent );
        }

        public override void Think( double deltaTime )
        {
            base.Think( deltaTime );

            if( !myStarted )
                return;

            ulong time = Map.TimeTicks;

            Vector2d inc = time > myStartTime ? Velocity * Tools.TicksToSeconds( Map.TimeTicks - myStartTime ) : new Vector2d();

            Vector2d dest = myStartPos + inc;
            Vector2d add = dest - new Vector2d( OriginX, OriginY );

            myDistanceTravelled = add.Length;

            if ( Range != 0 && myDistanceTravelled > Range )
            {
                add.Normalize();
                add *= myDistanceTravelled - Range;
            }

            MoveHorizontal( add.X );
            MoveVertical( add.Y );

            if ( Range != 0 && myDistanceTravelled >= Range )
            {
                OnReachRange();
                
                if( IsServer )
                    Remove();
            }
        }

        protected virtual void OnReachRange()
        {
            SendPositionUpdate();
        }

        protected override void OnCollideWithWall()
        {
            base.OnCollideWithWall();

            if( IsServer )
                SendPositionUpdate();
        }

        protected override void OnCollideWithEntity( Entity ent )
        {
            base.OnCollideWithEntity( ent );

            if ( IsServer )
                SendPositionUpdate();
        }

        protected override void OnRegisterNetworkedUpdateHandlers()
        {
            base.OnRegisterNetworkedUpdateHandlers();

            RegisterNetworkedUpdateHandler( "ProjStart", delegate( byte[] payload )
            {
                Start(
                    BitConverter.ToUInt64( payload, 0 ),
                    new Vector2d(
                        BitConverter.ToDouble( payload, sizeof( UInt64 ) ),
                        BitConverter.ToDouble( payload, sizeof( UInt64 ) + sizeof( Double ) )
                    )
                );
            } );

            RegisterNetworkedUpdateHandler( "ProjStop", delegate( byte[] payload )
            {
                Stop(
                    new Vector2d(
                        BitConverter.ToDouble( payload, 0 ),
                        BitConverter.ToDouble( payload, sizeof( Double ) )
                    )
                );
            } );
        }

        protected override void OnSave( System.IO.BinaryWriter writer, bool sendToClient )
        {
            base.OnSave( writer, sendToClient );

            writer.Write( Owner.EntityID );

            writer.Write( myStarted );

            if( myStarted )
            {
                writer.Write( myStartTime );
                writer.Write( myStartPos.X );
                writer.Write( myStartPos.Y );
            }

            writer.Write( Velocity.X );
            writer.Write( Velocity.Y );

            writer.Write( Range );
            writer.Write( myDistanceTravelled );
        }
    }
}
