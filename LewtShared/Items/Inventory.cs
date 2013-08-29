using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Shared.Entities;

namespace Lewt.Shared.Items
{
    public class Inventory
    {
        private List<InventorySlot> mySlots;

        public readonly Entity Owner;

        public int Capacity
        {
            get
            {
                return mySlots.Count;
            }
        }

        public InventorySlot[] Slots
        {
            get
            {
                return mySlots.ToArray();
            }
        }

        public InventorySlot this[ int index ]
        {
            get
            {
                return mySlots[ index ];
            }
        }

        public Inventory( Entity owner, int capacity )
        {
            Owner = owner;
            mySlots = new List<InventorySlot>();
            SetCapacity( capacity );
        }

        public Inventory( Entity owner, System.IO.BinaryReader reader )
            : this( owner, reader.ReadUInt16() )
        {
            ushort itemCount = reader.ReadUInt16();

            for ( int i = 0; i < itemCount; ++i )
            {
                ushort index = reader.ReadUInt16();
                ushort count = reader.ReadUInt16();
                Item item = Item.Load( owner.Map, reader );

                this[ index ].Item = item;
                this[ index ].Count = count;
            }
        }

        public void SetCapacity( int capacity )
        {
            while ( Capacity < capacity )
                mySlots.Add( new InventorySlot( this, (UInt16) Capacity ) );

            while ( Capacity > capacity )
                mySlots.RemoveAt( mySlots.Count - 1 );
        }

        public bool CanAddItem( Item item )
        {
            for ( int i = 0; i < mySlots.Count; ++i )
                if ( mySlots[ i ].CanStack( item ) )
                    return true;

            return false;
        }

        public InventorySlot Add( Item item )
        {
            for ( int i = 0; i < mySlots.Count * 2; ++i )
            {
                InventorySlot slot = mySlots[ i % mySlots.Count ];
                if ( ( ( !item.IsStackable || i >= mySlots.Count ) && !slot.HasItem ) || ( slot.HasItem && slot.CanStack( item ) ) )
                {
                    slot.PushItem( item );
                    return slot;
                }
            }

            return null;
        }

        public void Remove( Item item )
        {
            for ( int i = 0; i < mySlots.Count; ++i )
            {
                if ( mySlots[ i ].HasItem && ( mySlots[ i ].Item == item || mySlots[ i ].CanStack( item ) ) )
                {
                    mySlots[ i ].PopItem();
                    return;
                }
            }
        }

        public bool Contains( Item item )
        {
            for ( int i = 0; i < mySlots.Count; ++i )
                if ( mySlots[ i ].HasItem && ( mySlots[ i ].Item == item || mySlots[ i ].CanStack( item ) ) )
                    return true;

            return false;
        }

        public void Save( System.IO.BinaryWriter writer )
        {
            writer.Write( (ushort) Capacity );

            int itemCount = 0;
            foreach ( InventorySlot slot in mySlots )
                if ( slot.HasItem )
                    ++itemCount;

            writer.Write( (ushort) itemCount );

            for ( int i = 0; i < Capacity; ++i )
            {
                if ( this[ i ].HasItem )
                {
                    writer.Write( (ushort) i );
                    writer.Write( (ushort) this[ i ].Count );
                    this[ i ].Item.Save( writer );
                }
            }
        }
    }
}
