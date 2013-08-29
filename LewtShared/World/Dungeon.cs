using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ResourceLib;

using Lewt.Shared.Rendering;
using Lewt.Shared.Entities;
using OpenTK;

namespace Lewt.Shared.World
{
    public class Dungeon : Map
    {
        private struct GenEndPoint
        {
            public int X;
            public int Y;
            public int Size;
            public ConnectorFace Face;
            public Chunk Chunk;
            public int Skin;
            public int Depth;

            public GenEndPoint( Chunk chunk, ChunkConnector connector, int depth )
            {
                X = chunk.X + connector.X;
                Y = chunk.Y + connector.Y;
                Size = connector.Size;
                Face = (ConnectorFace )( ( connector.Horizontal ? 0 : 1 ) + ( connector.BottomOrRight ? 2 : 0 ) );
                Chunk = chunk;
                Skin = connector.Skin;
                Depth = depth;
            }
        }

        private struct GenChunkRect
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Width;
            public readonly int Height;

            public int Left
            {
                get
                {
                    return X;
                }
            }

            public int Right
            {
                get
                {
                    return X + Width;
                }
            }

            public int Top
            {
                get
                {
                    return Y;
                }
            }

            public int Bottom
            {
                get
                {
                    return Y + Height;
                }
            }

            public GenChunkRect( Chunk chunk )
                : this( chunk.X, chunk.Y, chunk.Width, chunk.Height )
            {

            }

            public GenChunkRect( int x, int y, ChunkTemplate template )
                : this( x, y, template.Width, template.Height )
            {

            }

            public GenChunkRect( int x, int y, int width, int height )
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public bool Intersects( GenChunkRect rect )
            {
                return
                    Left < rect.Right &&
                    Right > rect.Left &&
                    Top < rect.Bottom &&
                    Bottom > rect.Top;
            }
        }

        public enum GenChunkAddedStatus
        {
            NotAdded = 0,
            Added = 1,
            ImpossibleToAdd = 2
        }

        private bool myHasGenerated;
        public readonly Sprite IconSprite;

        public readonly DungeonClass DungeonClass;

        public readonly int X;
        public readonly int Y;

        public bool HasGenerated
        {
            get
            {
                return myHasGenerated;
            }
        }

        public Dungeon( UInt16 id, DungeonClass type, int x = 0, int y = 0, bool isServer = false )
            : base( true, id, isServer )
        {
            DungeonClass = type;

            X = x;
            Y = y;

            myHasGenerated = false;

            if ( !isServer )
            {
                int iconIndex = DungeonClass.IconIndexes[ id % DungeonClass.IconIndexes.Length ];
                int iconX = iconIndex % ( DungeonClass.IconTexture.Width / 16 );
                int iconY = iconIndex / ( DungeonClass.IconTexture.Width / 16 );

                IconSprite = new Sprite( DungeonClass.IconTexture )
                {
                    SubrectSize = new Vector2( 16.0f, 16.0f ),
                    SubrectOffset = new Vector2( iconX, iconY ) * 16.0f
                };
            }
        }

        public void Generate( uint seed = 0 )
        {
            Random rand = seed == 0 ? new Random() : new Random( (int) seed );

            int area = 0;
            int areaGoal = rand.Next( DungeonClass.AreaMin, DungeonClass.AreaMax );
            int maxDepth = (int) Math.Pow( areaGoal, 1 / 3 ) + 16;

            ChunkTemplate[] templates = DungeonClass.ChunkTemplates;
            Stack<GenEndPoint> endPoints = new Stack<GenEndPoint>();
            List<GenEndPoint> looseEnds = new List<GenEndPoint>();
            List<GenChunkRect> rects = new List<GenChunkRect>();

            GenAddChunk( rand, endPoints, looseEnds, rects, templates, ref area, 0, true );

            int tries = 0;
            while ( endPoints.Count > 0 )
            {
                if ( tries < 64 && area < areaGoal )
                {
                    switch ( GenAddChunk( rand, endPoints, looseEnds, rects, templates, ref area, maxDepth ) )
                    {
                        case GenChunkAddedStatus.Added:
                            tries = 0; break;
                        case GenChunkAddedStatus.NotAdded:
                            ++tries; break;
                        case GenChunkAddedStatus.ImpossibleToAdd:
                            tries = 64; break;
                    }
                }
                else
                {
                    looseEnds.Add( endPoints.Pop() );
                    tries = 0;
                }
            }

            for( int i = looseEnds.Count - 1; i >= 0; -- i )
            {
                GenEndPoint end = looseEnds[ i ];
                bool horizontal = end.Face == ConnectorFace.Left || end.Face == ConnectorFace.Right;

                bool matchFound = false;

                for ( int j = i - 1; j >= 0; --j )
                {
                    GenEndPoint match = looseEnds[ j ];

                    if ( end.Size == match.Size && end.Skin == match.Skin &&
                        ( (int) match.Face ^ (int) end.Face ) == 0x2 &&
                        ( ( horizontal && end.Y == match.Y && ( end.X + 1 == match.X || end.X == match.X + 1 ) ) ||
                          ( !horizontal && end.X == match.X && ( end.Y + 1 == match.Y || end.Y == match.Y + 1 ) ) ) )
                    {
                        looseEnds.RemoveAt( i-- );
                        looseEnds.RemoveAt( j );

                        matchFound = true;
                        break;
                    }
                }

                if( !matchFound )
                    for ( int j = 0; j < end.Size; ++j )
                        end.Chunk.GetTile(
                            end.X + ( !horizontal ? j : 0 ),
                            end.Y + ( horizontal ? j : 0 ) ).IsWall = true;
            }

            PostWorldInitialize();

            myHasGenerated = true;
        }
        
        private GenChunkAddedStatus GenAddChunk( Random rand, Stack<GenEndPoint> endPoints, List<GenEndPoint> looseEnds, List<GenChunkRect> rects, ChunkTemplate[] templates, ref int area, int maxDepth = 0, bool first = false )
        {
            ChunkTemplate template;

            Chunk newChunk;
            List<GenEndPoint> endsToAdd;

            if ( first )
            {
                template = templates[ rand.Next( templates.Length ) ];
                newChunk = new Chunk( 0, 0, template, this );
                Chunks.Add( newChunk );
                endsToAdd = new List<GenEndPoint>();
                for ( int i = 0; i < 4; ++i )
                    foreach ( ChunkConnector connector in template.GetConnectors( (ConnectorFace) i ) )
                        endsToAdd.Add( new GenEndPoint( newChunk, connector, 0 ) );

                while( endsToAdd.Count > 0 )
                {
                    int index = rand.Next( endsToAdd.Count );
                    endPoints.Push( endsToAdd[ index ] );
                    endsToAdd.RemoveAt( index );
                }

                rects.Add( new GenChunkRect( newChunk ) );
                area += newChunk.Area;

                return GenChunkAddedStatus.Added;
            }

            List<ChunkTemplate> validTemplates = new List<ChunkTemplate>();

            GenEndPoint endPoint = endPoints.Pop();

            ConnectorFace oppositeFace = (ConnectorFace) ( ( (int) endPoint.Face + 2 ) % 4 );

            foreach ( ChunkTemplate temp in templates )
                if ( temp.GetConnectors( oppositeFace, endPoint.Size, endPoint.Skin ).Length != 0 )
                    validTemplates.Add( temp );

            if ( validTemplates.Count == 0 )
            {
                endPoints.Push( endPoint );
                return GenChunkAddedStatus.ImpossibleToAdd;
            }

            template = validTemplates[ rand.Next( validTemplates.Count ) ];

            ChunkConnector[] cons = template.GetConnectors( oppositeFace, endPoint.Size, endPoint.Skin );

            if ( cons.Length == 0 )
            {
                endPoints.Push( endPoint );
                return GenChunkAddedStatus.NotAdded;
            }

            ChunkConnector con = cons[ rand.Next( cons.Length ) ];

            int x = endPoint.X - con.X;
            int y = endPoint.Y - con.Y;

            switch ( endPoint.Face )
            {
                case ConnectorFace.Left:
                    --x; break;
                case ConnectorFace.Top:
                    --y; break;
                case ConnectorFace.Right:
                    ++x; break;
                case ConnectorFace.Bottom:
                    ++y; break;
            }

            GenChunkRect newRect = new GenChunkRect( x, y, template );

            foreach ( GenChunkRect rect in rects )
                if ( rect.Intersects( newRect ) )
                {
                    endPoints.Push( endPoint );
                    return GenChunkAddedStatus.NotAdded;
                }

            newChunk = new Chunk( x, y, template, this );
            Chunks.Add( newChunk );
            endsToAdd = new List<GenEndPoint>();
            for ( int i = 0; i < 4; ++i )
                foreach ( ChunkConnector connector in template.GetConnectors( (ConnectorFace) i ) )
                {
                    if ( connector.X != con.X || connector.Y != con.Y )
                    {
                        GenEndPoint end = new GenEndPoint( newChunk, connector, endPoint.Depth + 1 );
                        if ( maxDepth != 0 && endPoint.Depth >= maxDepth )
                            looseEnds.Add( end );
                        else
                            endsToAdd.Add( end );
                    }
                }

            while ( endsToAdd.Count > 0 )
            {
                int index = rand.Next( endsToAdd.Count );
                endPoints.Push( endsToAdd[ index ] );
                endsToAdd.RemoveAt( index );
            }

            rects.Add( newRect );
            area += newChunk.Area;

            return GenChunkAddedStatus.Added;
        }
    }
}
