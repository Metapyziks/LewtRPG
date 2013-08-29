using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Lewt.Shared.Rendering
{
    public static class SpriteRenderer
    {
        private static int myProgram;
        private static int myTextureLocation;
        private static int myPosLocation;
        private static int myTexLocation;
        private static int myClrLocation;

        private static bool myStarted;

        public static bool HasStarted
        {
            get
            {
                return myStarted;
            }
        }

        public static bool AllowCustomShaders = false;

        public static int ShaderProgram
        {
            get
            {
                if ( myProgram == 0 )
                    myProgram = GL.CreateProgram();

                return myProgram;
            }
        }

        private static Sprite myRectSprite;

        public static void SetUp( int width, int height )
        {
            int vert = GL.CreateShader( ShaderType.VertexShader );
            int frag = GL.CreateShader( ShaderType.FragmentShader );
            ECheck( "shader creation" );

            if ( AllowCustomShaders )
            {
                String shadDir = "CustomShaders";
                String shadPref = shadDir + Path.DirectorySeparatorChar + "sprite.";

                if ( !Directory.Exists( shadDir ) )
                    Directory.CreateDirectory( shadDir );

                if ( MapRenderer.GL3 )
                {
                    if ( File.Exists( shadPref + "gl3.vert" ) )
                        GL.ShaderSource( vert, File.ReadAllText( shadPref + "gl3.vert" ) );
                    else
                    {
                        GL.ShaderSource( vert, GL3ShaderSource.SpriteRendering.Vertex );
                        File.WriteAllText( shadPref + "gl3.vert", GL3ShaderSource.SpriteRendering.Vertex );
                    }

                    if ( File.Exists( shadPref + "gl3.frag" ) )
                        GL.ShaderSource( frag, File.ReadAllText( shadPref + "gl3.frag" ) );
                    else
                    {
                        GL.ShaderSource( frag, GL3ShaderSource.SpriteRendering.Fragment );
                        File.WriteAllText( shadPref + "gl3.frag", GL3ShaderSource.SpriteRendering.Fragment );
                    }
                }
                else
                {
                    if ( File.Exists( shadPref + "gl2.vert" ) )
                        GL.ShaderSource( vert, File.ReadAllText( shadPref + "gl2.vert" ) );
                    else
                    {
                        GL.ShaderSource( vert, GL2ShaderSource.SpriteRendering.Vertex );
                        File.WriteAllText( shadPref + "gl2.vert", GL2ShaderSource.SpriteRendering.Vertex );
                    }

                    if ( File.Exists( shadPref + "gl2.frag" ) )
                        GL.ShaderSource( frag, File.ReadAllText( shadPref + "gl2.frag" ) );
                    else
                    {
                        GL.ShaderSource( frag, GL2ShaderSource.SpriteRendering.Fragment );
                        File.WriteAllText( shadPref + "gl2.frag", GL2ShaderSource.SpriteRendering.Fragment );
                    }
                }
            }
            else
            {
                if ( MapRenderer.GL3 )
                {
                    GL.ShaderSource( vert, GL3ShaderSource.SpriteRendering.Vertex );
                    GL.ShaderSource( frag, GL3ShaderSource.SpriteRendering.Fragment );
                }
                else
                {
                    GL.ShaderSource( vert, GL2ShaderSource.SpriteRendering.Vertex );
                    GL.ShaderSource( frag, GL2ShaderSource.SpriteRendering.Fragment );
                }
            }
            ECheck( "shader source" );

            GL.CompileShader( vert );
            ECheck( "vert shader compilation" );
            GL.CompileShader( frag );
            ECheck( "frag shader compilation" );

            Debug.WriteLine( GL.GetShaderInfoLog( vert ) );
            Debug.WriteLine( GL.GetShaderInfoLog( frag ) );

            GL.AttachShader( ShaderProgram, vert );
            GL.AttachShader( ShaderProgram, frag );
            ECheck( "shader attachment" );

            GL.LinkProgram( ShaderProgram );
            GL.UseProgram( ShaderProgram );
            ECheck( "shader linking" );

            Debug.WriteLine( GL.GetProgramInfoLog( ShaderProgram ) );
            if( MapRenderer.GL3 )
                GL.BindFragDataLocation( ShaderProgram, 0, "out_frag_colour" );
            ECheck( "bind frag data location" );

            GL.Uniform2( GL.GetUniformLocation( ShaderProgram, "screen_resolution" ), (float) width, (float) height );
            ECheck( "setting resolution" );

            myPosLocation = GL.GetAttribLocation( ShaderProgram, "in_position" );
            myTexLocation = GL.GetAttribLocation( ShaderProgram, "in_texture" );
            myClrLocation = GL.GetAttribLocation( ShaderProgram, "in_colour" );

            myTextureLocation = GL.GetUniformLocation( ShaderProgram, "texture0" );
            ECheck( "finding attribute locations" );
            GL.Uniform1( myTextureLocation, 0 );
            ECheck( "setting texture loc" );

            ECheck( "end" );
        }

        public static void Begin()
        {
            GL.UseProgram( ShaderProgram );
            GL.ActiveTexture( TextureUnit.Texture0 );

            int stride = 8 * sizeof( float );
            GL.VertexAttribPointer( myPosLocation,
                2, VertexAttribPointerType.Float, false, stride, 0 );
            GL.VertexAttribPointer( myTexLocation,
                2, VertexAttribPointerType.Float, false, stride, 2 * sizeof( float ) );
            GL.VertexAttribPointer( myClrLocation,
                4, VertexAttribPointerType.Float, false, stride, 4 * sizeof( float ) );

            GL.Begin( BeginMode.Quads );
            
            myStarted = true;
        }

        public static void End()
        {
            GL.End();

            myStarted = false;
        }

        public static void Render( Sprite sprite )
        {
            if ( !myStarted )
                throw new Exception( "Must call SpriteRenderer.Begin() first!" );

            if ( !sprite.Texture.Ready || Texture.Current != sprite.Texture.ID )
            {
                GL.End();
                ECheck();
                sprite.Texture.Bind();
                GL.Begin( BeginMode.Quads );
            }

            float[] verts = sprite.Vertices;

            if ( MapRenderer.NVidiaCard )
            {
                for ( int i = 0; i < verts.Length; i += 8 )
                {
                    GL.VertexAttrib2( myTexLocation, verts[ i + 2 ], verts[ i + 3 ] );
                    GL.VertexAttrib4( myClrLocation, verts[ i + 4 ], verts[ i + 5 ], verts[ i + 6 ], verts[ i + 7 ] );
                    GL.VertexAttrib2( myPosLocation, verts[ i ], verts[ i + 1 ] );
                }
            }
            else
            {
                for ( int i = 0; i < verts.Length; i += 8 )
                {
                    GL.VertexAttrib2( myPosLocation, verts[ i ], verts[ i + 1 ] );
                    GL.VertexAttrib2( myTexLocation, verts[ i + 2 ], verts[ i + 3 ] );
                    GL.VertexAttrib4( myClrLocation, verts[ i + 4 ], verts[ i + 5 ], verts[ i + 6 ], verts[ i + 7 ] );
                }
            }
        }

        public static void DrawRect( float x, float y, float width, float height, Color4 colour )
        {
            if ( myRectSprite == null )
                myRectSprite = new Sprite( width, height, colour );
            else
            {
                myRectSprite.Size = new Vector2( width, height );
                myRectSprite.Colour = colour;
            }

            myRectSprite.Position = new Vector2( x, y );

            myRectSprite.Render();
        }

        internal static void ECheck( String identifier = "unknown" )
        {
            ErrorCode error;
            if ( ( error = GL.GetError() ) != 0 )
                throw new Exception( error.ToString() + "\n   at " + identifier + ".\nGL Version: " + GL.GetString( StringName.Version ) );
        }
    }
}
