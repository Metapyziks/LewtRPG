using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Lewt.Shared.World
{
    public class Tile
    {
        private readonly int myX;
        private readonly int myY;

        public int X
        {
            get
            {
                return myX;
            }
        }
        public int Y
        {
            get
            {
                return myY;
            }
        }

        public virtual bool IsSolid
        {
            get
            {
                return false;
            }
        }

        public Tile()
            : this( 0, 0 )
        {

        }

        public Tile( int x, int y )
        {
            myX = x;
            myY = y;
        }

        public virtual void PostWorldInitialize()
        {

        }

        public virtual float[] GetRawVertexData()
        {
            return new float[ 0 ];
        }

        public virtual void Save( BinaryWriter writer )
        {

        }

        public virtual void Load( BinaryReader reader )
        {

        }
    }
}
