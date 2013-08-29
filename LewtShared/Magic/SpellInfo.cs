using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using ResourceLib;
using Lewt.Shared.Entities;

namespace Lewt.Shared.Magic
{
    public enum CastType
    {
        Self,
        Touch,
        Projectile
    }

    public struct SpellInfo
    {
        private struct SpellQuality
        {
            public readonly double Base;
            public readonly double Final;
            public readonly double Weighting;

            public SpellQuality( double baseVal, double finalVal, double weighting = 1.0 )
            {
                Base = baseVal;
                Final = finalVal;
                Weighting = weighting;
            }

            public double GetValue( double strength )
            {
                if ( Weighting != 1.0 )
                    strength = Math.Pow( strength, Weighting );

                return ( Final - Base ) * strength + Base;
            }
        }

        private static UInt16 stNextSpellID = 0;

        private static Dictionary<UInt16, SpellInfo> mySpellInfos;
        private static Dictionary<String, SpellInfo> mySpellStrings;

        private static void LoadSpellInfos()
        {
            InfoObject[] spellInfos = ResourceLib.Info.GetAll( "spell" );

            mySpellInfos = new Dictionary<ushort, SpellInfo>();
            mySpellStrings = new Dictionary<string, SpellInfo>();

            foreach( InfoObject info in spellInfos )
            {
                SpellInfo spell = new SpellInfo( stNextSpellID++, info );
                mySpellInfos.Add( spell.ID, spell );
                mySpellStrings.Add( spell.Name, spell );
            }
        }

        public static SpellInfo[] GetAll()
        {
            if ( mySpellInfos == null )
                LoadSpellInfos();

            return mySpellInfos.Values.ToArray();
        }

        public static SpellInfo Get( UInt16 id )
        {
            if ( mySpellInfos == null )
                LoadSpellInfos();

            return mySpellInfos[ id ];
        }

        public static SpellInfo Get( String name )
        {
            if ( mySpellInfos == null )
                LoadSpellInfos();

            return mySpellStrings[ name ];
        }

        private Dictionary<double, String> myNames;
        private Dictionary<String, SpellQuality> myQualities;
        private String myDescription;

        public readonly UInt16 ID;
        public readonly string Name;
        public readonly CastType CastType;
        public readonly DamageType DamageType;
        public readonly Color4 Colour;
        public readonly String EffectClassName;

        public SpellInfo( UInt16 id, InfoObject info )
        {
            ID = id;
            Name = info.Name;

            CastType = (CastType) Enum.Parse( typeof( CastType ), info[ "cast type" ].AsString() );
            DamageType = DamageType.Magical;

            if ( CastType == CastType.Projectile )
                DamageType |= DamageType.Ranged;
            else if ( CastType == CastType.Touch )
                DamageType |= DamageType.Melee;

            if ( info.ContainsKey( "elemental types" ) )
                foreach( InfoValue val in info[ "elemental types" ].AsArray() )
                    DamageType |= (DamageType) Enum.Parse( typeof( DamageType ), val.AsString() );

            myNames = new Dictionary<double,string>();

            foreach ( InfoValue val in info[ "names" ].AsArray() )
            {
                InfoValue[] arr = val.AsArray();
                myNames.Add( arr[ 0 ].AsDouble(), arr[ 1 ].AsString() );
            }

            if ( info.ContainsKey( "colour" ) )
            {
                InfoValue[] arr = info[ "colour" ].AsArray();
                Colour = new Color4( (byte) arr[ 0 ].AsInteger(), (byte) arr[ 1 ].AsInteger(), (byte) arr[ 2 ].AsInteger(), 255 );
            }
            else
                Colour = Color4.White;
            
            InfoObject quals = info[ "qualities" ] as InfoObject;

            myQualities = new Dictionary<string, SpellQuality>();
            foreach ( String key in quals.Keys )
            {
                InfoValue[] vals = quals[ key ].AsArray();
                myQualities.Add( key, new SpellQuality( vals[ 0 ].AsDouble(),
                    ( vals.Length == 1 ) ? vals[ 0 ].AsDouble() : vals[ 1 ].AsDouble(),
                    ( vals.Length == 3 ) ? vals[ 2 ].AsDouble() : 1.0 ) );
            }

            myDescription = info[ "description" ].AsString();

            EffectClassName = info[ "effect class" ].AsString();
        }

        public String GetName( double strength )
        {
            String lastName = myNames[ 0.0 ];

            foreach ( KeyValuePair<double, string> keyVal in myNames )
            {
                if ( keyVal.Key > strength )
                    break;
                else
                    lastName = keyVal.Value;
            }

            return lastName;
        }

        public String GetDescription( double strength )
        {
            String desc = myDescription;

            foreach ( String key in myQualities.Keys )
            {
                desc = desc.Replace( "[" + key + "]", GetInteger( key, strength ).ToString() );
                desc = desc.Replace( "{" + key + "}", GetDouble( key, strength ).ToString( "F" ) );
            }

            return desc;
        }

        public int GetManaCost( double strength )
        {
            return GetInteger( "mana cost", strength );
        }

        public double GetDuration( double strength )
        {
            if ( myQualities.ContainsKey( "duration" ) )
                return GetDouble( "duration", strength );

            return 0.0;
        }

        public int GetValue( double strength )
        {
            return GetInteger( "value", strength );
        }

        public int GetInteger( String key, double strength )
        {
            return (int) Math.Round( GetDouble( key, strength ) );
        }

        public double GetDouble( String key, double strength )
        {
            return myQualities[ key ].GetValue( strength );
        }
    }
}
