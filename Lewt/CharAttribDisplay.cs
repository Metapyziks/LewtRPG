using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Client.UI;
using Lewt.Shared.Entities;
using OpenTK;
using Lewt.Shared.Rendering;
using Lewt.Shared.Stats;

namespace Lewt
{
    class CharAttribDisplay : UIWindow
    {
        private UILabel myText;

        public CharAttribDisplay( Character character )
        {
            Title = "Attributes and Skills";

            String str = "Attributes:";
            foreach ( CharAttribute attrib in CharAttribute.GetAll() )
            {
                String line = "\n  " + attrib.ToString();

                int baseVal = character.GetAttributeLevel( attrib, false );
                int currVal = character.GetAttributeLevel( attrib, true );
                int diff = currVal - baseVal;

                while ( line.Length < 20 )
                    line += " ";

                line += ": " + currVal.ToString();

                while ( line.Length < 26 )
                    line += " ";

                line += "(" + ( diff > 0 ? "+" : "" ) + diff.ToString() + ")";

                str += line;
            }

            str += "\n\nSkills:";
            foreach ( CharSkill skill in CharSkill.GetAll() )
            {
                String line = "\n  " + skill.ToString();

                int baseVal = character.GetSkillLevel( skill, false );
                int currVal = character.GetSkillLevel( skill, true );
                int diff = currVal - baseVal;

                while ( line.Length < 20 )
                    line += " ";

                line += ": " + currVal.ToString();

                while ( line.Length < 26 )
                    line += " ";

                line += "(" + ( diff > 0 ? "+" : "" ) + diff.ToString() + ")";

                str += line;
            }

            str += "\n\nHit Points: " + character.HitPoints + "/" + character.MaxHitPoints;
            str += "\nMana Level: " + character.ManaLevel + "/" + character.MaxManaLevel;
#if DEBUG
            str += "\nWalk Speed: " + character.WalkSpeed.ToString( "F" );
            str += "\nMana Regen: " + character.ManaRechargePeriod.ToString( "F" );
            str += "\nCast Delay: " + character.CastCooldownTime.ToString( "F" );
            str += "\nHeal Delay: " + character.FastHPRechargeDelay.ToString( "F" );
#endif

            myText = new UILabel( Font.Large )
            {
                Text = str,
                Position = new Vector2( 4, 4 )
            };
            AddChild( myText );

            Width = myText.Width + 8 + PaddingLeft + PaddingRight;
            Height = myText.Height + 8 + PaddingTop + PaddingBottom;
        }
    }
}
