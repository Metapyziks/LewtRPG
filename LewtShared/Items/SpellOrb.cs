using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ResourceLib;
using Lewt.Shared.Rendering;
using Lewt.Shared.Magic;

namespace Lewt.Shared.Items
{
    public class SpellOrb : SpellItem
    {
        public override string ItemDescription
        {
            get
            {
                return base.ItemDescription + "\n\nMana Cost: " + Spell.ManaCost.ToString();
            }
        }

        public SpellOrb( SpellInfo spellInfo, double strength )
            : base( spellInfo, strength )
        {

        }

        public SpellOrb( SpellOrb copy )
            : base( copy )
        {

        }

        protected override void InitializeGraphics()
        {
            ItemSprite = new Rendering.Sprite( Res.Get<Texture>( "images_items_spellorb" ), 2.0f );
            ItemSprite.Colour = Spell.Colour;

            base.InitializeGraphics();
        }

        public override void Cast( Entities.Character caster, Entities.Entity applicator, OpenTK.Vector2d castPos, double angle )
        {
            caster.ManaLevel -= Spell.ManaCost;
            base.Cast( caster, applicator, castPos, angle );
        }

        public override bool CanCast( Entities.Character caster )
        {
            return caster.ManaLevel >= Spell.ManaCost;
        }
    }
}
