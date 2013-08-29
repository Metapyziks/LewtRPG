using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Lewt.Shared.Entities;

namespace Lewt.Shared.Magic
{
    public class MagicalEffect
    {
        public static MagicalEffect Create( Entity caster, SpellInfo spellInfo, double strength )
        {
            return Create( caster, null, spellInfo, strength );
        }

        public static MagicalEffect Create( Entity caster, Entity applicator, SpellInfo spellInfo, double strength )
        {
            MagicalEffect effect = null;
            Exception innerException = new Exception();

            try
            {
                Type t = Assembly.GetAssembly( typeof( Entity ) ).GetType( spellInfo.EffectClassName )
                    ?? Scripts.GetType( spellInfo.EffectClassName );
                ConstructorInfo c = t.GetConstructor( new Type[] { typeof( Entity ), typeof( Entity ), typeof( SpellInfo ), typeof( double ) } );
                effect = c.Invoke( new object[] { caster, applicator, spellInfo, strength } ) as MagicalEffect;
            }
            catch ( Exception e )
            {
                innerException = e;
            }

            if ( effect == null )
                throw new Exception( "Magical Effect of type '" + spellInfo.EffectClassName + "' could not be created!", innerException );

            return effect;
        }

        public static MagicalEffect Load( System.IO.BinaryReader reader )
        {
            MagicalEffect effect = null;
            String typeName = reader.ReadString();

            Exception innerException = new Exception();

            try
            {
                Type t = Assembly.GetAssembly( typeof( Entity ) ).GetType( typeName )
                    ?? Scripts.GetType( typeName );
                ConstructorInfo c = t.GetConstructor( new Type[] { typeof( System.IO.BinaryReader ) } );
                effect = c.Invoke( new object[] { reader } ) as MagicalEffect;
            }
            catch ( Exception e )
            {
                innerException = e;
            }

            if ( effect == null )
                throw new Exception( "Magical Effect of type '" + typeName + "' could not be created!", innerException );

            return effect;
        }

        private UInt32 myCasterID;
        private UInt32 myApplicatorID;
        private Entity myCaster;
        private Entity myApplicator;

        public Entity Caster
        {
            get
            {
                return myCaster;
            }
        }
        public Entity Applicator
        {
            get
            {
                return myApplicator;
            }
        }

        public readonly SpellInfo SpellInfo;
        public readonly double Strength;
        public readonly double Duration;

        protected MagicalEffect( Entity caster, Entity applicator, SpellInfo spellInfo, double strength )
        {
            myCaster = caster;
            myApplicator = applicator;

            SpellInfo = spellInfo;
            Strength = strength;

            Duration = SpellInfo.GetDuration( Strength );
        }

        protected MagicalEffect( System.IO.BinaryReader reader )
        {
            myCasterID = reader.ReadUInt32();
            myApplicatorID = reader.ReadUInt32();

            SpellInfo = SpellInfo.Get( reader.ReadUInt16() );
            Strength = reader.ReadDouble();

            Duration = SpellInfo.GetDuration( Strength );
        }

        public void Start( Entity target )
        {
            if ( Caster == null )
                FindCaster( target.Map );

            target.AddMagicalEffect( this );

            OnStartEffect( target );

            if ( Duration == 0 )
                OnEndEffect( target, 0.0 );
        }

        public void Think( Entity target, double secondsElapsed )
        {
            if ( Caster == null )
                FindCaster( target.Map );

            if ( secondsElapsed >= Duration )
            {
                OnEndEffect( target, secondsElapsed );
                target.RemoveMagicalEffect( this );
                return;
            }

            OnApplyEffect( target, secondsElapsed );
        }

        private void FindCaster( Lewt.Shared.World.Map map )
        {
            myCaster = map.GetEntity( myCasterID );
            if ( myApplicatorID != 0xFFFFFFFF )
                myApplicator = map.GetEntity( myApplicatorID );
        }

        protected virtual void OnStartEffect( Entity target )
        {

        }

        protected virtual void OnApplyEffect( Entity target, double secondsElapsed )
        {

        }

        protected virtual void OnEndEffect( Entity target, double secondsElapsed )
        {

        }

        public void Save( System.IO.BinaryWriter writer )
        {
            writer.Write( GetType().FullName );

            writer.Write( Caster.EntityID );
            writer.Write( Applicator != null ? Applicator.EntityID : 0xFFFFFFFF );
            writer.Write( SpellInfo.ID );
            writer.Write( Strength );

            OnSave( writer );
        }

        protected virtual void OnSave( System.IO.BinaryWriter writer )
        {

        }
    }
}
