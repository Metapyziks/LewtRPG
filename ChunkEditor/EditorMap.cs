using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lewt.Shared.World;

namespace ChunkEditor
{
    public class EditorMap : Map
    {
        public Chunk Chunk;

        public int Width
        {
            get
            {
                return Chunk.Width;
            }
        }

        public int Height
        {
            get
            {
                return Chunk.Height;
            }
        }

        public EditorMap( bool interior, int width, int height, byte defaultSkin )
            : base( interior )
        {
            AlwaysPlaceEntities = true;

            Chunk = new Chunk( 0, 0, width, height, this );
            Chunks.Add( Chunk );

            Chunk.PostWorldInitialize( true );

            Chunk.FillTiles( true, defaultSkin );
        }

        public EditorMap( bool interior, ChunkTemplate template )
            : base( interior )
        {
            AlwaysPlaceEntities = true;

            Chunk = new Chunk( 0, 0, template, this );
            Chunks.Add( Chunk );

            Chunk.PostWorldInitialize( true );
        }
    }
}
