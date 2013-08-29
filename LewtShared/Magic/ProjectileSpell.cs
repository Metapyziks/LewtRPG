using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

using Lewt.Shared.Entities;

namespace Lewt.Shared.Magic
{
    public class ProjectileSpell : Spell
    {
        public ProjectileSpell( SpellInfo info, double strength )
            : base( info, strength )
        {

        }

        protected MagicalEffect CreateEffect( Entity caster )
        {
            return CreateEffect( caster, null );
        }

        protected MagicalEffect CreateEffect( Entity caster, Entity applicator )
        {
            return MagicalEffect.Create( caster, applicator, Info, Strength );
        }

        public override void Cast( Entity caster, Entity applicator, Vector2d castPos, double angle )
        {
            base.Cast( caster, applicator, castPos, angle );

            if ( caster.Map.IsServer )
            {
                MagicalProjectile proj = new MagicalProjectile( caster, castPos, angle, Info.GetDouble( "move speed", Strength ), Colour, CreateEffect( caster, applicator ) );
                caster.Map.AddEntity( proj );
                proj.Start();
            }
        }
    }
}
