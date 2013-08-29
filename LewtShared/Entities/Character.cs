using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

using Lewt.Shared.Rendering;
using OpenTK.Graphics;
using Lewt.Shared.Magic;
using Lewt.Shared.Stats;
using Lewt.Shared.Items;
using System.IO;

namespace Lewt.Shared.Entities
{
    public enum WalkDirection
    {
        Still = 0,
        Left = 1,
        Up = 2,
        Right = 3,
        Down = 4
    }

    #region Event Handlers
    public class DamagedEventArgs : EventArgs
    {
        public readonly Entity Attacker;
        public readonly Entity Weapon;
        public readonly DamageType DamageType;
        public readonly int Damage;

        public DamagedEventArgs( Entity attacker, Entity weapon, DamageType damageType, int damage )
        {
            Attacker = attacker;
            Weapon = weapon;
            DamageType = damageType;
            Damage = damage;
        }
    }

    public delegate void DamagedEventHandler( object sender, DamagedEventArgs e );

    public class KilledEventArgs : EventArgs
    {
        public readonly Entity Attacker;
        public readonly Entity Weapon;
        public readonly DamageType DamageType;

        public KilledEventArgs( Entity attacker, Entity weapon, DamageType damageType )
        {
            Attacker = attacker;
            Weapon = weapon;
            DamageType = damageType;
        }
    }

    public delegate void KilledEventHandler( object sender, KilledEventArgs e );
    #endregion Event Handlers

    public class Character : Entity, IDamageable, IContainer
    {
        public const double CastAnimationDuration = 0.25;

        private Spell myEquippedSpell;
        private SpellItem myEquippedSpellItem;
        private bool myOrbEquipped;

        private WalkDirection myFacingDirection;

        private Vector2d myWalkStartPos;
        private ulong myWalkStartTime;

        private WalkDirection myNextWalkDirection;
        private WalkDirection myCurrentWalkDirection;

        private ulong myLastWalkStateSent;

        private AnimatedSprite myAnim;

        protected AnimatedSprite Anim
        {
            get
            {
                if ( myAnim == null )
                    InitializeGraphics();

                return myAnim;
            }
            set
            {
                myAnim = value;
            }
        }

        public Spell EquippedSpell
        {
            get
            {
                return myEquippedSpell;
            }
        }

        public SpellItem EquippedSpellItem
        {
            get
            {
                return myEquippedSpellItem;
            }
            set
            {
                myEquippedSpellItem = value;
                myEquippedSpell = ( value != null ? value.Spell : null );
                myOrbEquipped = value is SpellOrb;

                if ( Map != null && IsServer )
                {
                    if ( myEquippedSpell != null )
                    {
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter( stream );
                        writer.Write( myEquippedSpell.Info.ID );
                        writer.Write( myEquippedSpell.Strength );
                        writer.Write( myOrbEquipped );
                        SendStateUpdate( "EquipSpell", stream );
                        writer.Close();
                    }
                    else
                        SendStateUpdate( "UnEquipSpell" );
                }
            }
        }

        public bool OrbEquipped
        {
            get
            {
                return myOrbEquipped;
            }
        }

        public Inventory Inventory
        {
            get;
            set;
        }

        private ulong myLastCastTime;

        private int myHitPoints;
        private int myManaLevel;

        private ulong myLastManaRechargeTime;
        private ulong myLastHurtTime;
        private ulong myLastHPRechargeTime;

        private Dictionary<CharAttribute, int> myBaseAttributes;
        private Dictionary<CharSkill, int> myBaseSkills;

        public int HitPoints
        {
            get
            {
                if ( myHitPoints > MaxHitPoints )
                    myHitPoints = MaxHitPoints;

                return myHitPoints;
            }
            set
            {
                myHitPoints = Math.Max( Math.Min( MaxHitPoints, value ), 0 );

                if ( Map != null && IsServer )
                    SendStateUpdate( "SetHitPoints", BitConverter.GetBytes( myHitPoints ) );
            }
        }

        public bool IsAlive
        {
            get
            {
                return HitPoints > 0;
            }
        }

        public int ManaLevel
        {
            get
            {
                if ( myManaLevel > MaxManaLevel )
                    myManaLevel = MaxManaLevel;

                return myManaLevel;
            }
            set
            {
                myManaLevel = Math.Max( Math.Min( MaxManaLevel, value ), 0 );

                if ( Map != null && IsServer )
                    SendStateUpdate( "SetManaLevel", BitConverter.GetBytes( myManaLevel ) );
            }
        }

        public bool HasSpell {
            get { return EquippedSpell != null; }
        }

        public bool CanCast
        {
            get
            {
                return IsAlive &&
                    HasSpell && 
                    Map != null && 
                    Map.TimeTicks - myLastCastTime > Tools.SecondsToTicks( CastCooldownTime );
            }
        }

        public double CastAngle
        {
            get
            {
                return ( Math.PI / 2.0 ) * ( (int) FacingDirection + 1 );
            }
        }

        public WalkDirection WalkDirection
        {
            get
            {
                return myCurrentWalkDirection;
            }
        }

        public WalkDirection FacingDirection
        {
            get
            {
                return myFacingDirection;
            }
        }

        public event DamagedEventHandler Damaged;
        public event KilledEventHandler Killed;

        protected int BaseMaxHitPoints;
        protected int FinalMaxHitPoints;

        public int MaxHitPoints
        {
            get
            {
                return BaseMaxHitPoints +
                    (int)( GetSkillLevel( CharSkill.Get( "hitpoints" ) ) / 100.0 * ( FinalMaxHitPoints - BaseMaxHitPoints ) );
            }
        }

        protected int BaseMaxManaLevel;
        protected int FinalMaxManaLevel;

        public int MaxManaLevel
        {
            get
            {
                return BaseMaxManaLevel +
                    (int) ( GetSkillLevel( CharSkill.Get( "manalevel" ) ) / 100.0 * ( FinalMaxManaLevel - BaseMaxManaLevel ) );
            }
        }

        protected double BaseWalkSpeed;
        protected double FinalWalkSpeed;

        public double WalkSpeed
        {
            get
            {
                return BaseWalkSpeed +
                    GetSkillLevel( CharSkill.Get( "athletics" ) ) / 100.0 * ( FinalWalkSpeed - BaseWalkSpeed );
            }
        }

        protected double BaseManaRechargePeriod;
        protected double FinalManaRechargePeriod;

        public double ManaRechargePeriod
        {
            get
            {
                double baseLog = Math.Log( BaseManaRechargePeriod, 2.0 );
                double fastLog = Math.Log( FinalManaRechargePeriod, 2.0 );

                return Math.Pow( 2.0, GetSkillLevel( CharSkill.Get( "meditation" ) ) / 100.0  * ( fastLog - baseLog ) + baseLog );
            }
        }

        protected double BaseCastCooldownTime;
        protected double FinalCastCooldownTime;

        public double CastCooldownTime
        {
            get
            {
                double baseLog = Math.Log( BaseCastCooldownTime, 2.0 );
                double fastLog = Math.Log( FinalCastCooldownTime, 2.0 );

                return Math.Pow( 2.0, GetSkillLevel( CharSkill.Get( "reflexes" ) ) / 100.0 * ( fastLog - baseLog ) + baseLog );
            }
        }

        public bool RechargeHitPoints;

        public double HPRechargeDelay;
        public double SlowHPRechargePeriod;
        public double FastHPRechargePeriod;

        public double BaseFastHPRechargeDelay;
        public double FinalFastHPRechargeDelay;

        public double FastHPRechargeDelay
        {
            get
            {
                double baseLog = Math.Log( BaseFastHPRechargeDelay, 2.0 );
                double fastLog = Math.Log( FinalFastHPRechargeDelay, 2.0 );

                return Math.Pow( 2.0, GetSkillLevel( CharSkill.Get( "healing" ) ) / 100.0 * ( fastLog - baseLog ) + baseLog );
            }
        }

        public double HPRechargePeriod
        {
            get
            {
                double prog = Math.Min( Tools.TicksToSeconds( Map.TimeTicks - myLastHurtTime ) - HPRechargeDelay, FastHPRechargeDelay ) / FastHPRechargeDelay;

                double baseLog = Math.Log( SlowHPRechargePeriod, 2.0 );
                double fastLog = Math.Log( FastHPRechargePeriod, 2.0 );

                return Math.Pow( 2.0, prog * ( fastLog - baseLog ) + baseLog );
            }
        }

        public Character()
        {
            CollideWithEntities = true;
            myCurrentWalkDirection = myFacingDirection = WalkDirection.Down;

            myHitPoints = MaxHitPoints;
            myManaLevel = MaxManaLevel;

            Inventory = new Inventory( this, 0 );
        }

        public Character( System.IO.BinaryReader reader, bool sentFromServer )
            : base( reader, sentFromServer )
        {
            myHitPoints = reader.ReadInt16();
            myManaLevel = reader.ReadInt16();

            ushort attribCount = reader.ReadUInt16();
            for ( int i = 0; i < attribCount; ++i )
                myBaseAttributes.Add( CharAttribute.GetByID( reader.ReadUInt16() ), reader.ReadByte() );

            ushort skillCount = reader.ReadUInt16();
            for ( int i = 0; i < skillCount; ++i )
                myBaseSkills.Add( CharSkill.GetByID( reader.ReadUInt16() ), reader.ReadByte() );

            myCurrentWalkDirection = myFacingDirection = (WalkDirection) reader.ReadByte();

            if( !sentFromServer )
                Inventory = new Inventory( this, reader );
        }

        protected override void OnInitialize()
        {
 	        base.OnInitialize();

            SendToClients = true;

            BaseMaxHitPoints = 10;
            FinalMaxHitPoints = 10;

            BaseMaxManaLevel = 0;
            FinalMaxManaLevel = 0;

            BaseWalkSpeed = 2.0;
            FinalWalkSpeed = 2.0;

            BaseManaRechargePeriod = 1.0;
            FinalManaRechargePeriod = 1.0 / 16.0;

            BaseCastCooldownTime = 1.0;
            FinalCastCooldownTime = 1.0 / 8.0;

            RechargeHitPoints = false;

            myLastCastTime = 0;
            myLastManaRechargeTime = 0;

            myBaseAttributes = new Dictionary<CharAttribute, int>();
            myBaseSkills = new Dictionary<CharSkill, int>();
        }

        protected override void OnEnterMap( World.Map map )
        {
            base.OnEnterMap( map );

            if ( !IsAlive && IsClient )
            {
                Anim.StartFrame = 24;
                Anim.FrameCount = 1;
                Anim.Stop();
                Anim.Reset();
            }
        }

        public override bool ShouldCollide( Entity ent )
        {
            return IsAlive && base.ShouldCollide( ent );
        }

        public int GetAttributeLevel( CharAttribute attribute, bool includeMagicalEffects = true )
        {
            return GetBaseAttributeLevel( attribute );
        }

        public int GetBaseAttributeLevel( CharAttribute attribute )
        {
            return ( myBaseAttributes.ContainsKey( attribute ) ? myBaseAttributes[ attribute ] : 0 );
        }

        public void SetBaseAttributeLevel( CharAttribute attribute, int amount )
        {
            amount = Tools.Clamp( amount, 0, 100 );

            if ( !myBaseAttributes.ContainsKey( attribute ) )
                myBaseAttributes.Add( attribute, amount );
            else
                myBaseAttributes[ attribute ] = amount;

            if ( Map != null && IsServer )
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                stream.Write( BitConverter.GetBytes( attribute.ID ), 0, 2 );
                stream.WriteByte( (byte) amount );
                SendStateUpdate( "SetBaseAttribute", stream );
                stream.Close();
            }
        }

        public int GetSkillLevel( CharSkill skill, bool includeMagicalEffects = true )
        {
            int val = GetBaseSkillLevel( skill );

            foreach ( KeyValuePair<CharAttribute, double> keyVal in skill.AttributeMods )
                val += (int) Math.Round( GetAttributeLevel( keyVal.Key, includeMagicalEffects ) * keyVal.Value );

            return val;
        }

        public int GetBaseSkillLevel( CharSkill skill )
        {
            return ( myBaseSkills.ContainsKey( skill ) ? myBaseSkills[ skill ] : 0 );
        }

        public void SetBaseSkillLevel( CharSkill skill, int amount )
        {
            amount = Tools.Clamp( amount, 0, 50 );

            if ( !myBaseSkills.ContainsKey( skill ) )
                myBaseSkills.Add( skill, amount );
            else
                myBaseSkills[ skill ] = amount;

            if ( Map != null && IsServer )
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                stream.Write( BitConverter.GetBytes( skill.ID ), 0, 2 );
                stream.WriteByte( (byte) amount );
                SendStateUpdate( "SetBaseSkill", stream );
                stream.Close();
            }
        }

        public virtual void Resurrect() {
            if ( IsServer && !IsAlive ) {
                HitPoints = MaxHitPoints;

                SendStateUpdate( "Resurrect" );
            }

            // Character hit points should be sent already so client thinks we are alive
            if ( IsClient ) {
                Anim.StartFrame = 0;
                Anim.FrameCount = 1;
                Anim.Stop();
                Anim.Reset();
            }
        }

        public void Cast( Spell spell )
        {
            Cast( spell, ( Math.PI / 2.0 ) * ( (int) FacingDirection + 1 ) );
        }

        public void Cast( Spell spell, double angle )
        {
            if ( IsServer && CanCast && EquippedSpellItem.CanCast( this ) )
            {
                SendStateUpdate( "SpellCast" );

                myLastCastTime = myLastManaRechargeTime = Map.TimeTicks;

                StopWalking();

                EquippedSpellItem.Cast( this, new Vector2d( OriginX, OriginY - 0.5 ), angle );
            }
        }

        public void Cast()
        {
            if ( IsClient && CanCast && ( !OrbEquipped || EquippedSpell.ManaCost <= ManaLevel ) )
            {
                myLastCastTime = Map.TimeTicks;

                StopWalking();

                switch ( FacingDirection )
                {
                    case WalkDirection.Left:
                        Anim.StartFrame = 14;
                        Anim.FlipHorizontal = true;
                        break;
                    case WalkDirection.Up:
                        Anim.StartFrame = 22;
                        Anim.FlipHorizontal = false;
                        break;
                    case WalkDirection.Right:
                        Anim.StartFrame = 14;
                        Anim.FlipHorizontal = false;
                        break;
                    case WalkDirection.Down:
                        Anim.StartFrame = 6;
                        Anim.FlipHorizontal = false;
                        break;
                }

                Anim.FrameCount = 1;
                Anim.Stop();
                Anim.Reset();
            }
        }

        public virtual void Hurt( Entity attacker, Entity weapon, DamageType damageType, int damage )
        {
            if ( IsAlive && IsServer )
            {
                HitPoints -= damage;

                if( Damaged != null )
                    Damaged( this, new DamagedEventArgs( attacker, weapon, damageType, damage ) );

                if ( !IsAlive )
                {
                    OnDie( attacker, weapon, damageType );

                    if ( Killed != null )
                        Killed( this, new KilledEventArgs( attacker, weapon, damageType ) );
                    
                    System.IO.MemoryStream stream = new System.IO.MemoryStream();
                    stream.Write( BitConverter.GetBytes( attacker != null ? attacker.EntityID : 0xFFFFFFFF ), 0, 4 );
                    stream.Write( BitConverter.GetBytes( weapon != null ? weapon.EntityID : 0xFFFFFFFF ), 0, 4 );
                    stream.Write( BitConverter.GetBytes( (ushort) damageType ), 0, 2 );
                    SendStateUpdate( "Die", stream );
                    stream.Close();
                }
            }

            myLastHurtTime = Map.TimeTicks;
        }

        protected override void InitializeGraphics()
        {
            WalkStep( WalkDirection.Still );

            base.InitializeGraphics();
        }

        public void StartWalking( WalkDirection dir )
        {
            StartWalking( dir, Map.TimeTicks, new Vector2d( OriginX, OriginY ) );
        }

        public virtual void StartWalking( WalkDirection dir, ulong startTime, Vector2d startPos )
        {
            if ( !IsAlive || Tools.TicksToSeconds( Map.TimeTicks - myLastCastTime ) < CastAnimationDuration )
                return;

            myNextWalkDirection = dir;

            myWalkStartTime = startTime;
            myWalkStartPos = startPos;

            if ( dir == WalkDirection.Up || dir == WalkDirection.Down )
                OriginX = startPos.X;
            else
                OriginY = startPos.Y;

            if ( IsServer )
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                stream.WriteByte( (byte) myNextWalkDirection );
                stream.Write( BitConverter.GetBytes( myWalkStartTime ), 0, sizeof( UInt64 ) );
                stream.Write( BitConverter.GetBytes( myWalkStartPos.X ), 0, sizeof( Double ) );
                stream.Write( BitConverter.GetBytes( myWalkStartPos.Y ), 0, sizeof( Double ) );
                SendStateUpdate( "StartWalking", stream );
                stream.Close();

                myLastWalkStateSent = Map.TimeTicks;
            }
        }

        public void StopWalking()
        {
            StopWalking( new Vector2d( OriginX, OriginY ) );
        }

        public void StopWalking( Vector2d stopPos )
        {
            if ( !IsAlive )
                return;

            myNextWalkDirection = WalkDirection.Still;

            OriginX = stopPos.X;
            OriginY = stopPos.Y;

            if ( IsServer )
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                stream.Write( BitConverter.GetBytes( OriginX ), 0, sizeof( Double ) );
                stream.Write( BitConverter.GetBytes( OriginY ), 0, sizeof( Double ) );
                SendStateUpdate( "StopWalking", stream );
                stream.Close();

                myLastWalkStateSent = Map.TimeTicks;
            }
        }

        protected bool WalkStep( WalkDirection dir )
        {
            if ( !IsAlive )
                return false;

            bool hitWall = false;

            if ( dir == WalkDirection.Still )
            {
                if ( Tools.TicksToSeconds( Map.TimeTicks - myLastCastTime ) >= CastAnimationDuration )
                {
                    switch ( ( Anim.StartFrame % 8 <= 4 ) ? WalkDirection : FacingDirection )
                    {
                        case WalkDirection.Left:
                            Anim.StartFrame = 8;
                            Anim.FlipHorizontal = true;
                            break;
                        case WalkDirection.Up:
                            Anim.StartFrame = 16;
                            Anim.FlipHorizontal = false;
                            break;
                        case WalkDirection.Right:
                            Anim.StartFrame = 8;
                            Anim.FlipHorizontal = false;
                            break;
                        case WalkDirection.Down:
                            Anim.StartFrame = 0;
                            Anim.FlipHorizontal = false;
                            break;
                    }

                    Anim.FrameCount = 1;
                    Anim.Stop();
                    Anim.Reset();
                }
            }
            else
            {
                myFacingDirection = dir;

                double dest;
                ulong time = Map.TimeTicks;
                ulong start = myWalkStartTime;
                double inc = ( time > start ) ? WalkSpeed * Tools.TicksToSeconds( time - start ) : 0;

                int anim_startframe = 0;
                bool anim_flip = false;

                switch ( dir )
                {
                    case WalkDirection.Left:
                        dest = myWalkStartPos.X - inc;
                        hitWall = MoveHorizontal( dest - OriginX );
                        anim_startframe = 9;
                        anim_flip = true;
                        break;
                    case WalkDirection.Up:
                        dest = myWalkStartPos.Y - inc;
                        hitWall = MoveVertical( dest - OriginY );
                        anim_startframe = 17;
                        anim_flip = false;
                        break;
                    case WalkDirection.Right:
                        dest = myWalkStartPos.X + inc;
                        hitWall = MoveHorizontal( dest - OriginX );
                        anim_startframe = 9;
                        anim_flip = false;
                        break;
                    case WalkDirection.Down:
                        dest = myWalkStartPos.Y + inc;
                        hitWall = MoveVertical( dest - OriginY );
                        anim_startframe = 1;
                        anim_flip = false;
                        break;
                }


                if ( !hitWall && inc > 0 ) {
                    Anim.StartFrame = anim_startframe;
                    Anim.FlipHorizontal = anim_flip;
                    Anim.FrameCount = 4;
                    Anim.Start();
                }
            }

            myCurrentWalkDirection = dir;

            return hitWall;
        }

        protected virtual void OnDie( Entity attacker, Entity weapon, DamageType damageType )
        {
            if ( IsClient )
            {
                Anim.StartFrame = 24;
                Anim.FrameCount = 1;
                Anim.Stop();
                Anim.Reset();
            }
        }

        public override void Think( double deltaTime )
        {
            base.Think( deltaTime );

            if ( !IsAlive )
                return;

            if ( IsServer )
            {
                if ( myCurrentWalkDirection != Entities.WalkDirection.Still &&
                    myNextWalkDirection == myCurrentWalkDirection &&
                    Tools.TicksToSeconds( Map.TimeTicks - myLastWalkStateSent ) >= 1.0 )
                    StartWalking( myCurrentWalkDirection );

                if ( ManaLevel < MaxManaLevel &&
                    Tools.TicksToSeconds( Map.TimeTicks - myLastManaRechargeTime ) >= ManaRechargePeriod )
                {
                    myLastManaRechargeTime = Map.TimeTicks;
                    ManaLevel++;
                }

                if ( RechargeHitPoints && myHitPoints < MaxHitPoints &&
                    Tools.TicksToSeconds( Map.TimeTicks - myLastHurtTime ) >= HPRechargeDelay &&
                    Tools.TicksToSeconds( Map.TimeTicks - myLastHPRechargeTime ) >= HPRechargePeriod )
                {
                    myLastHPRechargeTime = Map.TimeTicks;
                    HitPoints++;
                }
            }

            if ( WalkStep( myNextWalkDirection ) )
                StopWalking();
        }

        public override void Render()
        {
            base.Render();

            Anim.X = ScreenX;
            Anim.Y = ScreenY - 12.0f * MapRenderer.CameraScale;
            Anim.Render();
        }

        protected override void OnRegisterNetworkedUpdateHandlers()
        {
            base.OnRegisterNetworkedUpdateHandlers();

            RegisterNetworkedUpdateHandler( "StartWalking", delegate( byte[] payload )
            {
                StartWalking(
                    (WalkDirection) payload[ 0 ],
                    BitConverter.ToUInt64( payload, 1 ),
                    new Vector2d(
                        BitConverter.ToDouble( payload, 1 + sizeof( UInt64 ) ),
                        BitConverter.ToDouble( payload, 1 + sizeof( UInt64 ) + sizeof( Double ) )
                    )
                );
            } );

            RegisterNetworkedUpdateHandler( "StopWalking", delegate( byte[] payload )
            {
                StopWalking(
                    new Vector2d( BitConverter.ToDouble( payload, 0 ),
                        BitConverter.ToDouble( payload, sizeof( Double ) )
                    )
                );
            } );

            RegisterNetworkedUpdateHandler( "SpellCast", delegate( byte[] payload )
            {
                Cast();
            } );

            RegisterNetworkedUpdateHandler( "SetHitPoints", delegate( byte[] payload )
            {
                HitPoints = BitConverter.ToInt32( payload, 0 );
            } );

            RegisterNetworkedUpdateHandler( "Die", delegate( byte[] payload )
            {
                uint attackerID = BitConverter.ToUInt32( payload, 0 );
                uint weaponID = BitConverter.ToUInt32( payload, 4 );
                DamageType damageType = (DamageType) BitConverter.ToUInt16( payload, 6 );

                Entity attacker = null;
                Entity weapon = null;

                if ( attackerID != 0xFFFFFFFF )
                    attacker = Map.GetEntity( attackerID );

                if ( weaponID != 0xFFFFFFFF )
                    weapon = Map.GetEntity( weaponID );

                OnDie( attacker, weapon, damageType );

                if ( Killed != null )
                    Killed( this, new KilledEventArgs( attacker, weapon, damageType ) );
            } );

            RegisterNetworkedUpdateHandler( "Resurrect", delegate( byte[] payload ) {
                Resurrect();
            } );

            RegisterNetworkedUpdateHandler( "SetManaLevel", delegate( byte[] payload )
            {
                ManaLevel = BitConverter.ToInt32( payload, 0 );
            } );

            RegisterNetworkedUpdateHandler( "SetBaseAttribute", delegate( byte[] payload )
            {
                UInt16 id = BitConverter.ToUInt16( payload, 0 );
                byte amount = payload[ 2 ];

                SetBaseAttributeLevel( CharAttribute.GetByID( id ), amount );
            } );

            RegisterNetworkedUpdateHandler( "SetBaseSkill", delegate( byte[] payload )
            {
                UInt16 id = BitConverter.ToUInt16( payload, 0 );
                byte amount = payload[ 2 ];

                SetBaseSkillLevel( CharSkill.GetByID( id ), amount );
            } );

            RegisterNetworkedUpdateHandler( "EquipSpell", delegate( byte[] payload )
            {
                MemoryStream stream = new MemoryStream( payload );
                BinaryReader reader = new BinaryReader( stream );
                myEquippedSpell = Spell.Create( SpellInfo.Get( reader.ReadUInt16() ), reader.ReadDouble() );
                myOrbEquipped = reader.ReadBoolean();
                reader.Close();
            } );

            RegisterNetworkedUpdateHandler( "UnEquipSpell", delegate( byte[] payload )
            {
                myEquippedSpell = null;
                myOrbEquipped = false;
            } );
        }

        protected override void OnSave( System.IO.BinaryWriter writer, bool sendToClient )
        {
            base.OnSave( writer, sendToClient );

            writer.Write( (short) HitPoints );
            writer.Write( (short) ManaLevel );

            writer.Write( (ushort) myBaseAttributes.Count );
            foreach ( KeyValuePair<CharAttribute, int> keyVal in myBaseAttributes )
            {
                writer.Write( keyVal.Key.ID );
                writer.Write( (byte) keyVal.Value );
            }

            writer.Write( (ushort) myBaseSkills.Count );
            foreach ( KeyValuePair<CharSkill, int> keyVal in myBaseSkills )
            {
                writer.Write( keyVal.Key.ID );
                writer.Write( (byte) keyVal.Value );
            }

            writer.Write( (byte) myFacingDirection );

            if( !sendToClient )
                Inventory.Save( writer );
        }
    }
}
