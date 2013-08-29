namespace ChunkEditor
{
    partial class ChunkEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.TilePropsGB = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.PlacementBoxRB = new System.Windows.Forms.RadioButton();
            this.PlacementFreeRB = new System.Windows.Forms.RadioButton();
            this.SkinCB = new System.Windows.Forms.CheckBox();
            this.SolidityCB = new System.Windows.Forms.CheckBox();
            this.VariationCB = new System.Windows.Forms.CheckBox();
            this.VariationGB = new System.Windows.Forms.GroupBox();
            this.DefRB = new System.Windows.Forms.RadioButton();
            this.RanRB = new System.Windows.Forms.RadioButton();
            this.VeraRB = new System.Windows.Forms.RadioButton();
            this.VercRB = new System.Windows.Forms.RadioButton();
            this.VerbRB = new System.Windows.Forms.RadioButton();
            this.SolidityGB = new System.Windows.Forms.GroupBox();
            this.FloorRB = new System.Windows.Forms.RadioButton();
            this.WallRB = new System.Windows.Forms.RadioButton();
            this.SkinGB = new System.Windows.Forms.GroupBox();
            this.SkinIDNUD = new System.Windows.Forms.NumericUpDown();
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dungeonToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lightingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.smoothLightingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ChunkViewGB = new System.Windows.Forms.GroupBox();
            this.ViewControl = new OpenTK.GLControl();
            this.EntityPlaceGB = new System.Windows.Forms.GroupBox();
            this.SnapToTileCB = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.EntityClassCB = new System.Windows.Forms.ComboBox();
            this.CurToolGB = new System.Windows.Forms.GroupBox();
            this.EntityPropsRB = new System.Windows.Forms.RadioButton();
            this.EntityPlaceRB = new System.Windows.Forms.RadioButton();
            this.TilePlaceRB = new System.Windows.Forms.RadioButton();
            this.EntityPropGB = new System.Windows.Forms.GroupBox();
            this.EntityPG = new System.Windows.Forms.PropertyGrid();
            this.EntityCM = new System.Windows.Forms.ContextMenuStrip( this.components );
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cloneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exteriorChunkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TilePropsGB.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.VariationGB.SuspendLayout();
            this.SolidityGB.SuspendLayout();
            this.SkinGB.SuspendLayout();
            ( (System.ComponentModel.ISupportInitialize) ( this.SkinIDNUD ) ).BeginInit();
            this.MenuStrip.SuspendLayout();
            this.ChunkViewGB.SuspendLayout();
            this.EntityPlaceGB.SuspendLayout();
            this.CurToolGB.SuspendLayout();
            this.EntityPropGB.SuspendLayout();
            this.EntityCM.SuspendLayout();
            this.SuspendLayout();
            // 
            // TilePropsGB
            // 
            this.TilePropsGB.Controls.Add( this.groupBox1 );
            this.TilePropsGB.Controls.Add( this.SkinCB );
            this.TilePropsGB.Controls.Add( this.SolidityCB );
            this.TilePropsGB.Controls.Add( this.VariationCB );
            this.TilePropsGB.Controls.Add( this.VariationGB );
            this.TilePropsGB.Controls.Add( this.SolidityGB );
            this.TilePropsGB.Controls.Add( this.SkinGB );
            this.TilePropsGB.Enabled = false;
            this.TilePropsGB.Location = new System.Drawing.Point( 12, 134 );
            this.TilePropsGB.Name = "TilePropsGB";
            this.TilePropsGB.Size = new System.Drawing.Size( 200, 430 );
            this.TilePropsGB.TabIndex = 2;
            this.TilePropsGB.TabStop = false;
            this.TilePropsGB.Text = "Tile Placement Properties";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add( this.PlacementBoxRB );
            this.groupBox1.Controls.Add( this.PlacementFreeRB );
            this.groupBox1.Location = new System.Drawing.Point( 6, 19 );
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size( 188, 42 );
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Placement Mode";
            // 
            // PlacementBoxRB
            // 
            this.PlacementBoxRB.AutoSize = true;
            this.PlacementBoxRB.Location = new System.Drawing.Point( 105, 19 );
            this.PlacementBoxRB.Name = "PlacementBoxRB";
            this.PlacementBoxRB.Size = new System.Drawing.Size( 43, 17 );
            this.PlacementBoxRB.TabIndex = 1;
            this.PlacementBoxRB.Text = "Box";
            this.PlacementBoxRB.UseVisualStyleBackColor = true;
            // 
            // PlacementFreeRB
            // 
            this.PlacementFreeRB.AutoSize = true;
            this.PlacementFreeRB.Checked = true;
            this.PlacementFreeRB.Location = new System.Drawing.Point( 6, 19 );
            this.PlacementFreeRB.Name = "PlacementFreeRB";
            this.PlacementFreeRB.Size = new System.Drawing.Size( 46, 17 );
            this.PlacementFreeRB.TabIndex = 0;
            this.PlacementFreeRB.TabStop = true;
            this.PlacementFreeRB.Text = "Free";
            this.PlacementFreeRB.UseVisualStyleBackColor = true;
            // 
            // SkinCB
            // 
            this.SkinCB.AutoSize = true;
            this.SkinCB.Location = new System.Drawing.Point( 135, 67 );
            this.SkinCB.Name = "SkinCB";
            this.SkinCB.Size = new System.Drawing.Size( 59, 17 );
            this.SkinCB.TabIndex = 1;
            this.SkinCB.Text = "Enable";
            this.SkinCB.UseVisualStyleBackColor = true;
            this.SkinCB.CheckedChanged += new System.EventHandler( this.SkinCB_CheckedChanged );
            // 
            // SolidityCB
            // 
            this.SolidityCB.AutoSize = true;
            this.SolidityCB.Checked = true;
            this.SolidityCB.CheckState = System.Windows.Forms.CheckState.Checked;
            this.SolidityCB.Location = new System.Drawing.Point( 135, 118 );
            this.SolidityCB.Name = "SolidityCB";
            this.SolidityCB.Size = new System.Drawing.Size( 59, 17 );
            this.SolidityCB.TabIndex = 2;
            this.SolidityCB.Text = "Enable";
            this.SolidityCB.UseVisualStyleBackColor = true;
            this.SolidityCB.CheckedChanged += new System.EventHandler( this.SolidityCB_CheckedChanged );
            // 
            // VariationCB
            // 
            this.VariationCB.AutoSize = true;
            this.VariationCB.Location = new System.Drawing.Point( 135, 190 );
            this.VariationCB.Name = "VariationCB";
            this.VariationCB.Size = new System.Drawing.Size( 59, 17 );
            this.VariationCB.TabIndex = 12;
            this.VariationCB.Text = "Enable";
            this.VariationCB.UseVisualStyleBackColor = true;
            this.VariationCB.CheckedChanged += new System.EventHandler( this.VariationCB_CheckedChanged );
            // 
            // VariationGB
            // 
            this.VariationGB.Controls.Add( this.DefRB );
            this.VariationGB.Controls.Add( this.RanRB );
            this.VariationGB.Controls.Add( this.VeraRB );
            this.VariationGB.Controls.Add( this.VercRB );
            this.VariationGB.Controls.Add( this.VerbRB );
            this.VariationGB.Enabled = false;
            this.VariationGB.Location = new System.Drawing.Point( 6, 190 );
            this.VariationGB.Name = "VariationGB";
            this.VariationGB.Size = new System.Drawing.Size( 123, 135 );
            this.VariationGB.TabIndex = 11;
            this.VariationGB.TabStop = false;
            this.VariationGB.Text = "Variation";
            // 
            // DefRB
            // 
            this.DefRB.AutoSize = true;
            this.DefRB.Checked = true;
            this.DefRB.Location = new System.Drawing.Point( 6, 19 );
            this.DefRB.Name = "DefRB";
            this.DefRB.Size = new System.Drawing.Size( 59, 17 );
            this.DefRB.TabIndex = 3;
            this.DefRB.TabStop = true;
            this.DefRB.Text = "Default";
            this.DefRB.UseVisualStyleBackColor = true;
            // 
            // RanRB
            // 
            this.RanRB.AutoSize = true;
            this.RanRB.Location = new System.Drawing.Point( 6, 42 );
            this.RanRB.Name = "RanRB";
            this.RanRB.Size = new System.Drawing.Size( 65, 17 );
            this.RanRB.TabIndex = 7;
            this.RanRB.Text = "Random";
            this.RanRB.UseVisualStyleBackColor = true;
            // 
            // VeraRB
            // 
            this.VeraRB.AutoSize = true;
            this.VeraRB.Location = new System.Drawing.Point( 6, 65 );
            this.VeraRB.Name = "VeraRB";
            this.VeraRB.Size = new System.Drawing.Size( 76, 17 );
            this.VeraRB.TabIndex = 4;
            this.VeraRB.Text = "Variation A";
            this.VeraRB.UseVisualStyleBackColor = true;
            // 
            // VercRB
            // 
            this.VercRB.AutoSize = true;
            this.VercRB.Location = new System.Drawing.Point( 6, 111 );
            this.VercRB.Name = "VercRB";
            this.VercRB.Size = new System.Drawing.Size( 76, 17 );
            this.VercRB.TabIndex = 6;
            this.VercRB.Text = "Variation C";
            this.VercRB.UseVisualStyleBackColor = true;
            // 
            // VerbRB
            // 
            this.VerbRB.AutoSize = true;
            this.VerbRB.Location = new System.Drawing.Point( 6, 88 );
            this.VerbRB.Name = "VerbRB";
            this.VerbRB.Size = new System.Drawing.Size( 76, 17 );
            this.VerbRB.TabIndex = 5;
            this.VerbRB.Text = "Variation B";
            this.VerbRB.UseVisualStyleBackColor = true;
            // 
            // SolidityGB
            // 
            this.SolidityGB.Controls.Add( this.FloorRB );
            this.SolidityGB.Controls.Add( this.WallRB );
            this.SolidityGB.Location = new System.Drawing.Point( 6, 118 );
            this.SolidityGB.Name = "SolidityGB";
            this.SolidityGB.Size = new System.Drawing.Size( 123, 66 );
            this.SolidityGB.TabIndex = 10;
            this.SolidityGB.TabStop = false;
            this.SolidityGB.Text = "Solidity";
            // 
            // FloorRB
            // 
            this.FloorRB.AutoSize = true;
            this.FloorRB.Location = new System.Drawing.Point( 6, 42 );
            this.FloorRB.Name = "FloorRB";
            this.FloorRB.Size = new System.Drawing.Size( 48, 17 );
            this.FloorRB.TabIndex = 1;
            this.FloorRB.Text = "Floor";
            this.FloorRB.UseVisualStyleBackColor = true;
            // 
            // WallRB
            // 
            this.WallRB.AutoSize = true;
            this.WallRB.Checked = true;
            this.WallRB.Location = new System.Drawing.Point( 6, 19 );
            this.WallRB.Name = "WallRB";
            this.WallRB.Size = new System.Drawing.Size( 46, 17 );
            this.WallRB.TabIndex = 0;
            this.WallRB.TabStop = true;
            this.WallRB.Text = "Wall";
            this.WallRB.UseVisualStyleBackColor = true;
            // 
            // SkinGB
            // 
            this.SkinGB.Controls.Add( this.SkinIDNUD );
            this.SkinGB.Enabled = false;
            this.SkinGB.Location = new System.Drawing.Point( 6, 67 );
            this.SkinGB.Name = "SkinGB";
            this.SkinGB.Size = new System.Drawing.Size( 123, 45 );
            this.SkinGB.TabIndex = 9;
            this.SkinGB.TabStop = false;
            this.SkinGB.Text = "Skin ID";
            // 
            // SkinIDNUD
            // 
            this.SkinIDNUD.Location = new System.Drawing.Point( 6, 19 );
            this.SkinIDNUD.Maximum = new decimal( new int[] {
            7,
            0,
            0,
            0} );
            this.SkinIDNUD.Name = "SkinIDNUD";
            this.SkinIDNUD.Size = new System.Drawing.Size( 111, 20 );
            this.SkinIDNUD.TabIndex = 0;
            // 
            // MenuStrip
            // 
            this.MenuStrip.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.editToolStripMenuItem} );
            this.MenuStrip.Location = new System.Drawing.Point( 0, 0 );
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Size = new System.Drawing.Size( 754, 24 );
            this.MenuStrip.TabIndex = 3;
            this.MenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem} );
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size( 37, 20 );
            this.fileToolStripMenuItem.Text = "File";
            this.fileToolStripMenuItem.Click += new System.EventHandler( this.fileToolStripMenuItem_Click );
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.dungeonToolStripMenuItem,
            this.exteriorChunkToolStripMenuItem} );
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
            this.newToolStripMenuItem.Text = "New";
            // 
            // dungeonToolStripMenuItem
            // 
            this.dungeonToolStripMenuItem.Name = "dungeonToolStripMenuItem";
            this.dungeonToolStripMenuItem.Size = new System.Drawing.Size( 161, 22 );
            this.dungeonToolStripMenuItem.Text = "Dungeon Chunk";
            this.dungeonToolStripMenuItem.Click += new System.EventHandler( this.dungeonToolStripMenuItem_Click );
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler( this.openToolStripMenuItem_Click );
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size( 149, 6 );
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler( this.saveToolStripMenuItem_Click );
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size( 152, 22 );
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler( this.saveAsToolStripMenuItem_Click );
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.lightingToolStripMenuItem,
            this.smoothLightingToolStripMenuItem} );
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size( 44, 20 );
            this.viewToolStripMenuItem.Text = "View";
            // 
            // lightingToolStripMenuItem
            // 
            this.lightingToolStripMenuItem.Name = "lightingToolStripMenuItem";
            this.lightingToolStripMenuItem.Size = new System.Drawing.Size( 163, 22 );
            this.lightingToolStripMenuItem.Text = "Lighting";
            this.lightingToolStripMenuItem.Click += new System.EventHandler( this.lightingToolStripMenuItem_Click );
            // 
            // smoothLightingToolStripMenuItem
            // 
            this.smoothLightingToolStripMenuItem.Checked = true;
            this.smoothLightingToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.smoothLightingToolStripMenuItem.Enabled = false;
            this.smoothLightingToolStripMenuItem.Name = "smoothLightingToolStripMenuItem";
            this.smoothLightingToolStripMenuItem.Size = new System.Drawing.Size( 163, 22 );
            this.smoothLightingToolStripMenuItem.Text = "Smooth Lighting";
            this.smoothLightingToolStripMenuItem.Click += new System.EventHandler( this.smoothLightingToolStripMenuItem_Click );
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size( 39, 20 );
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // ChunkViewGB
            // 
            this.ChunkViewGB.Controls.Add( this.ViewControl );
            this.ChunkViewGB.Enabled = false;
            this.ChunkViewGB.Location = new System.Drawing.Point( 218, 27 );
            this.ChunkViewGB.Name = "ChunkViewGB";
            this.ChunkViewGB.Size = new System.Drawing.Size( 524, 537 );
            this.ChunkViewGB.TabIndex = 4;
            this.ChunkViewGB.TabStop = false;
            this.ChunkViewGB.Text = "Chunk View";
            // 
            // ViewControl
            // 
            this.ViewControl.BackColor = System.Drawing.Color.Black;
            this.ViewControl.Location = new System.Drawing.Point( 6, 19 );
            this.ViewControl.Name = "ViewControl";
            this.ViewControl.Size = new System.Drawing.Size( 512, 512 );
            this.ViewControl.TabIndex = 2;
            this.ViewControl.VSync = false;
            this.ViewControl.Load += new System.EventHandler( this.ViewControl_Load );
            this.ViewControl.Paint += new System.Windows.Forms.PaintEventHandler( this.ViewControl_Paint );
            this.ViewControl.MouseDown += new System.Windows.Forms.MouseEventHandler( this.ViewControl_MouseDown );
            this.ViewControl.MouseMove += new System.Windows.Forms.MouseEventHandler( this.ViewControl_MouseMove );
            this.ViewControl.MouseUp += new System.Windows.Forms.MouseEventHandler( this.ViewControl_MouseUp );
            // 
            // EntityPlaceGB
            // 
            this.EntityPlaceGB.Controls.Add( this.SnapToTileCB );
            this.EntityPlaceGB.Controls.Add( this.label1 );
            this.EntityPlaceGB.Controls.Add( this.EntityClassCB );
            this.EntityPlaceGB.Enabled = false;
            this.EntityPlaceGB.Location = new System.Drawing.Point( 12, 134 );
            this.EntityPlaceGB.Name = "EntityPlaceGB";
            this.EntityPlaceGB.Size = new System.Drawing.Size( 200, 430 );
            this.EntityPlaceGB.TabIndex = 13;
            this.EntityPlaceGB.TabStop = false;
            this.EntityPlaceGB.Text = "Entity Placement";
            this.EntityPlaceGB.Visible = false;
            // 
            // SnapToTileCB
            // 
            this.SnapToTileCB.AutoSize = true;
            this.SnapToTileCB.Checked = true;
            this.SnapToTileCB.CheckState = System.Windows.Forms.CheckState.Checked;
            this.SnapToTileCB.Location = new System.Drawing.Point( 115, 64 );
            this.SnapToTileCB.Name = "SnapToTileCB";
            this.SnapToTileCB.Size = new System.Drawing.Size( 79, 17 );
            this.SnapToTileCB.TabIndex = 2;
            this.SnapToTileCB.Text = "Snap to tile";
            this.SnapToTileCB.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point( 6, 16 );
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size( 61, 13 );
            this.label1.TabIndex = 1;
            this.label1.Text = "Entity Class";
            // 
            // EntityClassCB
            // 
            this.EntityClassCB.FormattingEnabled = true;
            this.EntityClassCB.Location = new System.Drawing.Point( 6, 32 );
            this.EntityClassCB.Name = "EntityClassCB";
            this.EntityClassCB.Size = new System.Drawing.Size( 188, 21 );
            this.EntityClassCB.TabIndex = 0;
            // 
            // CurToolGB
            // 
            this.CurToolGB.Controls.Add( this.EntityPropsRB );
            this.CurToolGB.Controls.Add( this.EntityPlaceRB );
            this.CurToolGB.Controls.Add( this.TilePlaceRB );
            this.CurToolGB.Enabled = false;
            this.CurToolGB.Location = new System.Drawing.Point( 12, 27 );
            this.CurToolGB.Name = "CurToolGB";
            this.CurToolGB.Size = new System.Drawing.Size( 200, 101 );
            this.CurToolGB.TabIndex = 14;
            this.CurToolGB.TabStop = false;
            this.CurToolGB.Text = "Current Tool";
            // 
            // EntityPropsRB
            // 
            this.EntityPropsRB.AutoSize = true;
            this.EntityPropsRB.Location = new System.Drawing.Point( 6, 65 );
            this.EntityPropsRB.Name = "EntityPropsRB";
            this.EntityPropsRB.Size = new System.Drawing.Size( 101, 17 );
            this.EntityPropsRB.TabIndex = 2;
            this.EntityPropsRB.Text = "Entity Properties";
            this.EntityPropsRB.UseVisualStyleBackColor = true;
            this.EntityPropsRB.CheckedChanged += new System.EventHandler( this.EntityPropsRB_CheckedChanged );
            // 
            // EntityPlaceRB
            // 
            this.EntityPlaceRB.AutoSize = true;
            this.EntityPlaceRB.Location = new System.Drawing.Point( 6, 42 );
            this.EntityPlaceRB.Name = "EntityPlaceRB";
            this.EntityPlaceRB.Size = new System.Drawing.Size( 104, 17 );
            this.EntityPlaceRB.TabIndex = 1;
            this.EntityPlaceRB.Text = "Entity Placement";
            this.EntityPlaceRB.UseVisualStyleBackColor = true;
            this.EntityPlaceRB.CheckedChanged += new System.EventHandler( this.EntityPlaceRB_CheckedChanged );
            // 
            // TilePlaceRB
            // 
            this.TilePlaceRB.AutoSize = true;
            this.TilePlaceRB.Checked = true;
            this.TilePlaceRB.Location = new System.Drawing.Point( 6, 19 );
            this.TilePlaceRB.Name = "TilePlaceRB";
            this.TilePlaceRB.Size = new System.Drawing.Size( 95, 17 );
            this.TilePlaceRB.TabIndex = 0;
            this.TilePlaceRB.TabStop = true;
            this.TilePlaceRB.Text = "Tile Placement";
            this.TilePlaceRB.UseVisualStyleBackColor = true;
            this.TilePlaceRB.CheckedChanged += new System.EventHandler( this.TilePlaceRB_CheckedChanged );
            // 
            // EntityPropGB
            // 
            this.EntityPropGB.Controls.Add( this.EntityPG );
            this.EntityPropGB.Location = new System.Drawing.Point( 12, 134 );
            this.EntityPropGB.Name = "EntityPropGB";
            this.EntityPropGB.Size = new System.Drawing.Size( 200, 430 );
            this.EntityPropGB.TabIndex = 15;
            this.EntityPropGB.TabStop = false;
            this.EntityPropGB.Text = "Entity Properties";
            this.EntityPropGB.Visible = false;
            // 
            // EntityPG
            // 
            this.EntityPG.Enabled = false;
            this.EntityPG.Location = new System.Drawing.Point( 6, 19 );
            this.EntityPG.Name = "EntityPG";
            this.EntityPG.Size = new System.Drawing.Size( 188, 405 );
            this.EntityPG.TabIndex = 0;
            this.EntityPG.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler( this.EntityPG_PropertyValueChanged );
            // 
            // EntityCM
            // 
            this.EntityCM.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.removeToolStripMenuItem,
            this.cloneToolStripMenuItem} );
            this.EntityCM.Name = "EntityCM";
            this.EntityCM.Size = new System.Drawing.Size( 118, 48 );
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size( 117, 22 );
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler( this.removeToolStripMenuItem_Click );
            // 
            // cloneToolStripMenuItem
            // 
            this.cloneToolStripMenuItem.Name = "cloneToolStripMenuItem";
            this.cloneToolStripMenuItem.Size = new System.Drawing.Size( 117, 22 );
            this.cloneToolStripMenuItem.Text = "Clone";
            this.cloneToolStripMenuItem.Click += new System.EventHandler( this.cloneToolStripMenuItem_Click );
            // 
            // exteriorChunkToolStripMenuItem
            // 
            this.exteriorChunkToolStripMenuItem.Name = "exteriorChunkToolStripMenuItem";
            this.exteriorChunkToolStripMenuItem.Size = new System.Drawing.Size( 161, 22 );
            this.exteriorChunkToolStripMenuItem.Text = "Exterior Chunk";
            this.exteriorChunkToolStripMenuItem.Click += new System.EventHandler( this.exteriorChunkToolStripMenuItem_Click );
            // 
            // ChunkEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 754, 576 );
            this.Controls.Add( this.EntityPropGB );
            this.Controls.Add( this.CurToolGB );
            this.Controls.Add( this.ChunkViewGB );
            this.Controls.Add( this.MenuStrip );
            this.Controls.Add( this.EntityPlaceGB );
            this.Controls.Add( this.TilePropsGB );
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.MenuStrip;
            this.Name = "ChunkEditorForm";
            this.Text = "Lewt Chunk Editor";
            this.TilePropsGB.ResumeLayout( false );
            this.TilePropsGB.PerformLayout();
            this.groupBox1.ResumeLayout( false );
            this.groupBox1.PerformLayout();
            this.VariationGB.ResumeLayout( false );
            this.VariationGB.PerformLayout();
            this.SolidityGB.ResumeLayout( false );
            this.SolidityGB.PerformLayout();
            this.SkinGB.ResumeLayout( false );
            ( (System.ComponentModel.ISupportInitialize) ( this.SkinIDNUD ) ).EndInit();
            this.MenuStrip.ResumeLayout( false );
            this.MenuStrip.PerformLayout();
            this.ChunkViewGB.ResumeLayout( false );
            this.EntityPlaceGB.ResumeLayout( false );
            this.EntityPlaceGB.PerformLayout();
            this.CurToolGB.ResumeLayout( false );
            this.CurToolGB.PerformLayout();
            this.EntityPropGB.ResumeLayout( false );
            this.EntityCM.ResumeLayout( false );
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox TilePropsGB;
        private System.Windows.Forms.RadioButton RanRB;
        private System.Windows.Forms.RadioButton VercRB;
        private System.Windows.Forms.RadioButton VerbRB;
        private System.Windows.Forms.RadioButton VeraRB;
        private System.Windows.Forms.RadioButton DefRB;
        private System.Windows.Forms.NumericUpDown SkinIDNUD;
        private System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dungeonToolStripMenuItem;
        private System.Windows.Forms.GroupBox ChunkViewGB;
        private OpenTK.GLControl ViewControl;
        private System.Windows.Forms.GroupBox SkinGB;
        private System.Windows.Forms.GroupBox VariationGB;
        private System.Windows.Forms.CheckBox VariationCB;
        private System.Windows.Forms.GroupBox SolidityGB;
        private System.Windows.Forms.CheckBox SolidityCB;
        private System.Windows.Forms.RadioButton FloorRB;
        private System.Windows.Forms.RadioButton WallRB;
        private System.Windows.Forms.CheckBox SkinCB;
        private System.Windows.Forms.GroupBox EntityPlaceGB;
        private System.Windows.Forms.CheckBox SnapToTileCB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox EntityClassCB;
        private System.Windows.Forms.GroupBox CurToolGB;
        private System.Windows.Forms.RadioButton EntityPlaceRB;
        private System.Windows.Forms.RadioButton TilePlaceRB;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lightingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem smoothLightingToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton PlacementBoxRB;
        private System.Windows.Forms.RadioButton PlacementFreeRB;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.RadioButton EntityPropsRB;
        private System.Windows.Forms.GroupBox EntityPropGB;
        private System.Windows.Forms.PropertyGrid EntityPG;
        private System.Windows.Forms.ContextMenuStrip EntityCM;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cloneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exteriorChunkToolStripMenuItem;
    }
}

