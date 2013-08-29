using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Shared.Entities;

namespace Lewt.Shared.Items
{
    public class InventorySlot
    {
        private int myCount;
        private Item myItem;

        public readonly Inventory Inventory;
        public readonly UInt16 ID;

        public Item Item
        {
            get
            {
                return myItem;
            }
            set
            {
                if ( value == myItem )
                    return;

                if ( myItem != null )
                    myItem.RemoveFromInventory();

                myItem = value;

                if ( myItem != null && myItem.Inventory != Inventory )
                {
                    if ( myItem.Inventory != null )
                        myItem.RemoveFromInventory();
                    myItem.AddToInventory( Inventory, this );
                }

                UpdateContents();
            }
        }

        public bool HasItem
        {
            get
            {
                return Item != null;
            }
        }

        public bool IsEquipped
        {
            get
            {
                return HasItem && Item.IsEquipped;
            }
        }

        public bool IsStackable
        {
            get
            {
                return ( HasItem ? Item.IsStackable : false );
            }
        }

        public int MaxStackSize
        {
            get
            {
                return ( HasItem ? Item.MaxStackSize : 0 );
            }
        }

        public int Count
        {
            get
            {
                return ( HasItem ? myCount : 0 );
            }
            set
            {
                if ( HasItem )
                {
                    myCount = Tools.Clamp( value, 0, Item.MaxStackSize );
                    if ( myCount == 0 )
                        Item = null;
                    else
                        UpdateContents();
                }
            }
        }

        public String ItemName
        {
            get
            {
                return ( HasItem ? Item.ItemName : "Empty" );
            }
        }

        public String ItemDescription
        {
            get
            {
                return ( HasItem ? Item.ItemDescription : "" );
            }
        }

        public event EventHandler SlotContentsChanged;

        public InventorySlot( Inventory inventory, UInt16 id )
        {
            Inventory = inventory;
            ID = id;
        }

        public void UpdateContents()
        {
            if ( SlotContentsChanged != null )
                SlotContentsChanged( this, new EventArgs() );
        }

        public bool CanStack( Item item )
        {
            return !HasItem || ( Count < MaxStackSize && Item.CanStack( item ) );
        }

        public void SetItem( Item item )
        {
            Item = item;
            Count = 1;
        }

        public void PushItem( Item item )
        {
            if ( !HasItem )
                SetItem( item );
            else if ( IsStackable && Count < MaxStackSize )
                Count++;
        }

        public void PushItems( Item[] items )
        {
            if ( !HasItem )
            {
                Item = items[ 0 ];
                Count = items.Length;
            }
            else
                Count += items.Length;
        }

        public Item PopItem()
        {
            if ( HasItem )
            {
                Item toReturn = Item;
                Count--;

                if ( IsStackable && Count > 0 )
                    return Item.Clone( toReturn ) as Item;
                else
                    return toReturn;
            }

            return null;
        }

        public Item[] PopItems( int count )
        {
            count = Math.Min( Count, count );

            Item[] items = new Item[ count ];
            for ( int i = 1; i < count; ++i )
                items[ i ] = Item.Clone( Item ) as Item;

            if ( count == Count )
                items[ 0 ] = Item;
            else
                items[ 0 ] = Item.Clone( Item ) as Item;
                
            Count -= count;

            return items;
        }

        public void Swap( InventorySlot slot )
        {
            Item item = Item;
            int count = Count;

            Item = slot.Item;
            Count = slot.Count;

            slot.Item = item;
            slot.Count = count;
        }

        public void Clear()
        {
            Item = null;
        }
    }
}
