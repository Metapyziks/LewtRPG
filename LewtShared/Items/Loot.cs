using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lewt.Shared.Items
{
    [ItemInfoName( "loot" )]
    public class Loot : Item
    {
        public static ItemInfo[] GetAll()
        {
            return ItemInfo.GetAll( "loot" );
        }

        public static ItemInfo GetInfo( String infoName )
        {
            return ItemInfo.Get( "loot", infoName );
        }

        public static ItemInfo GetInfo( UInt16 id )
        {
            return ItemInfo.Get( "loot", id );
        }

        public static Loot Create( String infoName, double quality )
        {
            return new Loot( GetInfo( infoName ), quality );
        }

        public Loot( ItemInfo itemInfo, double quality )
            : base( itemInfo, quality )
        {

        }
    }
}
