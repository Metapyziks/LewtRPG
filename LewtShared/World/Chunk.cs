using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using OpenTK.Graphics;

using Lewt.Shared.Rendering;
using Lewt.Shared.Entities;

namespace Lewt.Shared.World
{
    public class Chunk
    {
        public static bool SmoothLighting = true;

        public static Chunk Load( Stream stream, Map parent )
        {
            UInt32 length = BitConverter.ToUInt32( stream.ReadBytes( 4 ), 0 );
            byte[] data = new byte[ length ];
            stream.Read( data, 0, (int) length );

            Chunk chunk;

            MemoryStream mstr = new MemoryStream( data );
            using ( BinaryReader reader = new BinaryReader( mstr ) )
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();

                chunk = new Chunk( x, y, width, height, parent );

                chunk.OnLoad( reader );
            }
            mstr.Close();

            parent.AddChunk( chunk );

            return chunk;
        }

        private GameTile[,] myTiles;
        private LightColour[,] myLighting;
        private List<Entity> myEnts;
        private VertexBuffer myVB;
        private Chunk[] myNeighbours;

        private bool myTilesChanged;
        private bool myLightingChanged;
        private bool myEntSortingChanged;

        private int myLightCount;

        public readonly int X;
        public readonly int Y;

        public readonly int Width;
        public readonly int Height;

        public int Area
        {
            get
            {
                return Width * Height;
            }
        }

        public readonly Map Map;

        internal List<Light> LightSources;

        internal VertexBuffer VertexBuffer
        {
            get
            {
                return myVB;
            }
        }

        public virtual Entity[] Entities
        {
            get
            {
                return myEnts.ToArray();
            }
        }

        public Chunk[] Neighbours
        {
            get
            {
                return myNeighbours;
            }
        }

        public Chunk( int x, int y, ChunkTemplate template, Map map )
            : this( x, y, template.Width, template.Height, map )
        {
            for ( int tx = 0; tx < Width; ++tx )
                for ( int ty = 0; ty < Height; ++ty )
                {
                    GameTile tile = myTiles[ tx, ty ];
                    tile.IsWall = template.GetIsWall( tx, ty );
                    tile.Skin = template.GetSkin( tx, ty );
                    tile.Alt = template.GetAlt( tx, ty );
                }
            
            myEnts = new List<Entity>();

            foreach ( Entity ent in template.Entities )
            {
                if ( !Map.AlwaysPlaceEntities && Tools.Random() > ent.Probability )
                    continue;

                Entity clone = Entity.Clone( ent );
                clone.OriginX += X;
                clone.OriginY += Y;
                AddEntity( clone );
            }

            myNeighbours = new Chunk[ 0 ];
        }

        public Chunk( int x, int y, int width, int height, Map map )
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;

            Map = map;

            myTiles = new GameTile[ width, height ];

            for ( int tx = 0; tx < Width; ++tx )
                for ( int ty = 0; ty < Height; ++ty )
                {
                    if( map.IsInterior )
                        myTiles[ tx, ty ] = new InteriorTile( X + tx, Y + ty, this, map );
                    else
                        myTiles[ tx, ty ] = new ExteriorTile( X + tx, Y + ty, this, map );
                }
                    
            myLighting = new LightColour[ width + 1, height + 1 ];

            FillLighting( LightColour.Default );

            myEnts = new List<Entity>();

            myVB = new Rendering.VertexBuffer( map.IsExterior );

            LightSources = new List<Light>();

            myLightingChanged = true;
            myTilesChanged = true;
            myEntSortingChanged = true;

            myNeighbours = new Chunk[ 0 ];
        }

        public virtual void PostWorldInitialize( bool editor = false )
        {
            List<Chunk> neighbours = new List<Chunk>();

            foreach ( Chunk chunk in Map.TileChunks )
                if ( chunk != this &&
                    ( chunk.X == X + Width || chunk.X + chunk.Width == X &&
                        chunk.Y < Y + Height && chunk.Y + chunk.Height > Y ) ||
                    ( chunk.Y == Y + Height || chunk.Y + chunk.Height == Y &&
                        chunk.X < X + Width && chunk.X + chunk.Width > X ) )
                        neighbours.Add( chunk );

            myNeighbours = neighbours.ToArray();

            for ( int tx = 0; tx < Width; ++tx )
                for ( int ty = 0; ty < Height; ++ty )
                    myTiles[ tx, ty ].PostWorldInitialize();

            UpdateTiles();

            foreach ( Entity ent in Entities )
            {
                RemoveEntity( ent );
                ent.Chunk = null;
                Map.AddEntity( ent );
                ent.PostWorldInitialize( editor );
            }
        }

        public void FillTiles( bool wall, byte skin = 255, byte alt = 255 )
        {
            for( int tx = 0; tx < Width; ++ tx )
                for ( int ty = 0; ty < Height; ++ty )
                {
                    GameTile t = myTiles[ tx, ty ];

                    t.IsWall = wall;

                    if ( skin != 255 )
                        t.Skin = skin;

                    if ( alt != 255 )
                        t.Alt = alt;
                }

            UpdateTiles();
        }

        public void FillLighting( LightColour colour )
        {
            for ( int x = 0; x < Width + 1; ++x )
                for ( int y = 0; y < Height + 1; ++y )
                    myLighting[ x, y ] = colour;

            UpdateLighting();
        }

        public void UpdateLighting()
        {
            myLightingChanged = true;
        }

        public void UpdateTiles()
        {
            myTilesChanged = true;
        }

        public void UpdateEntitySorting()
        {
            myEntSortingChanged = true;
        }

        public virtual void AddEntity( Entity ent )
        {
            myEnts.Add( ent );
            myEntSortingChanged = true;
        }

        public virtual void RemoveEntity( Entity ent )
        {
            myEnts.Remove( ent );
        }

        public virtual GameTile GetTile( int x, int y )
        {
            if ( x - X < 0 || y - Y < 0 || x - X >= Width || y - Y >= Height )
            {
                if ( Map.IsInterior )
                    return InteriorTile.Default;
                else
                    return ExteriorTile.Default;
            }

            return myTiles[ x - X, y - Y ];
        }

        public virtual LightColour GetLight( double x, double y )
        {
            int x0 = (int) Math.Floor( x - X );
            int y0 = (int) Math.Floor( y - Y );

            if ( x0 == x && y0 == y )
                return myLighting[ x0, y0 ];

            int x1 = x0 + 1;
            int y1 = y0 + 1;

            x -= x0;
            y -= y0;

            LightColour x0y0a = myLighting[ x0, y0 ];
            LightColour x1y0a = myLighting[ x1, y0 ];
            LightColour x1y1a = myLighting[ x1, y1 ];
            LightColour x0y1a = myLighting[ x0, y1 ];

            float r = (float) ( ( x0y0a.R * ( 1 - x ) + x1y0a.R * x ) * ( 1 - y ) + ( x0y1a.R * ( 1 - x ) + x1y1a.R * x ) * y );
            float g = (float) ( ( x0y0a.G * ( 1 - x ) + x1y0a.G * x ) * ( 1 - y ) + ( x0y1a.G * ( 1 - x ) + x1y1a.G * x ) * y );
            float b = (float) ( ( x0y0a.B * ( 1 - x ) + x1y0a.B * x ) * ( 1 - y ) + ( x0y1a.B * ( 1 - x ) + x1y1a.B * x ) * y );

            return new LightColour( r, g, b );
        }

        public void CalculateLighting()
        {
            if ( Map.IsExterior )
                FillLighting( new LightColour( 1.0f, 1.0f, 1.0f ) );
            else
            {
                FillLighting( new LightColour( 0.0f, 0.0f, 0.0f ) );

                for ( int i = LightSources.Count - 1; i >= 0; --i )
                {
                    Light source = LightSources[ i ];

                    if ( source.IsRemoved )
                    {
                        LightSources.Remove( source );
                        continue;
                    }

                    int minX = Math.Max( source.MinimumX, X );
                    int minY = Math.Max( source.MinimumY, Y );
                    int maxX = Math.Min( source.MaximumX, X + Width );
                    int maxY = Math.Min( source.MaximumY, Y + Height );

                    if ( minX > maxX || minY > maxY )
                        LightSources.Remove( source );
                    else
                        for ( int x = minX; x <= maxX; ++x )
                            for ( int y = minY; y <= maxY; ++y )
                                myLighting[ x - X, y - Y ] |= source.GetLightBrightness( Map, x, y );
                }
            }
        }

        protected virtual float[] GetRawTileData()
        {
            List<float[]> verts = new List<float[]>();

            int count = 0;

            for( int x = 0; x < Width; ++ x )
                for( int y = 0; y < Height; ++ y )
                {
                    float[] data = myTiles[ x, y ].GetRawVertexData();
                    count += data.Length;
                    verts.Add( data );
                }

            myLightCount = 0;
            
            for( int x = 0; x < Width; ++ x )
                for( int y = 0; y < Height; ++ y )
                    if ( myTiles[ x, y ].IsVisible )
                    {
                        ++myLightCount;
                        if ( myTiles[ x, y ].UseHalfLighting )
                            ++myLightCount;
                    }

            float[] vertArr = new float[ count ];

            for ( int i = 0, s = 0; s < count; s += verts[ i++ ].Length )
                Array.Copy( verts[ i ], 0, vertArr, s, verts[ i ].Length );

            return vertArr;
        }

        protected virtual float[] GetRawLightData()
        {
            float[] data = new float[ myLightCount * 20 ];

            int i = 0;

            for( int x = 0; x < Width; ++ x )
                for ( int y = 0; y < Height; ++y )
                    if ( myTiles[ x, y ].IsVisible )
                    {

                        float rx = ( X + x ) * 16.0f;
                        float ry = ( Y + y ) * 16.0f;

                        float yInc = 16.0f;

                        if ( SmoothLighting )
                        {
                            if ( myTiles[ x, y ].UseHalfLighting )
                            {
                                data[ i++ ] = rx;
                                data[ i++ ] = ry + 8.0f;
                                data[ i++ ] = myLighting[ x, y + 1 ].R;
                                data[ i++ ] = myLighting[ x, y + 1 ].G;
                                data[ i++ ] = myLighting[ x, y + 1 ].B;

                                data[ i++ ] = rx + 16.0f;
                                data[ i++ ] = ry + 8.0f;
                                data[ i++ ] = myLighting[ x + 1, y + 1 ].R;
                                data[ i++ ] = myLighting[ x + 1, y + 1 ].G;
                                data[ i++ ] = myLighting[ x + 1, y + 1 ].B;

                                data[ i++ ] = rx + 16.0f;
                                data[ i++ ] = ry + 16.0f;
                                data[ i++ ] = myLighting[ x + 1, y + 1 ].R;
                                data[ i++ ] = myLighting[ x + 1, y + 1 ].G;
                                data[ i++ ] = myLighting[ x + 1, y + 1 ].B;

                                data[ i++ ] = rx;
                                data[ i++ ] = ry + 16.0f;
                                data[ i++ ] = myLighting[ x, y + 1 ].R;
                                data[ i++ ] = myLighting[ x, y + 1 ].G;
                                data[ i++ ] = myLighting[ x, y + 1 ].B;

                                yInc = 8.0f;
                            }

                            data[ i++ ] = rx;
                            data[ i++ ] = ry;
                            data[ i++ ] = myLighting[ x, y ].R;
                            data[ i++ ] = myLighting[ x, y ].G;
                            data[ i++ ] = myLighting[ x, y ].B;

                            data[ i++ ] = rx + 16.0f;
                            data[ i++ ] = ry;
                            data[ i++ ] = myLighting[ x + 1, y ].R;
                            data[ i++ ] = myLighting[ x + 1, y ].G;
                            data[ i++ ] = myLighting[ x + 1, y ].B;

                            data[ i++ ] = rx + 16.0f;
                            data[ i++ ] = ry + yInc;
                            data[ i++ ] = myLighting[ x + 1, y + 1 ].R;
                            data[ i++ ] = myLighting[ x + 1, y + 1 ].G;
                            data[ i++ ] = myLighting[ x + 1, y + 1 ].B;

                            data[ i++ ] = rx;
                            data[ i++ ] = ry + yInc;
                            data[ i++ ] = myLighting[ x, y + 1 ].R;
                            data[ i++ ] = myLighting[ x, y + 1 ].G;
                            data[ i++ ] = myLighting[ x, y + 1 ].B;
                        }
                        else
                        {
                            LightColour light = GetLight( x + 0.5, y + 0.5 );

                            float r = light.R;
                            float g = light.G;
                            float b = light.B;

                            data[ i++ ] = rx;
                            data[ i++ ] = ry;
                            data[ i++ ] = r;
                            data[ i++ ] = g;
                            data[ i++ ] = b;

                            data[ i++ ] = rx + 16.0f;
                            data[ i++ ] = ry;
                            data[ i++ ] = r;
                            data[ i++ ] = g;
                            data[ i++ ] = b;

                            data[ i++ ] = rx + 16.0f;
                            data[ i++ ] = ry + 16.0f;
                            data[ i++ ] = r;
                            data[ i++ ] = g;
                            data[ i++ ] = b;

                            data[ i++ ] = rx;
                            data[ i++ ] = ry + 16.0f;
                            data[ i++ ] = r;
                            data[ i++ ] = g;
                            data[ i++ ] = b;
                        }
                    }

            return data;
        }

        public void Save( Stream stream, bool saveEntities = true )
        {
            MemoryStream mstr = new MemoryStream();
            BinaryWriter writer = new BinaryWriter( mstr );

            writer.Write( X );
            writer.Write( Y );
            writer.Write( Width );
            writer.Write( Height );
            OnSave( writer, saveEntities );

            stream.Write( BitConverter.GetBytes( (UInt32) mstr.Length ), 0, 4 );
            stream.Write( mstr.ToArray(), 0, (int) mstr.Length );

            writer.Close();
            mstr.Close();
        }

        protected virtual void OnSave( BinaryWriter writer, bool saveEntities = true )
        {
            for ( int x = 0; x < Width; ++x )
                for ( int y = 0; y < Height; ++y )
                    myTiles[ x, y ].Save( writer );

            writer.Write( saveEntities ? myEnts.Count : 0 );

            if( saveEntities )
                foreach ( Entity ent in myEnts )
                    ent.Save( writer, false );
        }

        protected virtual void OnLoad( BinaryReader reader )
        {
            for ( int x = 0; x < Width; ++x )
                for ( int y = 0; y < Height; ++y )
                    myTiles[ x, y ].Load( reader );

            int count = reader.ReadInt32();

            for ( int i = 0; i < count; ++i )
            {
                Entity ent = Entity.Load( reader, false );

                myEnts.Add( ent );
            }
        }

        protected virtual void SendTileData()
        {
            myVB.SetTileData( GetRawTileData() );
            myTilesChanged = false;
        }

        protected virtual void SendLightingData()
        {
            myVB.SetLightData( GetRawLightData() );
            myLightingChanged = false;
        }

        protected virtual void SortEntities()
        {
            myEnts = myEnts.OrderByDescending( x => x.OriginY ).ToList();
            myEntSortingChanged = false;
        }

        public virtual void Think( double deltaTime )
        {
            for ( int i = myEnts.Count - 1; i >= 0; --i )
                myEnts[ i ].Think( deltaTime );
        }

        public virtual void CheckEntityLocations()
        {
            for ( int i = myEnts.Count - 1; i >= 0; --i )
            {
                Entity ent = myEnts[ i ];

                if ( ent.IsRemoved )
                    Map.RemoveEntity( ent );
                else if ( ent.OriginX < X || ent.OriginX >= X + Width ||
                    ent.OriginY < Y || ent.OriginY >= Y + Height )
                    Map.RelocateEntity( ent );
            }
        }

        public virtual void RenderTiles()
        {
            if ( myTilesChanged )
                SendTileData();

            MapRenderer.DrawTiles( this );
        }

        public virtual void RenderLighting()
        {
            if ( myLightingChanged && !myTilesChanged )
                SendLightingData();

            MapRenderer.DrawLighting( this );
        }

        public virtual void RenderEntities( bool editor = false )
        {
            if ( myEntSortingChanged )
                SortEntities();

            if ( !editor )
                for ( int i = myEnts.Count - 1; i >= 0; -- i )
                    myEnts[ i ].Render();
            else
            {
                List<Entity> selected = new List<Entity>();

                for ( int i = myEnts.Count - 1; i >= 0; --i )
                {
                    Entity ent = myEnts[ i ];
                    if ( !ent.Selected )
                        ent.EditorRender();
                    else
                        selected.Add( ent );
                }

                foreach ( Entity ent in selected )
                    ent.EditorRender();
            }
        }

        public virtual void Dispose()
        {
            myVB.Dispose();
        }
    }
}
