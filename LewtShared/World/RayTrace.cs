using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

namespace Lewt.Shared.World
{
    struct TileCrossInfo
    {
        public int X;
        public int Y;
        public GameTile Tile;
        public double Duration;
        public bool IsSolid
        {
            get
            {
                return Tile.IsSolid;
            }
        }

        public TileCrossInfo( Map map, int x, int y, double duration = 0 )
        {
            X = x;
            Y = y;
            try
            {
                Tile = map.GetTile( x, y );
            }
            catch
            {
                Tile = InteriorTile.Default;
            }

            Duration = duration;
        }
    }

    struct RayTraceResult
    {
        public Vector2d Origin;
        public Vector2d Target;
        public Vector2d End;

        public double Ratio;

        public bool HitSolid;
        public List<TileCrossInfo> CrossedTiles;
        public TileCrossInfo LastTile
        {
            get
            {
                return CrossedTiles.Last();
            }
        }
        public Vector2d TilePos
        {
            get
            {
                return new Vector2d( LastTile.X, LastTile.Y );
            }
        }
        public byte HitDir;

        public Vector2d Travel
        {
            get
            {
                return End - Origin;
            }
        }
    }

    static class RayTrace
    {
        public static RayTraceResult Trace( Map map, Vector2d origin, Vector2d target, bool stopOnSolid = true )
        {
            RayTraceResult result = new RayTraceResult
            {
                Origin = origin,
                Target = target,
                HitSolid = false,
                CrossedTiles = new List<TileCrossInfo>()
            };

            Vector2d diff = target - origin;

            int startBlockX = (int) Math.Floor( origin.X );
            int startBlockY = (int) Math.Floor( origin.Y );


            result.CrossedTiles.Add( new TileCrossInfo( map, startBlockX, startBlockY ) );

            double maxLen = diff.Length;

            Vector2d inc = new Vector2d( diff.X > 0 ? 1 : diff.X < 0 ? -1 : 0, diff.Y > 0 ? 1 : diff.Y < 0 ? -1 : 0 );

            if ( inc.X == 0 && inc.Y == 0 || ( stopOnSolid && result.LastTile.IsSolid ) )
            {
                result.End = origin;
                result.HitSolid = result.LastTile.IsSolid;
                result.Ratio = 0;
                return result;
            }

            Vector2d absDiff = new Vector2d( Math.Abs( diff.X ), Math.Abs( diff.Y ) );

            Vector2d deltaX = inc.X != 0 ? new Vector2d( inc.X, diff.Y / absDiff.X ) : new Vector2d( 0, Double.PositiveInfinity );
            Vector2d deltaY = inc.Y != 0 ? new Vector2d( diff.X / absDiff.Y, inc.Y ) : new Vector2d( Double.PositiveInfinity, 0 );

            double lenX = deltaX.Length;
            double lenY = deltaY.Length;

            Vector2d iniPosComp = new Vector2d(
                inc.X > 0 ? Math.Floor( origin.X ) + 1 : inc.X < 0 ? Math.Floor( origin.X ) : origin.X,
                inc.Y > 0 ? Math.Floor( origin.Y ) + 1 : inc.Y < 0 ? Math.Floor( origin.Y ) : origin.Y );

            Vector2d iniPosDistComp = new Vector2d( Math.Abs( iniPosComp.X - origin.X ), Math.Abs( iniPosComp.Y - origin.Y ) );

            Vector2d curPosX =
                origin + deltaX * iniPosDistComp.X;
            Vector2d curPosY =
                origin + deltaY * iniPosDistComp.Y;

            Vector2d curDistComp = new Vector2d( lenX * iniPosDistComp.X, lenY * iniPosDistComp.Y );

            byte curDir = 0;

            Vector2d curPos;
            double curDist;

            if ( inc.X != 0 && ( inc.Y == 0 || curDistComp.X < curDistComp.Y ) )
            {
                curPos = curPosX;
                curDist = curDistComp.X;
                curDir = 0;
            }
            else
            {
                curPos = curPosY;
                curDist = curDistComp.Y;
                curDir = 1;
            }

            result.CrossedTiles.RemoveAt( 0 );
            result.CrossedTiles.Add( new TileCrossInfo( map, startBlockX, startBlockY, curDist ) );

            bool both = false;

            while ( curDist < maxLen )
            {
                int tileX = (int) Math.Floor( curPos.X ) - ( ( curDir == 0 || both ) && inc.X < 0 ? 1 : 0 );
                int tileY = (int) Math.Floor( curPos.Y ) - ( ( curDir == 1 || both ) && inc.Y < 0 ? 1 : 0 );

                if ( stopOnSolid && result.LastTile.IsSolid )
                {
                    result.HitDir = curDir;
                    break;
                }

                both = false;

                double oldDist = curDist;

                if ( inc.X != 0 && ( inc.Y == 0 || curDistComp.X < curDistComp.Y ) )
                {
                    curPos = curPosX;
                    curPosX += deltaX;
                    curDist = curDistComp.X;
                    curDistComp.X += lenX;
                    curDir = 0;
                }
                else if ( inc.X == 0 || curDistComp.X > curDistComp.Y )
                {
                    curPos = curPosY;
                    curPosY += deltaY;
                    curDist = curDistComp.Y;
                    curDistComp.Y += lenY;
                    curDir = 1;
                }
                else
                {
                    curPos = curPosX;
                    curPosX += deltaX;
                    curPosY += deltaY;
                    curDist = curDistComp.X;
                    curDistComp.X += lenX;
                    curDistComp.Y += lenY;
                    curDir = 0;
                    both = true;
                }

                double dur = Math.Min( curDist, maxLen ) - oldDist;

                result.CrossedTiles.Add( new TileCrossInfo( map, tileX, tileY, dur ) );
            }

            if ( curDist > maxLen )
            {
                curPos = target;
                curDist = maxLen;
            }

            result.End = curPos;
            result.Ratio = curDist / maxLen;
            result.HitSolid = ( result.Ratio < 1 );

            return result;
        }

        public static RayTraceResult Trace( Map map, Vector2d origin, Vector2d direction, double distance, bool stopOnSolid = true )
        {
            direction.Normalize();

            return Trace( map, origin, origin + direction * distance, stopOnSolid );
        }

        public static RayTraceResult Trace( Map map, Vector2d origin, double rotY, double distance, bool stopOnSolid = true )
        {
            Vector2d vec = new Vector2d( Math.Sin( rotY ), -Math.Cos( rotY ) );

            return Trace( map, origin, origin + vec * distance, stopOnSolid );
        }
    }
}
