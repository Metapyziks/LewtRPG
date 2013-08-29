using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ResourceLib;

namespace Lewt.Shared.Stats
{
    public struct CharAttribute
    {
        private struct AttributeInfo
        {
            public readonly String FullName;
            public readonly String Abbreviation;
            public readonly String Description;

            public AttributeInfo( String fullName, String abbreviation, String description )
            {
                FullName = fullName;
                Abbreviation = abbreviation;
                Description = description;
            }
        }

        private static UInt16 stNextAttribID = 0;

        private static Dictionary<String, CharAttribute> stAttribs;
        private static Dictionary<String, AttributeInfo> stAttribInfos;

        public static CharAttribute Strength
        {
            get
            {
                return Get( "strength" );
            }
        }
        public static CharAttribute Dexterity
        {
            get
            {
                return Get( "dexterity" );
            }
        }
        public static CharAttribute Intelligence
        {
            get
            {
                return Get( "intelligence" );
            }
        }
        public static CharAttribute Health
        {
            get
            {
                return Get( "health" );
            }
        }

        static CharAttribute()
        {
            stAttribs = new Dictionary<string, CharAttribute>();
            stAttribInfos = new Dictionary<string, AttributeInfo>();

            stAttribs.Add( "strength", new CharAttribute( "strength", "Strength", "st", "The physical power and bulk of a character" ) );
            stAttribs.Add( "dexterity", new CharAttribute( "dexterity", "Dexterity", "dx", "The physical agility, coordination and manual dexterity of a character" ) );
            stAttribs.Add( "intelligence", new CharAttribute( "intelligence", "Intelligence", "iq", "The mental capacity, acuity and awareness of a character" ) );
            stAttribs.Add( "health", new CharAttribute( "health", "Health", "ht", "The physical stamina, energy and vitality of a character" ) );
        }

        public static CharAttribute[] GetAll()
        {
            return stAttribs.Values.ToArray();
        }

        public static CharAttribute Get( String nameOrAbbreviation )
        {
            if ( nameOrAbbreviation.Length > 2 )
                return stAttribs[ nameOrAbbreviation ];
            else
            {
                foreach ( CharAttribute attrib in stAttribs.Values )
                    if ( attrib.Abbreviation == nameOrAbbreviation )
                        return attrib;

                throw new KeyNotFoundException();
            }
        }

        public static CharAttribute GetByID( UInt16 id )
        {
            foreach ( CharAttribute attrib in stAttribs.Values )
                if ( attrib.ID == id )
                    return attrib;

            throw new KeyNotFoundException();
        }

        public readonly String Name;
        public readonly UInt16 ID;

        public String FullName
        {
            get
            {
                return stAttribInfos[ Name ].FullName;
            }
        }
        public String Abbreviation
        {
            get
            {
                return stAttribInfos[ Name ].Abbreviation;
            }
        }
        public String Description
        {
            get
            {
                return stAttribInfos[ Name ].Description;
            }
        }

        private CharAttribute( String name, String fullName, String abbreviation, String description )
        {
            Name = name;
            ID = stNextAttribID++;

            stAttribInfos.Add( Name, new AttributeInfo( fullName, abbreviation, description ) );
        }

        public override string ToString()
        {
            return FullName + " (" + Abbreviation.ToUpper() + ")";
        }
    }
}
