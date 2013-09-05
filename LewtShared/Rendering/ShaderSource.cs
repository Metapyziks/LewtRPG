using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lewt.Shared.Rendering
{
    internal struct GL2ShaderSource
    {
        public struct SpriteRendering
        {
            public const String Vertex = @"
#version 120

precision highp float;

uniform vec2 screen_resolution;

attribute vec2 in_position;
attribute vec2 in_texture;
attribute vec4 in_colour;

varying vec2 var_texture;
varying vec4 var_colour;

void main( void )
{
    var_texture = in_texture;
    var_colour = in_colour;

    vec2 pos = in_position - screen_resolution / 2.0;
    pos.x /= screen_resolution.x / 2.0;
    pos.y /= -screen_resolution.y / 2.0;

    gl_Position = vec4( pos, 0.0, 1.0 );
}
            ";

            public const String Fragment = @"
#version 120

precision highp float;

uniform sampler2D texture0;
            
varying vec2 var_texture;
varying vec4 var_colour;

void main( void )
{
    vec4 clr = texture2D( texture0, var_texture ) * var_colour;

    if( clr.a != 0.0 )
        gl_FragColor = vec4( clr.rgba );
    else
        discard;
}
            ";
        }

        public struct TileRendering
        {
            public const String Vertex = @"
#version 120

precision highp float;

uniform vec2 camera_position;
uniform vec2 screen_resolution;
            
uniform float camera_scale = 1.0;

attribute vec2 in_position;
attribute vec2 in_tex;

varying vec2 var_tex;

void main( void )
{
    var_tex = in_tex;

    vec2 pos = in_position - camera_position * 16.0;
    pos.x /= screen_resolution.x / ( 2.0 * camera_scale );
    pos.y /= -screen_resolution.y / ( 2.0 * camera_scale );

    gl_Position = vec4( pos, 0.0, 1.0 );
}
            ";

            public const String Fragment = @"
#version 120

precision highp float;

uniform sampler2D texture0;
            
varying vec2 var_tex;

void main( void )
{
    vec4 clr = texture2D( texture0, var_tex );
                
    if( clr.a != 0.0 )
        gl_FragColor = vec4( clr.rgb, 1.0 );
    else
        discard;
}";
        }

        public struct OverworldTileRendering
        {
            public const String Vertex = @"
#version 120

precision highp float;

uniform vec2 camera_position;
uniform vec2 screen_resolution;
            
uniform float camera_scale = 1.0;

attribute vec2 in_position;
attribute vec2 in_tex;

attribute float in_trans_type;
attribute vec2 in_trans_pos;

varying vec2 var_tex;

varying int var_trans_type;
varying vec2 var_trans_pos;

void main( void )
{
    var_tex = in_tex;

    var_trans_type = int( in_trans_type );
    var_trans_pos = in_trans_pos;

    vec2 pos = in_position - camera_position * 16.0;
    pos.x /= screen_resolution.x / ( 2.0 * camera_scale );
    pos.y /= -screen_resolution.y / ( 2.0 * camera_scale );

    gl_Position = vec4( pos, 0.0, 1.0 );
}
            ";

            public const String Fragment = @"
#version 120

precision highp float;

uniform sampler2D texture0;
uniform sampler2D texture1;
            
varying vec2 var_tex;

varying int var_trans_type;
varying vec2 var_trans_pos;

void main( void )
{
    if( var_trans_type != 0.0 )
    {
        vec4 maskClr = texture2D( texture1, var_trans_pos );
        int mask = int( maskClr.b * 255.0 );
        
        if( ( mask & var_trans_type ) == 0 )
            discard;
    }

    vec4 clr = texture2D( texture0, var_tex );
                
    if( clr.a != 0.0 )
        gl_FragColor = vec4( clr.rgb, 1.0 );
    else
        discard;
}";
        }

        public struct LightRendering
        {
            public const String Vertex = @"
#version 120

precision highp float;

uniform vec2 camera_position;
uniform vec2 screen_resolution;
            
uniform float camera_scale = 1.0;

attribute vec2 in_position;
attribute vec3 in_colour;

varying vec3 var_colour;

void main( void )
{
    var_colour = in_colour;

    vec2 pos = in_position - camera_position * 16.0;
    pos.x /= screen_resolution.x / ( 2.0 * camera_scale );
    pos.y /= -screen_resolution.y / ( 2.0 * camera_scale );

    gl_Position = vec4( pos, 0.0, 1.0 );
}";

            public const String Fragment = @"
#version 120

precision highp float;

varying vec3 var_colour;

void main( void )
{
    gl_FragColor = vec4( var_colour.rgb, 1.0 );
}";
        }
    }

    internal struct GL3ShaderSource
    {
        public struct SpriteRendering
        {
            public const String Vertex = @"
#version 130

precision highp float;

uniform vec2 screen_resolution;

in vec2 in_position;
in vec2 in_texture;
in vec4 in_colour;

out vec2 var_texture;
out vec4 var_colour;

void main( void )
{
    var_texture = in_texture;
    var_colour = in_colour;

    vec2 pos = in_position - screen_resolution / 2.0;
    pos.x /= screen_resolution.x / 2.0;
    pos.y /= -screen_resolution.y / 2.0;

    gl_Position = vec4( pos, 0.0, 1.0 );
}";

            public const String Fragment = @"
#version 130

precision highp float;

uniform sampler2D texture0;
            
in vec2 var_texture;
in vec4 var_colour;

out vec4 out_frag_colour;

void main( void )
{
    vec4 clr = texture( texture0, var_texture ) * var_colour;

    if( clr.a != 0.0 )
        out_frag_colour = vec4( clr.rgba );
    else
        discard;
}";
        }

        public struct TileRendering
        {
            public const String Vertex = @"
#version 130

precision highp float;

uniform vec2 camera_position;
uniform vec2 screen_resolution;
            
uniform float camera_scale = 1.0;

in vec2 in_position;
in vec2 in_tex;

out vec2 var_tex;

void main( void )
{
    var_tex = in_tex;

    vec2 pos = in_position - camera_position * 16.0;
    pos.x /= screen_resolution.x / ( 2.0 * camera_scale );
    pos.y /= -screen_resolution.y / ( 2.0 * camera_scale );

    gl_Position = vec4( pos, 0.0, 1.0 );
}";

            public const String Fragment = @"
#version 130

precision highp float;

uniform sampler2D texture0;
            
in vec2 var_tex;

out vec4 out_frag_colour;

void main( void )
{
    vec4 clr = texture( texture0, var_tex );
                
    if( clr.a != 0.0 )
        out_frag_colour = vec4( clr.rgb, 1.0 );
    else
        discard;
}";
        }

        public struct OverworldTileRendering
        {
            public const String Vertex = @"
#version 130

precision highp float;

uniform vec2 camera_position;
uniform vec2 screen_resolution;
            
uniform float camera_scale = 1.0;

in vec2 in_position;
in vec2 in_tex;

in float in_trans_type;
in vec2 in_trans_pos;

out vec2 var_tex;

flat out int var_trans_type;
out vec2 var_trans_pos;

void main( void )
{
    var_tex = in_tex;

    var_trans_type = int( in_trans_type );
    var_trans_pos = in_trans_pos;

    vec2 pos = in_position - camera_position * 16.0;
    pos.x /= screen_resolution.x / ( 2.0 * camera_scale );
    pos.y /= -screen_resolution.y / ( 2.0 * camera_scale );

    gl_Position = vec4( pos, 0.0, 1.0 );
}
            ";

            public const String Fragment = @"
#version 130

precision highp float;

uniform sampler2D texture0;
uniform sampler2D texture1;
            
in vec2 var_tex;

flat in int var_trans_type;
in vec2 var_trans_pos;

out vec4 out_frag_colour;

void main( void )
{
    if( var_trans_type != 0 )
    {
        vec4 maskClr = texture( texture1, var_trans_pos );
        int mask = int( maskClr.b * 255.0 );
        
        if( ( mask & var_trans_type ) == 0 )
            discard;
    }

    vec4 clr = texture( texture0, var_tex );
                
    if( clr.a != 0.0 )
        out_frag_colour = vec4( clr.rgb, 1.0 );
    else
        discard;
}";
        }

        public struct LightRendering
        {
            public const String Vertex = @"
#version 130

precision highp float;

uniform vec2 camera_position;
uniform vec2 screen_resolution;
            
uniform float camera_scale = 1.0;

in vec2 in_position;
in vec3 in_colour;

out vec3 var_colour;

void main( void )
{
    var_colour = in_colour;

    vec2 pos = in_position - camera_position * 16.0;
    pos.x /= screen_resolution.x / ( 2.0 * camera_scale );
    pos.y /= -screen_resolution.y / ( 2.0 * camera_scale );

    gl_Position = vec4( pos, 0.0, 1.0 );
}";

            public const String Fragment = @"
#version 130

precision highp float;

in vec3 var_colour;

out vec4 out_frag_colour;

void main( void )
{
    out_frag_colour = vec4( var_colour.rgb, 1.0 );
}";
        }
    }
}
