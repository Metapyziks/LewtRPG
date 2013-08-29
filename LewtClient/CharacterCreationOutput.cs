using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Shared.Stats;

namespace Lewt.Client
{
    public class CharacterCreationOutput
    {
        private Dictionary<CharAttribute, int> myBaseAttributes;
        private Dictionary<CharSkill, int> myBaseSkills;

        public String PlayerName;

        public CharacterCreationOutput( int baseAttributePoints, int baseSkillPoints )
        {
            PlayerName = "Player";
            myBaseAttributes = new Dictionary<CharAttribute, int>();
            myBaseSkills = new Dictionary<CharSkill, int>();

            foreach( CharAttribute attrib in CharAttribute.GetAll() )
                myBaseAttributes.Add( attrib, baseAttributePoints );

            foreach ( CharSkill skill in CharSkill.GetAll() )
                myBaseSkills.Add( skill, baseSkillPoints );
        }

        public int GetAttributePoints( CharAttribute attribute )
        {
            return myBaseAttributes[ attribute ];
        }

        public void SetAttributePoints( CharAttribute attribute, int value )
        {
            myBaseAttributes[ attribute ] = value;
        }

        public int GetBaseSkillPoints( CharSkill skill )
        {
            return myBaseSkills[ skill ];
        }

        public int GetTotalSkillPoints( CharSkill skill )
        {
            int points = GetBaseSkillPoints( skill );

            foreach ( KeyValuePair<CharAttribute, double> keyVal in skill.AttributeMods )
                points += (int) ( GetAttributePoints( keyVal.Key ) * keyVal.Value );

            return points;
        }
    }
}
