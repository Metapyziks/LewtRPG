using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Shared.Magic;
using Lewt.Shared.Entities;
using OpenTK;

namespace Lewt.Shared.Items
{
    public class SpellItem : Item
    {
        public static SpellItem Load( System.IO.BinaryReader reader )
        {
            bool orb = reader.ReadBoolean();
            UInt16 spellID = reader.ReadUInt16();
            double strength = reader.ReadDouble();

            if ( orb )
                return new SpellOrb( SpellInfo.Get( spellID ), strength );
            else
                return new SpellScroll( SpellInfo.Get( spellID ), strength );
        }

        public readonly Spell Spell;

        public double Strength
        {
            get
            {
                return Spell.Strength;
            }
        }

        public override bool IsUseable
        {
            get
            {
                return true;
            }
        }

        public override bool IsEquippable
        {
            get
            {
                return true;
            }
        }

        public override string ItemName
        {
            get
            {
                return Spell.Name;
            }
        }

        public override string ItemDescription
        {
            get
            {
                return Spell.Description;
            }
        }

        public override int ItemValue
        {
            get
            {
                return Spell.Value;
            }
        }

        public SpellItem( SpellInfo spellInfo, double strength )
            : base( strength )
        {
            Spell = Spell.Create( spellInfo, strength );
        }

        public SpellItem( SpellItem copy )
            : this( copy.Spell.Info, copy.Strength )
        {

        }

        protected override void InitializeGraphics()
        {
            base.InitializeGraphics();
        }

        public void Cast( Character caster, Vector2d castPos, double angle )
        {
            Cast( caster, null, castPos, angle );
        }

        public virtual void Cast( Character caster, Entity applicator, Vector2d castPos, double angle )
        {
            Spell.Cast( caster, applicator, castPos, angle );
        }

        public virtual bool CanCast( Character caster )
        {
            return true;
        }

        protected override void OnEquip( Character target )
        {
            base.OnEquip( target );

            if ( target.Map.IsServer )
            {
                foreach ( InventorySlot slot in target.Inventory.Slots )
                    if ( slot.HasItem && slot.Item is SpellItem && slot.Item.IsEquipped && slot != Slot )
                        slot.Item.UnEquip();

                target.EquippedSpellItem = this;
            }
        }

        protected override void OnUnEquip( Character target )
        {
            base.OnUnEquip( target );

            if ( target.Map.IsServer )
                target.EquippedSpellItem = null;
        }

        protected override void OnSave( System.IO.BinaryWriter writer )
        {
            writer.Write( GetType() == typeof( SpellOrb ) );

            writer.Write( Spell.Info.ID );
            writer.Write( Strength );
        }
    }
}
