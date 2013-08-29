using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Client.UI;
using Lewt.Shared.Items;
using OpenTK;
using OpenTK.Input;
using Lewt.Shared.Rendering;
using OpenTK.Graphics;
using Lewt.Client.Networking;

namespace Lewt
{
    public class InventoryView : UIWindow
    {
        private class SlotButtonClickedEventArgs : EventArgs
        {
            public readonly Inventory Inventory;
            public readonly InventorySlot Slot;

            public SlotButtonClickedEventArgs( Inventory inventory, InventorySlot slot )
            {
                Inventory = inventory;
                Slot = slot;
            }
        }

        private delegate void SlotButtonClickedEventHandler( object sender, SlotButtonClickedEventArgs e );

        public class SlotButton : UIButton
        {
            public readonly InventorySlot Slot;

            public SlotButton( InventorySlot slot )
                : base( new Vector2( 40.0f, 40.0f ) )
            {
                Slot = slot;

                IsEnabled = Slot.HasItem;
                Colour = ( Slot.HasItem && Slot.Item.IsEquipped ) ? Color4.GreenYellow : Color4.White;

                Slot.SlotContentsChanged += delegate( object sender, EventArgs e )
                {
                    IsEnabled = Slot.HasItem;
                    Colour = ( Slot.HasItem && Slot.Item.IsEquipped ) ? Color4.GreenYellow : Color4.White;
                };
            }

            protected override void OnRender( OpenTK.Vector2 renderPosition = new Vector2() )
            {
                base.OnRender( renderPosition );

                if ( Slot.HasItem )
                    Slot.Item.InventoryRender( renderPosition + new Vector2( 4, 4 ) );
            }
        }

        private class ItemInfoView : UIPanel
        {
            public readonly SlotButton Button;
            public readonly Item Item;
            public bool Flipped;

            public ItemInfoView( SlotButton btn )
                : base( new Vector2( 192.0f, 128.0f ) )
            {
                Button = btn;
                Item = btn.Slot.Item;
                Colour = new Color4( 0, 0, 0, 223 );

                UILabel name = new UILabel( Font.Large, 1.5f )
                {
                    Colour = Color4.White,
                    Text = Item.ItemName,
                    Position = new Vector2( 4.0f, 4.0f ),
                    WrapWidth = 184.0f
                };
                AddChild( name );

                UILabel value = new UILabel( Font.Large, 1.0f )
                {
                    Colour = new Color4( 191, 191, 191, 255 ),
                    Text = "Value: " + Item.ItemValue.ToString(),
                    Position = new Vector2( 4.0f, name.Position.Y + name.Height + 4.0f )
                };
                AddChild( value );

                UILabel desc = new UILabel( Font.Large, 1.0f )
                {
                    Colour = new Color4( 191, 191, 191, 255 ),
                    Text = Item.ItemDescription,
                    Position = new Vector2( 4.0f, value.Position.Y + value.Height + 4.0f ),
                    WrapWidth = 184.0f
                };
                AddChild( desc );

                Height = name.Height + value.Height + desc.Height + 16.0f;

                Disable();
            }
        }

        private class InventorySingleView : UIPanel
        {
            private SlotButton[] mySlots;
            private InventoryView myInventoryView;

            public SlotButton[] Slots
            {
                get
                {
                    return mySlots;
                }
            }

            private ItemInfoView ItemView
            {
                get
                {
                    return myInventoryView.myItemView;
                }
                set
                {
                    myInventoryView.myItemView = value;
                }
            }
            
            public readonly Inventory Inventory;

            public event SlotButtonClickedEventHandler SlotButtonClicked;

            public InventorySingleView( InventoryView parent, Inventory inventory, Vector2 size )
                : base( size )
            {
                myInventoryView = parent;

                Inventory = inventory;

                mySlots = new SlotButton[ Inventory.Capacity ];

                InventorySlot[] slots = Inventory.Slots;

                int x = 0, y = 0;
                int cols = (int)( ( size.X - 4.0f ) / 44.0f );

                for ( int i = 0; i < mySlots.Length; ++i )
                {
                    SlotButton btn = new SlotButton( slots[ i ] )
                    {
                        Position = new Vector2( x * 44.0f + 4.0f, y * 44.0f + 4.0f )
                    };
                    mySlots[ i ] = btn;

                    btn.Click += delegate( object sender, MouseButtonEventArgs e )
                    {
                        if( SlotButtonClicked != null )
                            SlotButtonClicked( sender, new SlotButtonClickedEventArgs( Inventory, ( sender as SlotButton ).Slot ) );
                    };

                    btn.MouseUp += delegate( object sender, MouseButtonEventArgs e )
                    {
                        if ( myInventoryView.IsDraggingItem )
                            myInventoryView.StopDraggingItem( btn );
                    };

                    btn.MouseEnter += delegate( object sender, MouseMoveEventArgs e )
                    {
                        if ( btn.Slot.HasItem && !myInventoryView.IsDraggingItem && ( ItemView == null || ItemView.Item != btn.Slot.Item ) )
                        {
                            if ( ItemView != null )
                                Parent.RemoveChild( ItemView );

                            ItemView = new ItemInfoView( btn );
                            ItemView.Flipped = Parent.Parent.MousePosition.X + 224.0f > Parent.Parent.InnerWidth;
                            ItemView.Position = Parent.MousePosition + new Vector2( ItemView.Flipped ? -208.0f : 16.0f, 0.0f );
                            Parent.AddChild( ItemView );
                        }
                    };

                    btn.MouseMove += delegate( object sender, MouseMoveEventArgs e )
                    {
                        if ( btn.Slot.HasItem && ItemView != null && ItemView.Item == btn.Slot.Item )
                        {
                            ItemView.Flipped = Parent.Parent.MousePosition.X + 224.0f > Parent.Parent.InnerWidth;
                            ItemView.Position = Parent.MousePosition + new Vector2( ItemView.Flipped ? -208.0f : 16.0f, 0.0f );
                        }
                    };

                    btn.MouseLeave += delegate( object sender, MouseMoveEventArgs e )
                    {
                        if ( btn.Slot.HasItem && ItemView != null && ItemView.Item == btn.Slot.Item )
                        {
                            Parent.RemoveChild( ItemView );
                            ItemView = null;
                        }

                        if ( btn.MouseButtonPressed && btn.Slot.HasItem )
                            myInventoryView.StartDraggingItem( btn );
                    };

                    btn.Slot.SlotContentsChanged += delegate( object sender, EventArgs e )
                    {
                        if ( ItemView != null && ItemView.Button == btn && btn.Slot.Item != ItemView.Item )
                        {
                            Parent.RemoveChild( ItemView );
                            ItemView = null;

                            if ( btn.IsEnabled && btn.Slot.HasItem )
                            {
                                ItemView = new ItemInfoView( btn );
                                ItemView.Flipped = Parent.Parent.MousePosition.X + 224.0f > Parent.Parent.InnerWidth;
                                ItemView.Position = Parent.MousePosition + new Vector2( ItemView.Flipped ? -208.0f : 16.0f, 0.0f );
                                Parent.AddChild( ItemView );
                            }
                        }
                    };

                    AddChild( btn );

                    ++x;
                    if ( x >= cols )
                    {
                        x = 0;
                        ++y;
                    }
                }
            }
        }

        private InventorySingleView myPlayerView;
        private InventorySingleView myEntityView;

        private ItemInfoView myItemView;

        private bool myDraggingItem;
        private SlotButton myDragOriginButton;
        private Item myDraggedItem;
        private int myDragCount;
        private UISprite myDragSprite;

        public bool IsDraggingItem
        {
            get
            {
                return myDraggingItem;
            }
        }

        public InventoryView( Inventory inventory, Vector2 size )
            : base( size )
        {
            Title = "Inventory View";

            myPlayerView = new InventorySingleView( this, inventory, new Vector2( InnerWidth, InnerHeight ) );
            AddChild( myPlayerView );

            myPlayerView.SlotButtonClicked += delegate( object sender, SlotButtonClickedEventArgs e )
            {
                if ( !IsDraggingItem )
                {
                    if ( e.Slot.HasItem )
                        GameClient.SendUseItem( e.Slot );
                }
                else
                    myDraggingItem = false;
            };
        }

        public InventoryView( Inventory inventoryA, Inventory inventoryB, Vector2 size )
            : base( size )
        {
            Title = "Inventory View";

            myPlayerView = new InventorySingleView( this, inventoryA, new Vector2( InnerWidth / 2 - 2, InnerHeight ) );
            myEntityView = new InventorySingleView( this, inventoryB, new Vector2( InnerWidth / 2 - 2, InnerHeight ) )
            {
                Left = InnerWidth / 2 + 2
            };

            AddChild( myPlayerView );
            AddChild( myEntityView );

            myPlayerView.SlotButtonClicked += delegate( object sender, SlotButtonClickedEventArgs e )
            {
                if ( !IsDraggingItem )
                {
                    if ( e.Slot.HasItem )
                    {
                        Item item = e.Slot.Item;

                        if ( inventoryB.CanAddItem( item ) )
                            GameClient.SendTransferItem( inventoryA.Owner, inventoryB.Owner, e.Slot );
                    }
                }
                else
                    myDraggingItem = false;
            };

            myEntityView.SlotButtonClicked += delegate( object sender, SlotButtonClickedEventArgs e )
            {
                if ( !IsDraggingItem )
                {
                    if ( e.Slot.HasItem )
                    {
                        Item item = e.Slot.Item;

                        if ( inventoryA.CanAddItem( item ) )
                            GameClient.SendTransferItem( inventoryB.Owner, inventoryA.Owner, e.Slot );
                    }
                }
                else
                    myDraggingItem = false;
            };

            Closed += delegate( object sender, EventArgs e )
            {
                if ( inventoryA != null )
                    ( inventoryA.Owner as IContainer ).Inventory = inventoryA = null;
                if ( inventoryB != null )
                    ( inventoryB.Owner as IContainer ).Inventory = inventoryB = null;
            };
        }

        private void StartDraggingItem( SlotButton button )
        {
            myDraggingItem = true;
            myDragOriginButton = button;
            myDraggedItem = button.Slot.Item;
            myDragCount = button.Slot.Count;
            myDragSprite = new UISprite( myDraggedItem.ItemSprite );
            myDragSprite.Position = MousePosition - myDragSprite.Size * 0.5f - new Vector2( PaddingLeft, PaddingTop );
            AddChild( myDragSprite );

            button.Slot.Clear();

            foreach ( SlotButton btn in myPlayerView.Slots )
                btn.Enable();

            if( myEntityView != null )
                foreach ( SlotButton btn in myEntityView.Slots )
                    btn.Enable();
        }

        private void StopDraggingItem( SlotButton button )
        {
            myDragOriginButton.Slot.Item = myDraggedItem;
            myDragOriginButton.Slot.Count = myDragCount;

            RemoveChild( myDragSprite );
            myDragSprite = null;

            if ( myDragOriginButton != button )
            {
                myDraggingItem = false;

                button.Slot.Swap( myDragOriginButton.Slot );

                InventorySingleView invViewA = myDragOriginButton.Parent as InventorySingleView;
                InventorySingleView invViewB = button.Parent as InventorySingleView;

                GameClient.SendTransferItem( invViewA.Inventory.Owner, invViewB.Inventory.Owner, myDragOriginButton.Slot, button.Slot );
            }

            foreach ( SlotButton btn in myPlayerView.Slots )
                if( !btn.Slot.HasItem )
                    btn.Disable();

            if ( myEntityView != null )
                foreach ( SlotButton btn in myEntityView.Slots )
                    if ( !btn.Slot.HasItem )
                        btn.Disable();
        }

        protected override void OnMouseMove( Vector2 mousePos )
        {
            base.OnMouseMove( mousePos );

            if ( IsDraggingItem && myDragSprite != null )
                myDragSprite.Position = MousePosition - myDragSprite.Size * 0.5f - new Vector2( PaddingLeft, PaddingTop );
        }
    }
}
