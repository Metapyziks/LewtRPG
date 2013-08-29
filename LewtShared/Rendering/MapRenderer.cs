using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using Lewt.Shared.World;

using ResourceLib;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Lewt.Shared.Rendering
{
    public static class MapRenderer
    {
        private static bool myVersionChecked = false;

        public static bool AllowCustomShaders = false;

        public static bool GL3
        {
            get
            {
                if ( !myVersionChecked )
                    CheckGLVersion();

                return myGL3;
            }
        }

        public static bool NVidiaCard
        {
            get
            {
                if ( !myVersionChecked )
                    CheckGLVersion();

                return myNVidiaCard;
            }
        }

        public static float CameraScale
        {
            get
            {
                return myCameraScale;
            }
            set
            {
                if ( value != myCameraScale )
                {
                    myCameraScale = value;
                    myShouldUpdateTilePosition = true;
                    myShouldUpdateOverworldPosition = true;
                    myShouldUpdateLightPosition = true;

                    FindViewportLimits();
                }
            }
        }

        public static float CameraX
        {
            get
            {
                return myCameraX;
            }
            set
            {
                float val = (int) ( value * 16.0f + 0.5f ) / 16.0f;

                if ( val != myCameraX )
                {
                    myCameraX = val;
                    myShouldUpdateTilePosition = true;
                    myShouldUpdateOverworldPosition = true;
                    myShouldUpdateLightPosition = true;

                    FindViewportLimits();
                }
            }
        }
        public static float CameraY
        {
            get
            {
                return myCameraY;
            }
            set
            {
                float val = (int) ( value * 16.0f + 0.5f ) / 16.0f;

                if ( val != myCameraY )
                {
                    myCameraY = val;
                    myShouldUpdateTilePosition = true;
                    myShouldUpdateOverworldPosition = true;
                    myShouldUpdateLightPosition = true;

                    FindViewportLimits();
                }
            }
        }

        public static int ScreenWidth
        {
            get
            {
                return myScreenWidth;
            }
        }
        public static int ScreenHeight
        {
            get
            {
                return myScreenHeight;
            }
        }

        public static float ViewportLeft
        {
            get
            {
                return myVPLeft;
            }
        }

        public static float ViewportTop
        {
            get
            {
                return myVPTop;
            }
        }

        public static float ViewportRight
        {
            get
            {
                return myVPRight;
            }
        }

        public static float ViewportBottom
        {
            get
            {
                return myVPBottom;
            }
        }

        public static bool CullHiddenFaces = true;

        private static bool myGL3 = false;
        private static bool myNVidiaCard = false;

        private static int myScreenWidth;
        private static int myScreenHeight;

        private static float myCameraScale = 1.0f;
        private static float myCameraX = 0.0f;
        private static float myCameraY = 0.0f;

        private static float myVPLeft;
        private static float myVPTop;
        private static float myVPRight;
        private static float myVPBottom;

        private static Texture myInteriorTex;
        private static Texture myTerrainTex;
        private static Texture myTransMaskTex;
        private static Texture myWorldMapTex;

        private static int myTileProgram;
        private static int myTileTextureLocation;
        private static int myTilePositionLocation;
        private static int myTileScaleLocation;

        private static int myOverworldProgram;
        private static int myOverworldTextureLocation;
        private static int myOverworldMaskLocation;
        private static int myOverworldPositionLocation;
        private static int myOverworldScaleLocation;

        private static int myLightProgram;
        private static int myLightPositionLocation;
        private static int myLightScaleLocation;

        private static bool myShouldUpdateTilePosition;
        private static bool myShouldUpdateOverworldPosition;
        private static bool myShouldUpdateLightPosition;

        public static int TileShaderProgram
        {
            get
            {
                if ( myTileProgram == 0 )
                    myTileProgram = GL.CreateProgram();

                return myTileProgram;
            }
        }

        public static int OverworldShaderProgram
        {
            get
            {
                if ( myOverworldProgram == 0 )
                    myOverworldProgram = GL.CreateProgram();

                return myOverworldProgram;
            }
        }

        public static int LightShaderProgram
        {
            get
            {
                if ( myLightProgram == 0 )
                    myLightProgram = GL.CreateProgram();

                return myLightProgram;
            }
        }
        
        public static void SetUp( int width, int height )
        {
            myScreenWidth = width;
            myScreenHeight = height;

            SetUpTileShader( width, height );
            SetUpOverworldShader( width, height );
            SetUpLightShader( width, height );

            GL.Enable( EnableCap.Blend );
            GL.BlendFunc( BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha );
            GL.Enable( EnableCap.PolygonSmooth );

            GL.UseProgram( TileShaderProgram );
            
            myShouldUpdateTilePosition = true;
            myShouldUpdateLightPosition = true;

            FindViewportLimits();
        }

        private static void CheckGLVersion()
        {
            String str = GL.GetString( StringName.Version );
            Debug.WriteLine( "OpenGL Version: " + str );
            myGL3 = str.StartsWith( "3." ) || str.StartsWith( "4." );

            str = GL.GetString( StringName.Vendor );
            myNVidiaCard = str.ToUpper().StartsWith( "NVIDIA" );

            myVersionChecked = true;
        }

        private static void SetUpTileShader( float width, float height )
        {
            int vert = GL.CreateShader( ShaderType.VertexShader );
            int frag = GL.CreateShader( ShaderType.FragmentShader );
            ECheck( "shader creation" );

            if ( AllowCustomShaders )
            {
                String shadDir = "CustomShaders";
                String shadPref = shadDir + Path.DirectorySeparatorChar + "tile.";

                if ( !Directory.Exists( shadDir ) )
                    Directory.CreateDirectory( shadDir );

                if ( GL3 )
                {
                    if ( File.Exists( shadPref + "gl3.vert" ) )
                        GL.ShaderSource( vert, File.ReadAllText( shadPref + "gl3.vert" ) );
                    else
                    {
                        GL.ShaderSource( vert, GL3ShaderSource.TileRendering.Vertex );
                        File.WriteAllText( shadPref + "gl3.vert", GL3ShaderSource.TileRendering.Vertex );
                    }

                    if ( File.Exists( shadPref + "gl3.frag" ) )
                        GL.ShaderSource( frag, File.ReadAllText( shadPref + "gl3.frag" ) );
                    else
                    {
                        GL.ShaderSource( frag, GL3ShaderSource.TileRendering.Fragment );
                        File.WriteAllText( shadPref + "gl3.frag", GL3ShaderSource.TileRendering.Fragment );
                    }
                }
                else
                {
                    if ( File.Exists( shadPref + "gl2.vert" ) )
                        GL.ShaderSource( vert, File.ReadAllText( shadPref + "gl2.vert" ) );
                    else
                    {
                        GL.ShaderSource( vert, GL2ShaderSource.TileRendering.Vertex );
                        File.WriteAllText( shadPref + "gl2.vert", GL2ShaderSource.TileRendering.Vertex );
                    }

                    if ( File.Exists( shadPref + "gl2.frag" ) )
                        GL.ShaderSource( frag, File.ReadAllText( shadPref + "gl2.frag" ) );
                    else
                    {
                        GL.ShaderSource( frag, GL2ShaderSource.TileRendering.Fragment );
                        File.WriteAllText( shadPref + "gl2.frag", GL2ShaderSource.TileRendering.Fragment );
                    }
                }
            }
            else
            {
                if ( MapRenderer.GL3 )
                {
                    GL.ShaderSource( vert, GL3ShaderSource.TileRendering.Vertex );
                    GL.ShaderSource( frag, GL3ShaderSource.TileRendering.Fragment );
                }
                else
                {
                    GL.ShaderSource( vert, GL2ShaderSource.TileRendering.Vertex );
                    GL.ShaderSource( frag, GL2ShaderSource.TileRendering.Fragment );
                }
            }
            ECheck( "shader source" );

            GL.CompileShader( vert );
            ECheck( "vert shader compilation" );
            GL.CompileShader( frag );
            ECheck( "frag shader compilation" );

            Debug.WriteLine( GL.GetShaderInfoLog( vert ) );
            Debug.WriteLine( GL.GetShaderInfoLog( frag ) );

            GL.AttachShader( TileShaderProgram, vert );
            GL.AttachShader( TileShaderProgram, frag );
            ECheck( "shader attachment" );

            GL.LinkProgram( TileShaderProgram );
            GL.UseProgram( TileShaderProgram );
            ECheck( "shader linking" );

            Debug.WriteLine( GL.GetProgramInfoLog( TileShaderProgram ) );
            if( GL3 )
                GL.BindFragDataLocation( TileShaderProgram, 0, "out_frag_colour" );
            ECheck( "bind frag data location" );

            GL.Uniform2( GL.GetUniformLocation( TileShaderProgram, "screen_resolution" ), (float) width, (float) height );
            ECheck( "setting resolution" );
            
            myTilePositionLocation = GL.GetUniformLocation( TileShaderProgram, "camera_position" );
            myTileScaleLocation = GL.GetUniformLocation( TileShaderProgram, "camera_scale" );
            ECheck( "finding uniform locations" );

            myInteriorTex = Res.Get<Texture>( "images_terrain" );
            myWorldMapTex = Res.Get<Texture>( "images_worldmap" );
            ECheck( "loading textures" );

            myTileTextureLocation = GL.GetUniformLocation( TileShaderProgram, "texture0" );
            GL.Uniform1( myTileTextureLocation, 0 );
            ECheck( "setting texture loc" );
        }

        private static void SetUpOverworldShader( float width, float height )
        {
            int vert = GL.CreateShader( ShaderType.VertexShader );
            int frag = GL.CreateShader( ShaderType.FragmentShader );
            ECheck( "shader creation" );

            if ( AllowCustomShaders )
            {
                String shadDir = "CustomShaders";
                String shadPref = shadDir + Path.DirectorySeparatorChar + "overworld.";

                if ( !Directory.Exists( shadDir ) )
                    Directory.CreateDirectory( shadDir );

                if ( GL3 )
                {
                    if ( File.Exists( shadPref + "gl3.vert" ) )
                        GL.ShaderSource( vert, File.ReadAllText( shadPref + "gl3.vert" ) );
                    else
                    {
                        GL.ShaderSource( vert, GL3ShaderSource.OverworldTileRendering.Vertex );
                        File.WriteAllText( shadPref + "gl3.vert", GL3ShaderSource.TileRendering.Vertex );
                    }

                    if ( File.Exists( shadPref + "gl3.frag" ) )
                        GL.ShaderSource( frag, File.ReadAllText( shadPref + "gl3.frag" ) );
                    else
                    {
                        GL.ShaderSource( frag, GL3ShaderSource.OverworldTileRendering.Fragment );
                        File.WriteAllText( shadPref + "gl3.frag", GL3ShaderSource.TileRendering.Fragment );
                    }
                }
                else
                {
                    if ( File.Exists( shadPref + "gl2.vert" ) )
                        GL.ShaderSource( vert, File.ReadAllText( shadPref + "gl2.vert" ) );
                    else
                    {
                        GL.ShaderSource( vert, GL2ShaderSource.OverworldTileRendering.Vertex );
                        File.WriteAllText( shadPref + "gl2.vert", GL2ShaderSource.TileRendering.Vertex );
                    }

                    if ( File.Exists( shadPref + "gl2.frag" ) )
                        GL.ShaderSource( frag, File.ReadAllText( shadPref + "gl2.frag" ) );
                    else
                    {
                        GL.ShaderSource( frag, GL2ShaderSource.OverworldTileRendering.Fragment );
                        File.WriteAllText( shadPref + "gl2.frag", GL2ShaderSource.TileRendering.Fragment );
                    }
                }
            }
            else
            {
                if ( MapRenderer.GL3 )
                {
                    GL.ShaderSource( vert, GL3ShaderSource.OverworldTileRendering.Vertex );
                    GL.ShaderSource( frag, GL3ShaderSource.OverworldTileRendering.Fragment );
                }
                else
                {
                    GL.ShaderSource( vert, GL2ShaderSource.OverworldTileRendering.Vertex );
                    GL.ShaderSource( frag, GL2ShaderSource.OverworldTileRendering.Fragment );
                }
            }
            ECheck( "shader source" );

            GL.CompileShader( vert );
            ECheck( "vert shader compilation" );
            GL.CompileShader( frag );
            ECheck( "frag shader compilation" );

            Debug.WriteLine( GL.GetShaderInfoLog( vert ) );
            Debug.WriteLine( GL.GetShaderInfoLog( frag ) );

            GL.AttachShader( OverworldShaderProgram, vert );
            GL.AttachShader( OverworldShaderProgram, frag );
            ECheck( "shader attachment" );

            GL.LinkProgram( OverworldShaderProgram );
            GL.UseProgram( OverworldShaderProgram );
            ECheck( "shader linking" );

            Debug.WriteLine( GL.GetProgramInfoLog( OverworldShaderProgram ) );
            if ( GL3 )
                GL.BindFragDataLocation( OverworldShaderProgram, 0, "out_frag_colour" );
            ECheck( "bind frag data location" );

            GL.Uniform2( GL.GetUniformLocation( OverworldShaderProgram, "screen_resolution" ), (float) width, (float) height );
            ECheck( "setting resolution" );

            myOverworldPositionLocation = GL.GetUniformLocation( OverworldShaderProgram, "camera_position" );
            myOverworldScaleLocation = GL.GetUniformLocation( OverworldShaderProgram, "camera_scale" );
            ECheck( "finding uniform locations" );

            myTerrainTex = Res.Get<Texture>( "images_worldtiles" );
            myTransMaskTex = Res.Get<Texture>( "images_transmask" );
            ECheck( "loading textures" );

            myOverworldTextureLocation = GL.GetUniformLocation( OverworldShaderProgram, "texture0" );
            GL.Uniform1( myOverworldTextureLocation, 0 );

            myOverworldMaskLocation = GL.GetUniformLocation( OverworldShaderProgram, "texture1" );
            GL.Uniform1( myOverworldMaskLocation, 1 );
            ECheck( "setting texture loc" );
        }

        private static void SetUpLightShader( float width, float height )
        {
            int vert = GL.CreateShader( ShaderType.VertexShader );
            int frag = GL.CreateShader( ShaderType.FragmentShader );
            ECheck( "shader creation" );

            if ( AllowCustomShaders )
            {
                String shadDir = "CustomShaders";
                String shadPref = shadDir + Path.DirectorySeparatorChar + "light.";

                if ( !Directory.Exists( shadDir ) )
                    Directory.CreateDirectory( shadDir );

                if ( GL3 )
                {
                    if ( File.Exists( shadPref + "gl3.vert" ) )
                        GL.ShaderSource( vert, File.ReadAllText( shadPref + "gl3.vert" ) );
                    else
                    {
                        GL.ShaderSource( vert, GL3ShaderSource.LightRendering.Vertex );
                        File.WriteAllText( shadPref + "gl3.vert", GL3ShaderSource.LightRendering.Vertex );
                    }

                    if ( File.Exists( shadPref + "gl3.frag" ) )
                        GL.ShaderSource( frag, File.ReadAllText( shadPref + "gl3.frag" ) );
                    else
                    {
                        GL.ShaderSource( frag, GL3ShaderSource.LightRendering.Fragment );
                        File.WriteAllText( shadPref + "gl3.frag", GL3ShaderSource.LightRendering.Fragment );
                    }
                }
                else
                {
                    if ( File.Exists( shadPref + "gl2.vert" ) )
                        GL.ShaderSource( vert, File.ReadAllText( shadPref + "gl2.vert" ) );
                    else
                    {
                        GL.ShaderSource( vert, GL2ShaderSource.LightRendering.Vertex );
                        File.WriteAllText( shadPref + "gl2.vert", GL2ShaderSource.LightRendering.Vertex );
                    }

                    if ( File.Exists( shadPref + "gl2.frag" ) )
                        GL.ShaderSource( frag, File.ReadAllText( shadPref + "gl2.frag" ) );
                    else
                    {
                        GL.ShaderSource( frag, GL2ShaderSource.LightRendering.Fragment );
                        File.WriteAllText( shadPref + "gl2.frag", GL2ShaderSource.LightRendering.Fragment );
                    }
                }
            }
            else
            {
                if ( MapRenderer.GL3 )
                {
                    GL.ShaderSource( vert, GL3ShaderSource.LightRendering.Vertex );
                    GL.ShaderSource( frag, GL3ShaderSource.LightRendering.Fragment );
                }
                else
                {
                    GL.ShaderSource( vert, GL2ShaderSource.LightRendering.Vertex );
                    GL.ShaderSource( frag, GL2ShaderSource.LightRendering.Fragment );
                }
            }
            ECheck( "shader source" );

            GL.CompileShader( vert );
            ECheck( "vert shader compilation" );
            GL.CompileShader( frag );
            ECheck( "frag shader compilation" );

            Debug.WriteLine( GL.GetShaderInfoLog( vert ) );
            Debug.WriteLine( GL.GetShaderInfoLog( frag ) );

            GL.AttachShader( LightShaderProgram, vert );
            GL.AttachShader( LightShaderProgram, frag );
            ECheck( "shader attachment" );

            GL.LinkProgram( LightShaderProgram );
            GL.UseProgram( LightShaderProgram );
            ECheck( "shader linking" );

            Debug.WriteLine( GL.GetProgramInfoLog( LightShaderProgram ) );
            if( GL3 )
                GL.BindFragDataLocation( LightShaderProgram, 0, "out_frag_colour" );
            ECheck( "bind frag data location" );
            
            GL.Uniform2( GL.GetUniformLocation( LightShaderProgram, "screen_resolution" ), (float) width, (float) height );
            myLightPositionLocation = GL.GetUniformLocation( LightShaderProgram, "camera_position" );
            myLightScaleLocation = GL.GetUniformLocation( LightShaderProgram, "camera_scale" );
            ECheck( "finding uniform locations" );
        }

        private static void FindViewportLimits()
        {
            myVPLeft = -ScreenWidth / ( 32.0f * CameraScale ) + CameraX;
            myVPTop = -ScreenHeight / ( 32.0f * CameraScale ) + CameraY;
            myVPRight = myVPLeft + ScreenWidth / ( 16.0f * CameraScale );
            myVPBottom = myVPTop + ScreenHeight / ( 16.0f * CameraScale );
        }

        public static bool ProgramExists()
        {
            return GL.IsProgram( TileShaderProgram );
        }

        public static void DrawTiles( Chunk chunk )
        {
            bool exterior = chunk.Map.IsExterior;

            GL.UseProgram( exterior ? MapRenderer.OverworldShaderProgram : MapRenderer.TileShaderProgram );

            if ( exterior && myShouldUpdateOverworldPosition )
            {
                GL.Uniform2( myOverworldPositionLocation, CameraX, CameraY );
                GL.Uniform1( myOverworldScaleLocation, CameraScale );

                myShouldUpdateOverworldPosition = false;
            }
            else if ( !exterior && myShouldUpdateTilePosition )
            {
                GL.Uniform2( myTilePositionLocation, CameraX, CameraY );
                GL.Uniform1( myTileScaleLocation, CameraScale );

                myShouldUpdateTilePosition = false;
            }

            if ( !exterior )
            {
                GL.ActiveTexture( TextureUnit.Texture0 );
                myInteriorTex.Bind();
            }
            else
            {
                GL.ActiveTexture( TextureUnit.Texture0 );
                myTerrainTex.Bind();
                GL.ActiveTexture( TextureUnit.Texture1 );
                myTransMaskTex.Bind();
            }

            chunk.VertexBuffer.RenderTiles();

            ECheck();
        }

        public static void DrawTiles( OverworldMap map )
        {
            GL.UseProgram( MapRenderer.TileShaderProgram );

            if ( myShouldUpdateTilePosition )
            {
                GL.Uniform2( myTilePositionLocation, CameraX, CameraY );
                GL.Uniform1( myTileScaleLocation, CameraScale );

                myShouldUpdateTilePosition = false;
            }

            GL.ActiveTexture( TextureUnit.Texture0 );
            myWorldMapTex.Bind();

            map.VertexBuffer.RenderTiles();

            ECheck();
        }

        public static void DrawLighting( Chunk map )
        {
            GL.UseProgram( LightShaderProgram );

            if ( myShouldUpdateLightPosition )
            {
                GL.Uniform2( myLightPositionLocation, CameraX, CameraY );
                GL.Uniform1( myLightScaleLocation, CameraScale );

                myShouldUpdateLightPosition = false;
            }

            GL.BlendFunc( BlendingFactorSrc.DstColor, BlendingFactorDest.Zero );

            map.VertexBuffer.RenderLights();

            GL.BlendFunc( BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha );

            ECheck();
        }

        internal static void ECheck( String identifier = "unknown" )
        {
            ErrorCode error;
            if ( ( error = GL.GetError() ) != 0 )
                throw new Exception( error.ToString() + "\n   at " + identifier + "." );
        }
    }
}
