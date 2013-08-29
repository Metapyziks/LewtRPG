using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ResourceLib;
using Lewt.Shared.Rendering;
using Lewt.Shared.Magic;

namespace Lewt.Shared.Items
{
    public class SpellScroll : SpellItem
    {
        public override int ItemValue
        {
            get
            {
                return (int) Math.Ceiling( base.ItemValue * 0.5 );
            }
        }

        public SpellScroll( SpellInfo spellInfo, double strength )
            : base( spellInfo, strength )
        {

        }

        public SpellScroll( SpellOrb copy )
            : base( copy )
        {

        }

        protected override void InitializeGraphics()
        {
            ItemSprite = new Rendering.Sprite( Res.Get<Texture>( "images_items_spellscroll" ), 2.0f );

            base.InitializeGraphics();
        }

        public override bool CanCast( Entities.Character caster )
        {
            return caster.EquippedSpellItem == this && caster.Inventory.Contains( this );
        }

        public override void Cast( Entities.Character caster, Entities.Entity applicator, OpenTK.Vector2d castPos, double angle )
        {
            base.Cast( caster, applicator, castPos, angle );
            Inventory.Remove( this );
            caster.EquippedSpellItem = null;
        }
    }
}
