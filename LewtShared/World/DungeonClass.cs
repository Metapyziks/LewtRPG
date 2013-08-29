using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ResourceLib;

using Lewt.Shared.Rendering;

namespace Lewt.Shared.World
{
    public struct DungeonClass
    {
        private static DungeonClass[] stDungeonClasses;

        private static void LoadDungeonClasses()
        {
            InfoObject[] dungeonInfos = Info.GetAll( "dungeonclass" );
            stDungeonClasses = new DungeonClass[ dungeonInfos.Length ];

            for ( int i = 0; i < dungeonInfos.Length; ++i )
                stDungeonClasses[ i ] = new DungeonClass( dungeonInfos[ i ] );
        }

        public static DungeonClass[] GetAll()
        {
            if ( stDungeonClasses == null )
                LoadDungeonClasses();

            return stDungeonClasses;
        }

        public static DungeonClass Get( String name )
        {
            if ( stDungeonClasses == null )
                LoadDungeonClasses();

            foreach ( DungeonClass dClass in stDungeonClasses )
                if ( dClass.Name == name )
                    return dClass;

            throw new Exception( "Dungeon class not found with the name '" + name + "'" );
        }

        private String myIconTextureName;
        private Texture myIconTexture;

        private String myTileTextureName;
        private Texture myTileTexture;

        public readonly String Name;

        public readonly String FullName;

        public readonly byte EvilMin;
        public readonly byte EvilMax;

        public readonly byte SavageryMin;
        public readonly byte SavageryMax;

        public readonly byte TemperatureMin;
        public readonly byte TemperatureMax;

        public readonly int AreaMin;
        public readonly int AreaMax;

        public Texture IconTexture
        {
            get
            {
                if ( myIconTexture == null )
                    myIconTexture = Res.Get<Texture>( myIconTextureName );

                return myIconTexture;
            }
        }
        public readonly byte[]  IconIndexes;

        public Texture TileTexture
        {
            get
            {
                if ( myTileTexture == null )
                    myTileTexture = Res.Get<Texture>( myTileTextureName );

                return myTileTexture;
            }
        }
        public readonly byte DefaultSkinIndex;

        public ChunkTemplate[] ChunkTemplates
        {
            get
            {
                return ChunkTemplate.GetTemplates( this );
            }
        }

        private DungeonClass( InfoObject info )
        {
            Name = info.Name;

            FullName = info[ "name" ].AsString();

            EvilMin = (byte) info[ "evil min" ].AsInteger();
            EvilMax = (byte) info[ "evil max" ].AsInteger();

            SavageryMin = (byte) info[ "savage min" ].AsInteger();
            SavageryMax = (byte) info[ "savage max" ].AsInteger();

            TemperatureMin = (byte) info[ "temp min" ].AsInteger();
            TemperatureMax = (byte) info[ "temp max" ].AsInteger();

            AreaMin = (int) info[ "area min" ].AsInteger();
            AreaMax = (int) info[ "area max" ].AsInteger();

            myIconTextureName = info[ "icon file" ].AsString();
            myIconTexture = null;

            InfoValue[] icons = info[ "icon indexes" ].AsArray();
            IconIndexes = new byte[ icons.Length ];
            for ( int i = 0; i < icons.Length; ++i )
                IconIndexes[ i ] = (byte) icons[ i ].AsInteger();

            myTileTextureName = info[ "tile file" ].AsString();
            myTileTexture = null;

            DefaultSkinIndex = (byte) info[ "default skin index" ].AsInteger();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
