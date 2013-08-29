using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using OpenTK;

namespace Lewt.Shared.Entities
{
    public struct BoundingBox
    {
        private double myOffsetX;
        private double myOffsetY;

        private double myWidth;
        private double myHeight;

        public double OriginX;
        public double OriginY;

        public double Left
        {
            get
            {
                return OriginX + myOffsetX;
            }
            set
            {
                OriginX = value - myOffsetX;
            }
        }
        public double Top
        {
            get
            {
                return OriginY + myOffsetY;
            }
            set
            {
                OriginY = value - myOffsetY;
            }
        }
        public double Right
        {
            get
            {
                return OriginX + myOffsetX + myWidth;
            }
            set
            {
                OriginX = value - myOffsetX - myWidth;
            }
        }
        public double Bottom
        {
            get
            {
                return OriginY + myOffsetY + myHeight;
            }
            set
            {
                OriginY = value - myOffsetY - myHeight;
            }
        }

        public double Width
        {
            get
            {
                return myWidth;
            }
        }
        public double Height
        {
            get
            {
                return myHeight;
            }
        }

        public BoundingBox( double width, double height )
            : this( -width / 2, -height / 2, width, height )
        {
        }

        public BoundingBox( double offsetX, double offsetY, double width, double height )
        {
            myOffsetX = offsetX;
            myOffsetY = offsetY;
            myWidth = width;
            myHeight = height;
            OriginX = 0;
            OriginY = 0;
        }

        public BoundingBox( BinaryReader reader )
        {
            myOffsetX = reader.ReadDouble();
            myOffsetY = reader.ReadDouble();
            myWidth = reader.ReadDouble();
            myHeight = reader.ReadDouble();
            OriginX = reader.ReadDouble();
            OriginY = reader.ReadDouble();
        }

        public bool IsIntersecting( Vector2d pos )
        {
            return
                pos.X >= Left &&
                pos.X <= Right &&
                pos.Y >= Top &&
                pos.Y <= Bottom;
        }

        public bool IsIntersecting( BoundingBox other )
        {
            return
                Left < other.Right &&
                Right > other.Left &&
                Top < other.Bottom &&
                Bottom > other.Top;
        }

        public void Translate( double x, double y )
        {
            OriginX += x;
            OriginY += y;
        }

        public void WriteToStream( BinaryWriter writer )
        {
            writer.Write( myOffsetX );
            writer.Write( myOffsetY );
            writer.Write( myWidth );
            writer.Write( myHeight );
            writer.Write( OriginX );
            writer.Write( OriginY );
        }
    }
}
