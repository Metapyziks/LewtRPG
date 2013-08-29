using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Lewt.Shared.World;

namespace ChunkEditor
{
    public partial class NewDungeonDialog : Form
    {
        public int ChunkWidth;
        public int ChunkHeight;

        public byte ChunkSkin;

        public DungeonClass[] MapTypes
        {
            get
            {
                return DungTypeLB.SelectedItems.Cast<DungeonClass>().ToArray();
            }
        }

        public NewDungeonDialog()
        {
            InitializeComponent();
        }
        
        private void CancelBTN_Click( object sender, EventArgs e )
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void CreateBTN_Click( object sender, EventArgs e )
        {
            if ( DungTypeLB.SelectedItems.Count == 0 )
            {
                MessageBox.Show( "Please select at least one dungeon style.", "New Dungeon Chunk", MessageBoxButtons.OK, MessageBoxIcon.Error );
                return;
            }

            ChunkWidth = (int) WidthNUD.Value;
            ChunkHeight = (int) HeightNUD.Value;

            ChunkSkin = ( (DungeonClass) DungTypeLB.SelectedItems[ 0 ] ).DefaultSkinIndex;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void NewDungeonDialog_Load( object sender, EventArgs e )
        {
            DungeonClass[] dungClasses = DungeonClass.GetAll();

            foreach ( DungeonClass c in dungClasses )
                DungTypeLB.Items.Add( c );
        }

        private void DungTypeLB_SelectedIndexChanged( object sender, EventArgs e )
        {
            CreateBTN.Enabled = DungTypeLB.SelectedItems.Count != 0;
        }
    }
}
