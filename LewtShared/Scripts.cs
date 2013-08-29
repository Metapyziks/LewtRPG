using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.CodeDom.Compiler;

using Microsoft.CSharp;

using ResourceLib;
using System.Diagnostics;

namespace Lewt.Shared
{
    public class RScriptManager : RManager
    {
        public RScriptManager()
            : base( typeof( ScriptFile ), 1, "cs" )
        {

        }

        public override ResourceItem[] LoadFromFile( string keyPrefix, string fileName, string fileExtension, FileStream stream )
        {
            StreamReader reader = new StreamReader( stream );
            String contents = reader.ReadToEnd();

            ScriptFile file = new ScriptFile( keyPrefix + fileName, contents );
            Scripts.Register( file );
            ResourceItem[] items = new ResourceItem[]
            {
                new ResourceItem( keyPrefix + fileName, file )
            };

            return items;
        }

        public override object LoadFromArchive( BinaryReader stream )
        {
            ScriptFile sf = new ScriptFile( stream );
            Scripts.Register( sf );
            return sf;
        }

        public override void SaveToArchive( BinaryWriter stream, object item )
        {
            ( item as ScriptFile ).WriteToStream( stream );
        }
    }

    public class ScriptFile
    {
        public readonly String Name;
        public readonly String Contents;

        public ScriptFile( String name, String contents )
        {
            Name = name;
            Contents = contents;
        }

        public ScriptFile( BinaryReader reader )
        {
            Name = reader.ReadString();
            Contents = reader.ReadString();
        }

        public void WriteToStream( BinaryWriter writer )
        {
            writer.Write( Name );
            writer.Write( Contents );
        }
    }

    public static class Scripts
    {
        public enum Destination
        {
            Shared = 0,
            Client = 1,
            Server = 2,
            All    = 3
        }

        private static List<ScriptFile> myScripts = new List<ScriptFile>();

        private static Assembly myCompiledAssembly;

        internal static void Register( ScriptFile file )
        {
            for ( int i = 0; i < myScripts.Count; ++i )
                if ( myScripts[ i ].Name == file.Name )
                {
                    myScripts[ i ] = file;
                    return;
                }

            myScripts.Add( file );
        }

        public static void Compile( Destination dest )
        {
            CompilerParameters compParams = new CompilerParameters();

            List<String> myAllowedAssemblies = new List<String>
            {
                "System.dll",
                Assembly.GetAssembly( typeof( System.Linq.Enumerable ) ).Location,
                Assembly.GetAssembly( typeof( OpenTK.Vector2 ) ).Location,
                Assembly.GetAssembly( typeof( ResourceItem ) ).Location,
                "Lewt.Shared.dll"
            };

            if ( ( dest & Destination.Client ) != 0 )
                myAllowedAssemblies.Add( "Lewt.Client.dll" );

            if ( ( dest & Destination.Server ) != 0 )
                myAllowedAssemblies.Add( "Lewt.Server.dll" );

            compParams.ReferencedAssemblies.AddRange( myAllowedAssemblies.ToArray() );

            Dictionary<string,string> providerOptions = new Dictionary<string, string>();
            providerOptions.Add( "CompilerVersion", "v4.0" );

            CodeDomProvider compiler = new CSharpCodeProvider( providerOptions );

            compParams.GenerateExecutable = false;
            compParams.GenerateInMemory = true;

            String[] sources = new String[ myScripts.Count ];

            for ( int i = 0; i < myScripts.Count; ++i )
                sources[ i ] = myScripts[ i ].Contents;

            CompilerResults results = compiler.CompileAssemblyFromSource( compParams, sources );

            if ( results.Errors.Count > 0 )
            {
                Debug.WriteLine( results.Errors.Count + " error" + ( results.Errors.Count != 1 ? "s" : "" ) + " while compiling Scripts!" );
                foreach ( CompilerError error in results.Errors )
                {                    
                    if ( error.FileName != "" )
                        Debug.WriteLine( ( error.IsWarning ? "Warning" : "Error" ) + " in '" + error.FileName + "', at line " + error.Line );
                    
                    Debug.WriteLine( error.ErrorText );
                }
                return;
            }

            myCompiledAssembly = results.CompiledAssembly;
        }

        public static void Initialise()
        {
            MethodInfo info;

            foreach ( Type t in myCompiledAssembly.GetTypes() )
                if ( ( info = t.GetMethod( "Initialise", BindingFlags.Static | BindingFlags.NonPublic ) ) != null ) 
                    info.Invoke( null, new object[ 0 ] );
        }

        public static Type GetType( String typeName )
        {
            if ( myCompiledAssembly == null )
                Compile( Destination.All );

            return myCompiledAssembly.GetType( typeName ) ??
                Assembly.GetExecutingAssembly().GetType( typeName );
        }

        public static Type[] GetTypes( Type baseType )
        {
            if ( myCompiledAssembly == null )
                Compile( Destination.All );

            Type[] types = myCompiledAssembly.GetTypes();

            List<Type> matchingTypes = new List<Type>();

            foreach ( Type t in types )
                if ( t.DoesExtend( baseType ) )
                    matchingTypes.Add( t );

            return matchingTypes.ToArray();
        }

        public static object CreateInstance( String typeName, params object[] args )
        {
            Type t = GetType( typeName );
            Type[] argTypes = new Type[ args.Length ];
            for ( int i =0; i < args.Length; ++i )
                argTypes[ i ] = args[ i ].GetType();

            return t.GetConstructor( argTypes ).Invoke( args );
        }
    }
}
