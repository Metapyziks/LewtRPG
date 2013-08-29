using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Lewt.Shared.Rendering
{
    sealed class VertexBuffer
    {
        private static List<VertexBuffer> stToDispose = new List<VertexBuffer>();

        public readonly bool IsOverworld;

        private bool mySetUp = false;
        private bool myDataSet = false;
        private bool myLightDataSet = false;

        private int myVaoID;
        private int myTileVboID;
        private int myLightVboID;
        private int myTileLength;
        private int myLightLength;

        private int myTilePosLoc;
        private int myTileTexLoc;

        private int myOverworldMaskTypeLoc;
        private int myOverworldMaskPosLoc;

        private int myLightPosLoc;
        private int myLightColourLoc;

        private int VaoID
        {
            get
            {
                if ( myVaoID == 0 )
                    GL.GenVertexArrays( 1, out myVaoID );

                return myVaoID;
            }
        }

        private int TileVboID
        {
            get
            {
                if ( myTileVboID == 0 )
                    GL.GenBuffers( 1, out myTileVboID );

                return myTileVboID;
            }
        }

        private int LightVboID
        {
            get
            {
                if ( myLightVboID == 0 )
                    GL.GenBuffers( 1, out myLightVboID );

                return myLightVboID;
            }
        }

        public VertexBuffer( bool overworld = false )
        {
            IsOverworld = overworld;
        }

        public void SetTileData( float[] vertices )
        {
            if ( !mySetUp )
                SetUp();

            if( !IsOverworld )
                myTileLength = vertices.Length / 4;
            else
                myTileLength = vertices.Length / 7;

            GL.BindVertexArray( VaoID );
            GL.BindBuffer( BufferTarget.ArrayBuffer, TileVboID );

            GL.BufferData( BufferTarget.ArrayBuffer, new IntPtr( vertices.Length * sizeof( float ) ), vertices, BufferUsageHint.StaticDraw );

            GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
            GL.BindVertexArray( 0 );

            myDataSet = true;
        }

        public void SetLightData( float[] vertices )
        {
            if ( !mySetUp )
                SetUp();

            myLightLength = vertices.Length / 5;

            GL.BindVertexArray( VaoID );
            GL.BindBuffer( BufferTarget.ArrayBuffer, LightVboID );

            GL.BufferData( BufferTarget.ArrayBuffer, new IntPtr( vertices.Length * sizeof( float ) ), vertices, BufferUsageHint.DynamicDraw );

            GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
            GL.BindVertexArray( 0 );

            MapRenderer.ECheck();

            myLightDataSet = true;
        }

        public void SetUp()
        {
            while ( stToDispose.Count > 0 )
            {
                stToDispose.Last().Delete();
                stToDispose.Remove( stToDispose.Last() );
            }

            int program = ( !IsOverworld ? MapRenderer.TileShaderProgram : MapRenderer.OverworldShaderProgram );

            GL.UseProgram( program );

            GL.BindVertexArray( VaoID );
            GL.BindBuffer( BufferTarget.ArrayBuffer, TileVboID );

            myTilePosLoc = GL.GetAttribLocation( program, "in_position" );
            myTileTexLoc = GL.GetAttribLocation( program, "in_tex" );

            if ( IsOverworld )
            {
                myOverworldMaskTypeLoc = GL.GetAttribLocation( program, "in_trans_type" );
                myOverworldMaskPosLoc = GL.GetAttribLocation( program, "in_trans_pos" );
            }

            GL.UseProgram( MapRenderer.LightShaderProgram );

            GL.BindBuffer( BufferTarget.ArrayBuffer, LightVboID );

            myLightPosLoc = GL.GetAttribLocation( MapRenderer.LightShaderProgram, "in_position" );
            myLightColourLoc = GL.GetAttribLocation( MapRenderer.LightShaderProgram, "in_colour" );

            GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
            GL.BindVertexArray( 0 );

            MapRenderer.ECheck();

            mySetUp = true;
        }

        public void RenderTiles()
        {
            if ( myDataSet )
            {
                GL.BindVertexArray( VaoID );

                int stride = ( IsOverworld ? 7 : 4 ) * sizeof( float );
                GL.BindBuffer( BufferTarget.ArrayBuffer, TileVboID );
                GL.VertexAttribPointer( myTilePosLoc, 2, VertexAttribPointerType.Float, false, stride, 0 );
                GL.VertexAttribPointer( myTileTexLoc, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof( float ) );

                if ( IsOverworld )
                {
                    GL.VertexAttribPointer( myOverworldMaskTypeLoc, 1, VertexAttribPointerType.Float, false, stride, 4 * sizeof( float ) );
                    GL.VertexAttribPointer( myOverworldMaskPosLoc, 2, VertexAttribPointerType.Float, false, stride, 5 * sizeof( float ) );
                }

                GL.EnableVertexAttribArray( myTilePosLoc );
                GL.EnableVertexAttribArray( myTileTexLoc );

                if ( IsOverworld )
                {
                    GL.EnableVertexAttribArray( myOverworldMaskTypeLoc );
                    GL.EnableVertexAttribArray( myOverworldMaskPosLoc );
                }

                GL.DrawArrays( BeginMode.Quads, 0, myTileLength );
                GL.DisableVertexAttribArray( myTilePosLoc );
                GL.DisableVertexAttribArray( myTileTexLoc );

                if ( IsOverworld )
                {
                    GL.DisableVertexAttribArray( myOverworldMaskTypeLoc );
                    GL.DisableVertexAttribArray( myOverworldMaskPosLoc );
                }

                GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
                GL.BindVertexArray( 0 );
            }
        }

        public void RenderLights()
        {
            if ( myLightDataSet )
            {
                GL.BindVertexArray( VaoID );

                int stride = 5 * sizeof( float );
                GL.BindBuffer( BufferTarget.ArrayBuffer, LightVboID );
                GL.VertexAttribPointer( myLightPosLoc, 2, VertexAttribPointerType.Float, false, stride, 0 );
                GL.VertexAttribPointer( myLightColourLoc, 3, VertexAttribPointerType.Float, false, stride, 2 * sizeof( float ) );

                GL.EnableVertexAttribArray( myLightPosLoc );
                GL.EnableVertexAttribArray( myLightColourLoc );
                GL.DrawArrays( BeginMode.Quads, 0, myLightLength );
                GL.DisableVertexAttribArray( myLightPosLoc );
                GL.DisableVertexAttribArray( myLightColourLoc );

                GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
                GL.BindVertexArray( 0 );
            }
        }

        public void Dispose()
        {
            if ( myDataSet && !stToDispose.Contains( this ) )
                stToDispose.Add( this );

            myDataSet = false;
        }

        private void Delete()
        {
            GL.DeleteBuffers( 1, ref myTileVboID );
            GL.DeleteBuffers( 1, ref myLightVboID );
            GL.DeleteVertexArrays( 1, ref myVaoID );
        }
    }
}
