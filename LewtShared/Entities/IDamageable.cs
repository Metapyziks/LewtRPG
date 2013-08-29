using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lewt.Shared.Entities
{
    public interface IDamageable
    {
        void Hurt( Entity attacker, Entity weapon, DamageType damageType, int damage );
    }
}
