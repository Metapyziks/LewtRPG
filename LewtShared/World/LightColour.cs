using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK.Graphics;

namespace Lewt.Shared.World
{
    public struct LightColour
    {
        public static LightColour operator +( LightColour lightA, LightColour lightB )
        {
            return new LightColour( lightA.R + lightB.R, lightA.G + lightB.G, lightA.B + lightB.B );
        }

        public static LightColour operator |( LightColour lightA, LightColour lightB )
        {
            return new LightColour( (float) Math.Sqrt( lightA.R * lightA.R + lightB.R * lightB.R ),
                (float) Math.Sqrt( lightA.G * lightA.G + lightB.G * lightB.G ),
                (float) Math.Sqrt( lightA.B * lightA.B + lightB.B * lightB.B ) );
        }

        public static LightColour Default = new LightColour( 0.0f, 0.0f, 0.0f );

        public float R;
        public float G;
        public float B;

        public LightColour( float r, float g, float b )
        {
            R = r;
            G = g;
            B = b;
        }

        public Color4 ToColor4()
        {
            return new Color4( R, G, B, 1.0f );
        }
    }
}
