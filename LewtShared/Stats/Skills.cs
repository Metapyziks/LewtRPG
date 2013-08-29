using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ResourceLib;

namespace Lewt.Shared.Stats
{
    public struct CharSkill
    {
        private struct SkillInfo
        {
            public readonly String FullName;
            public readonly String Abbreviation;
            public readonly String Description;

            public readonly Dictionary<CharAttribute, double> AttributeMods;

            public SkillInfo( InfoObject obj )
            {
                FullName = obj[ "name" ].AsString();
                Abbreviation = obj[ "abbreviation" ].AsString();
                Description = obj[ "description" ].AsString();

                AttributeMods = new Dictionary<CharAttribute, double>();

                InfoObject modObj = obj[ "attribute mods" ] as InfoObject;

                foreach ( String str in modObj.Keys )
                    AttributeMods.Add( CharAttribute.Get( str ), modObj[ str ].AsDouble() / 100.0 );
            }
        }

        private static UInt16 stNextSkillID = 0;

        private static Dictionary<String, CharSkill> stSkills;
        private static Dictionary<String, SkillInfo> stSkillInfos;

        private static void LoadCharSkills()
        {
            InfoObject[] skillInfos = Info.GetAll( "skill" );
            stSkills = new Dictionary<string, CharSkill>();
            stSkillInfos = new Dictionary<string, SkillInfo>();

            for ( int i = 0; i < skillInfos.Length; ++i )
                stSkills.Add( skillInfos[ i ].Name, new CharSkill( skillInfos[ i ] ) );
        }

        public static CharSkill[] GetAll()
        {
            if ( stSkills == null )
                LoadCharSkills();

            return stSkills.Values.ToArray();
        }

        public static CharSkill Get( String nameOrAbbreviation )
        {
            if ( stSkills == null )
                LoadCharSkills();

            if ( nameOrAbbreviation.Length > 3 )
                return stSkills[ nameOrAbbreviation ];
            else
            {
                foreach ( CharSkill skill in stSkills.Values )
                    if ( skill.Abbreviation == nameOrAbbreviation )
                        return skill;

                throw new KeyNotFoundException();
            }
        }

        public static CharSkill GetByID( UInt16 id )
        {
            if ( stSkills == null )
                LoadCharSkills();

            foreach ( CharSkill skill in stSkills.Values )
                if ( skill.ID == id )
                    return skill;

            throw new KeyNotFoundException();
        }

        public readonly String Name;
        public readonly UInt16 ID;

        public String FullName
        {
            get
            {
                return stSkillInfos[ Name ].FullName;
            }
        }
        public String Abbreviation
        {
            get
            {
                return stSkillInfos[ Name ].Abbreviation;
            }
        }
        public String Description
        {
            get
            {
                return stSkillInfos[ Name ].Description;
            }
        }
        public KeyValuePair<CharAttribute, double>[] AttributeMods
        {
            get
            {
                return stSkillInfos[ Name ].AttributeMods.ToArray();
            }
        }

        private CharSkill( InfoObject obj )
        {
            Name = obj.Name;
            ID = stNextSkillID++;

            stSkillInfos.Add( Name, new SkillInfo( obj ) );
        }

        public override string ToString()
        {
            return FullName + " (" + Abbreviation.ToUpper() + ")";
        }
    }
}
