using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ChunkEditor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main( String[] args )
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            if( args.Length == 0 )
                Application.Run( new ChunkEditorForm() );
            else
                Application.Run( new ChunkEditorForm( args[ 0 ] ) );
        }
    }
}
