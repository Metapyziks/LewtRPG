using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Shared.Rendering;
using OpenTK;
using ResourceLib;
using Lewt.Shared.Entities;
using System.Reflection;
using Lewt.Shared.World;

namespace Lewt.Shared.Items
{
    [AttributeUsage( AttributeTargets.Class )]
    public class ItemInfoNameAttribute : Attribute
    {
        public readonly String Value;

        public ItemInfoNameAttribute( String value )
        {
            Value = value;
        }
    }

    public struct ItemInfo
    {
        private struct QualityInfo
        {
            public static readonly QualityInfo Default = new QualityInfo();

            private Dictionary<String, InfoValue> myValues;

            public readonly string Name;

            public double Threshold
            {
                get
                {
                    return GetDouble( "threshold" );
                }
            }

            public bool IsDefault
            {
                get
                {
                    return ( Name == null || Name == "" );
                }
            }

            public QualityInfo( InfoObject info )
            {
                Name = info.Name;

                myValues = new Dictionary<string, InfoValue>();

                foreach ( KeyValuePair<String, InfoValue> keyVal in info )
                    myValues.Add( keyVal.Key, keyVal.Value );
            }

            public bool ContainsKey( String key )
            {
                return myValues.ContainsKey( key );
            }

            public int GetInteger( String key )
            {
                return (int) myValues[ key ].AsInteger();
            }

            public double GetDouble( String key )
            {
                return myValues[ key ].AsDouble();
            }

            public bool GetBoolean( String key )
            {
                return myValues[ key ].AsBoolean();
            }

            public string GetString( String key )
            {
                return myValues[ key ].AsString();
            }
        }

        private static UInt16 stNextItemID = 0;

        private static Dictionary<String, Dictionary<UInt16, ItemInfo>> stItemInfos;
        private static Dictionary<String, Dictionary<String, ItemInfo>> stItemStrings;

        public static void LoadItemInfos()
        {
            InfoObject[] itemInfos = ResourceLib.Info.GetAll( "item" );
            Dictionary<String, List<ItemInfo>> dict = new Dictionary<string, List<ItemInfo>>();

            for ( int i = 0; i < itemInfos.Length; ++i )
            {
                InfoObject info = itemInfos[ i ];
                string itemType = info[ "type" ].AsString();
                if ( !dict.ContainsKey( itemType ) )
                    dict.Add( itemType, new List<ItemInfo>() );

                dict[ itemType ].Add( new ItemInfo( stNextItemID++, info ) );
            }

            stItemInfos = new Dictionary<string, Dictionary<ushort, ItemInfo>>();
            stItemStrings = new Dictionary<string, Dictionary<string, ItemInfo>>();

            foreach ( string itemType in dict.Keys )
            {
                List<ItemInfo> infos = dict[ itemType ];

                stItemInfos.Add( itemType, infos.ToDictionary( x => x.ID ) );
                stItemStrings.Add( itemType, infos.ToDictionary( x => x.Name ) );
            }
        }

        public static ItemInfo[] GetAll( String type )
        {
            if ( stItemInfos == null )
                LoadItemInfos();

            if ( stItemInfos.ContainsKey( type ) )
                return stItemInfos[ type ].Values.ToArray();

            return new ItemInfo[ 0 ];
        }

        public static ItemInfo Get( String type, UInt16 id )
        {
            if ( stItemInfos == null )
                LoadItemInfos();

            if ( stItemInfos.ContainsKey( type ) )
                return stItemInfos[ type ][ id ];

            throw new KeyNotFoundException();
        }

        public static ItemInfo Get( String type, String name )
        {
            if ( stItemInfos == null )
                LoadItemInfos();

            if ( stItemInfos.ContainsKey( type ) )
                return stItemStrings[ type ][ name ];

            throw new KeyNotFoundException();
        }

        private QualityInfo[] myQualities;

        public readonly UInt16 ID;
        public readonly string ItemType;
        public readonly string Name;

        public readonly string ItemName;
        public readonly string ItemDescription;
        public readonly Texture ItemTexture;

        private ItemInfo( UInt16 id, InfoObject info )
        {
            ID = id;
            ItemType = info[ "type" ].AsString();
            Name = info.Name;

            ItemName = info[ "name" ].AsString();
            ItemDescription = info[ "description" ].AsString();
            ItemTexture = Res.Get<Texture>( info[ "image" ].AsString() );

            InfoObject qualities = info[ "qualities" ] as InfoObject;
            myQualities = new QualityInfo[ qualities.Keys.Count ];

            int i = 0;
            foreach ( KeyValuePair<String, InfoValue> keyVal in qualities )
                myQualities[ i++ ] = new QualityInfo( keyVal.Value as InfoObject );

            myQualities = myQualities.OrderBy( x => x.Threshold ).ToArray();
        }

        private QualityInfo GetQualityInfo( double quality )
        {
            for ( int i = myQualities.Length - 1; i >= 0; --i )
                if ( myQualities[ i ].Threshold <= quality )
                    return myQualities[ i ];

            return QualityInfo.Default;
        }

        private QualityInfo GetNextQualityInfo( double quality )
        {
            for ( int i = 0; i < myQualities.Length; ++i )
                if ( myQualities[ i ].Threshold > quality )
                    return myQualities[ i ];

            return QualityInfo.Default;
        }

        public bool ContainsKey( String key, double quality )
        {
            return GetQualityInfo( quality ).ContainsKey( key );
        }

        public int GetInteger( String key, double quality, bool interpolate = false )
        {
            QualityInfo first = GetQualityInfo( quality );

            if ( interpolate )
            {
                QualityInfo next = GetNextQualityInfo( quality );
                if ( !next.IsDefault )
                {
                    int firstVal = first.GetInteger( key );
                    int nextVal = next.GetInteger( key );

                    double ratio = ( quality - first.Threshold ) / ( next.Threshold - first.Threshold );
                    return firstVal + (int) ( ratio * nextVal );
                }
            }

            return first.GetInteger( key );
        }

        public double GetDouble( String key, double quality, bool interpolate = false )
        {
            QualityInfo first = GetQualityInfo( quality );

            if ( interpolate )
            {
                QualityInfo next = GetNextQualityInfo( quality );
                if ( !next.IsDefault )
                {
                    double firstVal = first.GetDouble( key );
                    double nextVal = next.GetDouble( key );

                    double ratio = ( quality - first.Threshold ) / ( next.Threshold - first.Threshold );
                    return firstVal + ratio * nextVal;
                }
            }

            return first.GetInteger( key );
        }

        public bool GetBoolean( String key, double quality )
        {
            return GetQualityInfo( quality ).GetBoolean( key );
        }

        public string GetString( String key, double quality )
        {
            return GetQualityInfo( quality ).GetString( key );
        }
    }

    public class Item
    {
        public static Item Clone( Item item )
        {
            Type t = item.GetType();
            ConstructorInfo c = t.GetConstructor( new Type[] { t } );
            return c.Invoke( new object[] { item } ) as Item;
        }

        public static Item Load( Map map, System.IO.BinaryReader reader )
        {
            Item item = null;
            String typeName = reader.ReadString();
            bool equipped = reader.ReadBoolean();
            UInt32 equipper = equipped ? reader.ReadUInt32() : 0;

            Exception innerException = new Exception();

            try
            {
                Type t = Assembly.GetAssembly( typeof( Entity ) ).GetType( typeName )
                    ?? Scripts.GetType( typeName );

                if ( t.BaseType == typeof( SpellItem ) )
                    item = SpellItem.Load( reader );
                else
                {
                    UInt16 infoID = reader.ReadUInt16();
                    double quality = reader.ReadDouble();

                    String infoTypeName = ( t.GetCustomAttributes( typeof( ItemInfoNameAttribute ), true )[ 0 ] as ItemInfoNameAttribute ).Value;

                    ConstructorInfo c = t.GetConstructor( new Type[] { typeof( ItemInfo ), typeof( Double ) } );
                    item = c.Invoke( new object[] { ItemInfo.Get( infoTypeName, infoID ), quality } ) as Item;
                }

                if ( equipped )
                    item.Equip( map.GetEntity( equipper ) as Character );
            }
            catch ( Exception e )
            {
                innerException = e;
            }

            if ( item == null )
                throw new Exception( "Item of type '" + typeName + "' could not be created!", innerException );

            item.OnLoad( reader );

            return item;
        }

        private Inventory myInventory;
        private InventorySlot mySlot;
        private bool myGraphicsInitialized;
        private bool myEquipped;
        private Character myEquipper;

        protected readonly ItemInfo Info;
        public Sprite ItemSprite
        {
            get;
            protected set;
        }

        public readonly Double Quality;

        public bool IsEquipped
        {
            get
            {
                return myEquipped;
            }
        }

        public virtual bool IsEquippable
        {
            get
            {
                return false;
            }
        }

        public bool IsStackable
        {
            get
            {
                return !IsEquipped && ( MaxStackSize > 1 );
            }
        }

        public virtual int MaxStackSize
        {
            get
            {
                return 1;
            }
        }

        public virtual bool IsUseable
        {
            get
            {
                return false;
            }
        }

        public virtual String ItemName
        {
            get
            {
                return ( Info.ContainsKey( "prefix", Quality ) ?
                    Info.GetString( "prefix", Quality ) + " " : "" ) 
                    + Info.ItemName;
            }
        }
        public virtual String ItemDescription
        {
            get
            {
                return Info.ItemDescription;
            }
        }

        public virtual int ItemValue
        {
            get
            {
                return Info.GetInteger( "value", Quality, true );
            }
        }

        public bool IsInInventory
        {
            get
            {
                return myInventory != null;
            }
        }

        public Inventory Inventory
        {
            get
            {
                return myInventory;
            }
        }

        public InventorySlot Slot
        {
            get
            {
                return mySlot;
            }
        }

        public Entity Owner
        {
            get
            {
                return ( IsInInventory ? Inventory.Owner : null );
            }
        }

        protected Item( double quality )
        {
            Quality = quality;
        }

        public Item( ItemInfo itemInfo, double quality )
            : this( quality )
        {
            Info = itemInfo;
        }

        public Item( Item copy )
            : this( copy.Info, copy.Quality )
        {

        }

        protected virtual void InitializeGraphics()
        {
            if ( Info.ItemTexture != null )
                ItemSprite = new Sprite( Info.ItemTexture, 2.0f );

            myGraphicsInitialized = true;
        }

        public virtual bool CanStack( Item item )
        {
            return IsStackable && item.IsStackable && item.Quality == Quality;
        }

        public void Use( Player user )
        {
            OnItemUse( user );
        }

        protected virtual void OnItemUse( Player user )
        {
            if ( IsEquippable )
            {
                if ( IsEquipped && myEquipper == user )
                    UnEquip();
                else
                    Equip( user );
            }
        }

        public void Equip( Character target )
        {
            if ( myEquipped && myEquipper != target )
                UnEquip();

            myEquipped = true;
            myEquipper = target;
            OnEquip( target );
            Slot.UpdateContents();
        }

        protected virtual void OnEquip( Character target )
        {

        }

        public void UnEquip()
        {
            myEquipped = false;
            OnUnEquip( myEquipper );
            myEquipper = null;
            Slot.UpdateContents();
        }

        protected virtual void OnUnEquip( Character target )
        {

        }

        internal void AddToInventory( Inventory inventory, InventorySlot slot )
        {
            myInventory = inventory;
            mySlot = slot;
            OnAddToInventory( inventory );
        }

        protected virtual void OnAddToInventory( Inventory inventory )
        {

        }

        internal void RemoveFromInventory()
        {
            if ( IsEquipped )
                UnEquip();

            OnRemoveFromInventory( myInventory );
            myInventory = null;
            mySlot = null;
        }

        protected virtual void OnRemoveFromInventory( Inventory inventory )
        {

        }

        public virtual void InventoryRender( Vector2 position )
        {
            if ( !myGraphicsInitialized )
                InitializeGraphics();

            if( ItemSprite.Position != position )
                ItemSprite.Position = position;
            ItemSprite.Render();
        }

        public void Save( System.IO.BinaryWriter writer )
        {
            writer.Write( GetType().FullName );
            writer.Write( IsEquipped );

            if ( IsEquipped )
                writer.Write( myEquipper.EntityID );

            OnSave( writer );
        }

        protected virtual void OnSave( System.IO.BinaryWriter writer )
        {
            writer.Write( Info.ID );
            writer.Write( Quality );
        }

        protected virtual void OnLoad( System.IO.BinaryReader reader )
        {

        }
    }
}
