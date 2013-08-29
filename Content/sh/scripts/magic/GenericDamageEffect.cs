using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Shared.Magic;
using Lewt.Shared.Entities;

namespace Scripts.Magic
{
    public class GenericDamageEffect : MagicalEffect
    {
        public GenericDamageEffect( Entity caster, Entity applicator, SpellInfo spellInfo, double strength )
            : base( caster, applicator, spellInfo, strength )
        {

        }

        public GenericDamageEffect( System.IO.BinaryReader reader )
            : base( reader )
        {

        }

        protected override void OnStartEffect( Entity target )
        {
            base.OnStartEffect( target );

            if ( target is IDamageable )
                ( target as IDamageable ).Hurt( Caster, Applicator, SpellInfo.DamageType, SpellInfo.GetInteger( "hit damage", Strength ) );
        }
    }
}
