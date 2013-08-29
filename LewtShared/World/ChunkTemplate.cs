using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using ResourceLib;

using Lewt.Shared.Entities;

namespace Lewt.Shared.World
{
    public class RChunkTemplateManager : RManager
    {
        public RChunkTemplateManager()
            : base( typeof( ChunkTemplate ), 3, "cnk" )
        {

        }

        public override ResourceItem[] LoadFromFile( string keyPrefix, string fileName, string fileExtension, FileStream stream )
        {
            ChunkTemplate temp = new ChunkTemplate( stream );
            ChunkTemplate.AddTemplate( temp );
            return new ResourceItem[] { new ResourceItem( keyPrefix + fileName, temp ) };
        }

        public override object LoadFromArchive( BinaryReader stream )
        {
            ChunkTemplate temp = new ChunkTemplate( stream );
            ChunkTemplate.AddTemplate( temp );
            return temp;
        }

        public override void SaveToArchive( BinaryWriter stream, object item )
        {
            ( item as ChunkTemplate ).SaveToStream( stream );
        }
    }

    public struct ChunkConnector
    {
        public int X;
        public int Y;

        public bool Horizontal;
        public bool Vertical
        {
            get
            {
                return !Horizontal;
            }
            set
            {
                Horizontal = !value;
            }
        }

        public bool BottomOrRight;
        public bool TopOrLeft
        {
            get
            {
                return !BottomOrRight;
            }
            set
            {
                BottomOrRight = !value;
            }
        }

        public byte Skin
        {
            get
            {
                return Template.GetSkin( X, Y );
            }
        }

        public int Size;

        public ChunkConnector( BinaryReader reader, ChunkTemplate template )
        {
            X = reader.ReadByte();
            Y = reader.ReadByte();
            byte data = reader.ReadByte();

            Horizontal = ( data & 0x80 ) != 0;
            BottomOrRight = ( Horizontal && X != 0 ) || ( !Horizontal && Y != 0 );
            Size = data & 0x7F;

            Template = template;
        }

        public void WriteToStream( BinaryWriter writer )
        {
            writer.Write( (byte) X );
            writer.Write( (byte) Y );
            writer.Write( (byte) ( ( Horizontal ? 1 : 0 ) << 0x7 | ( Size & 0x7F ) ) );
        }

        internal ChunkTemplate Template;
    }

    public enum ConnectorFace : int
    {
        Left = 0,
        Top = 1,
        Right = 2,
        Bottom = 3
    }

    public class ChunkTemplate
    {
        private struct TileTemplate
        {
            public bool IsWall;
            public byte Skin;
            public byte Alt;

            public void Save( BinaryWriter writer )
            {
                writer.Write( (byte) ( ( ( IsWall ? 1 : 0 ) & 0x1 ) << 7 | ( Alt & 0x3 ) << 5 | ( Skin & 0x1F ) ) );
            }

            public void Load( BinaryReader reader )
            {
                byte data = reader.ReadByte();

                IsWall = ( data >> 7 & 0x1 ) == 1;
                Alt = (byte) ( data >> 5 & 0x3 );
                Skin = (byte) ( data & 0x1F );
            }
        }

        private static Dictionary<DungeonClass,List<ChunkTemplate>> stTemplates = new Dictionary<DungeonClass, List<ChunkTemplate>>();

        public static void AddTemplate( ChunkTemplate template )
        {
            foreach ( DungeonClass mapType in template.MapTypes )
            {
                if ( !stTemplates.ContainsKey( mapType ) )
                    stTemplates.Add( mapType, new List<ChunkTemplate>() );

                stTemplates[ mapType ].Add( template );
            }
        }

        public static ChunkTemplate[] GetTemplates( DungeonClass mapType )
        {
            if ( stTemplates.ContainsKey( mapType ) )
                return stTemplates[ mapType ].ToArray();

            return new ChunkTemplate[ 0 ];
        }

        private ChunkConnector[][] myConnectors;
        private ChunkConnector[][][] myOrganisedConnectors;

        private TileTemplate[,] myTiles;

        private int myWidth;
        private int myHeight;

        private DungeonClass[] myMapTypes;

        private Entity[] myEntities;

        public int Width
        {
            get
            {
                return myWidth;
            }
        }
        public int Height
        {
            get
            {
                return myHeight;
            }
        }

        public DungeonClass[] MapTypes
        {
            get
            {
                return myMapTypes;
            }
        }

        public Entity[] Entities
        {
            get
            {
                return myEntities;
            }
        }

        public ChunkTemplate( string filePath )
        {
            FileStream stream = new FileStream( filePath, FileMode.Open, FileAccess.Read );
            LoadFromStream( stream );
            stream.Close();
        }

        public ChunkTemplate( Stream stream )
        {
            LoadFromStream( stream );
        }

        public ChunkTemplate( BinaryReader stream )
        {
            LoadFromStream( stream );
        }

        public ChunkTemplate( Chunk chunk, ChunkConnector[][] connectors, params DungeonClass[] mapTypes )
        {
            myConnectors = connectors;

            myWidth = chunk.Width;
            myHeight = chunk.Height;

            myMapTypes = mapTypes;
            myEntities = chunk.Entities;

            myTiles = new TileTemplate[ Width, Height ];

            for ( int x = 0; x < Width; ++x )
                for ( int y = 0; y < Height; ++y )
                {
                    GameTile tile = chunk.GetTile( x - chunk.X, y - chunk.Y );

                    myTiles[ x, y ] = new TileTemplate
                    {
                        IsWall = tile.IsWall,
                        Skin = tile.Skin,
                        Alt = tile.Alt
                    };
                }

            FindOrganisedConnectors();
        }

        private void FindOrganisedConnectors()
        {
            myOrganisedConnectors = new ChunkConnector[ 4 ][][];

            for ( int i = 0; i < 4; ++i )
            {
                int length = ( ( i % 2 ) == 0 ? Height : Width ) - 2;

                myOrganisedConnectors[ i ] = new ChunkConnector[ length ][];

                List<ChunkConnector>[] lists = new List<ChunkConnector>[ length ];
                for ( int j = 0; j < length; ++j )
                    lists[ j ] = new List<ChunkConnector>();

                foreach ( ChunkConnector con in myConnectors[ i ] )
                    lists[ con.Size - 1 ].Add( con );

                for ( int j = 0; j < length; ++j )
                    myOrganisedConnectors[ i ][ j ] = lists[ j ].ToArray();
            }
        }

        public bool GetIsWall( int x, int y )
        {
            return myTiles[ x, y ].IsWall;
        }

        public byte GetSkin( int x, int y )
        {
            return myTiles[ x, y ].Skin;
        }

        public byte GetAlt( int x, int y )
        {
            return myTiles[ x, y ].Alt;
        }

        public void SetIsWall( int x, int y, bool value )
        {
            myTiles[ x, y ].IsWall = value;
        }

        public void SetSkin( int x, int y, byte value )
        {
            myTiles[ x, y ].Skin = value;
        }

        public void SetAlt( int x, int y, byte value )
        {
            myTiles[ x, y ].Alt = value;
        }

        /// <param name="side">0 - left
        /// 1 - top
        /// 2 - right
        /// 3 - bottom</param>
        public bool HasConnectors( ConnectorFace side )
        {
            return myConnectors[ (int) side ].Length > 0;
        }

        /// <param name="side">0 - left
        /// 1 - top
        /// 2 - right
        /// 3 - bottom</param>
        public ChunkConnector[] GetConnectors( ConnectorFace side, int size = 0, int skin = -1 )
        {
            if( size == 0 )
                return Array.FindAll( myConnectors[ (int) side ], x => ( skin == -1 || x.Skin == skin ) );

            if ( size > ( ( (int) side % 2 ) == 0 ? Height : Width ) - 2 )
                return new ChunkConnector[ 0 ];

            return Array.FindAll( myOrganisedConnectors[ (int) side ][ size - 1 ], x => ( skin == -1 || x.Skin == skin ) );
        }

        public void SaveToFile( string filePath )
        {
            FileStream fstr = new FileStream( filePath, FileMode.Create, FileAccess.Write );
            SaveToStream( fstr );
            fstr.Close();
        }

        public void SaveToStream( Stream stream )
        {
            SaveToStream( new BinaryWriter( stream ) );
        }

        public void SaveToStream( BinaryWriter stream )
        {
            stream.Write( (short) Width );
            stream.Write( (short) Height );
            
            for ( int x = 0; x < Width; ++x )
                for ( int y = 0; y < Height; ++y )
                    myTiles[ x, y ].Save( stream );

            stream.Write( (short) MapTypes.Length );
            foreach ( DungeonClass type in MapTypes )
                stream.Write( type.Name );

            stream.Write( (short) myEntities.Length );

            for ( int i = 0; i < myEntities.Length; ++i )
            {
                myEntities[ i ].Save( stream, false );
                stream.Write( myEntities[ i ].Probability );
            }

            for ( int i = 0; i < 4; ++i )
            {
                stream.Write( (byte) myConnectors[ i ].Length );

                foreach ( ChunkConnector con in myConnectors[ i ] )
                    con.WriteToStream( stream );
            }
        }

        protected void LoadFromStream( Stream stream )
        {
            LoadFromStream( new BinaryReader( stream ) );
        }

        protected void LoadFromStream( BinaryReader stream )
        {
            myWidth = stream.ReadInt16();
            myHeight = stream.ReadInt16();

            myTiles = new TileTemplate[ Width, Height ];

            for ( int x = 0; x < Width; ++x )
                for ( int y = 0; y < Height; ++y )
                {
                    myTiles[ x, y ] = new TileTemplate();
                    myTiles[ x, y ].Load( stream );
                }
            
            int types = stream.ReadInt16();

            myMapTypes = new DungeonClass[ types ];

            for ( int i = 0; i < types; ++i )
                myMapTypes[ i ] = DungeonClass.Get( stream.ReadString() );

            int entCount = stream.ReadInt16();

            myEntities = new Entity[ entCount ];

            for ( int i = 0; i < entCount; ++i )
            {
                myEntities[ i ] = Entity.Load( stream, false );
                myEntities[ i ].Probability = stream.ReadDouble();
            }
            
            myConnectors = new ChunkConnector[ 4 ][];

            for ( int i = 0; i < 4; ++i )
            {
                int connectors = stream.ReadByte();

                myConnectors[ i ] = new ChunkConnector[ connectors ];
                
                for( int j = 0; j < connectors; ++ j )
                    myConnectors[ i ][ j ] = new ChunkConnector( stream, this );
            }

            FindOrganisedConnectors();
        }
    }
}
