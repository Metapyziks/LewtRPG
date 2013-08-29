using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

using Lewt.Shared.Entities;
using OpenTK.Graphics;

namespace Lewt.Shared.Magic
{
    public class Spell
    {
        public static Spell Create( SpellInfo info, double strength )
        {
            switch ( info.CastType )
            {
                case CastType.Projectile:
                    return new ProjectileSpell( info, strength );
                default:
                    return null;
            }
        }

        public readonly SpellInfo Info;
        public readonly double Strength;

        public Color4 Colour
        {
            get
            {
                return Info.Colour;
            }
        }

        public int ManaCost
        {
            get
            {
                return Info.GetInteger( "mana cost", Strength );
            }
        }

        public int Value
        {
            get
            {
                return Info.GetInteger( "value", Strength );
            }
        }

        public String Name
        {
            get
            {
                return Info.GetName( Strength );
            }
        }

        public virtual String Description
        {
            get
            {
                return Info.GetDescription( Strength );
            }
        }

        protected Spell( SpellInfo info, double strength )
        {
            Info = info;
            Strength = strength;
        }

        public void Cast( Entity caster, Vector2d castPos, double angle )
        {
            Cast( caster, null, castPos, angle );
        }

        public virtual void Cast( Entity caster, Entity applicator, Vector2d castPos, double angle )
        {

        }
    }
}
