namespace ChunkEditor
{
    partial class NewDungeonDialog
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.HeightNUD = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.WidthNUD = new System.Windows.Forms.NumericUpDown();
            this.CreateBTN = new System.Windows.Forms.Button();
            this.CancelBTN = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.DungTypeLB = new System.Windows.Forms.ListBox();
            this.groupBox1.SuspendLayout();
            ( (System.ComponentModel.ISupportInitialize) ( this.HeightNUD ) ).BeginInit();
            ( (System.ComponentModel.ISupportInitialize) ( this.WidthNUD ) ).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add( this.label2 );
            this.groupBox1.Controls.Add( this.HeightNUD );
            this.groupBox1.Controls.Add( this.label1 );
            this.groupBox1.Controls.Add( this.WidthNUD );
            this.groupBox1.Location = new System.Drawing.Point( 158, 12 );
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size( 182, 58 );
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Dimentions";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point( 94, 16 );
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size( 38, 13 );
            this.label2.TabIndex = 3;
            this.label2.Text = "Height";
            // 
            // HeightNUD
            // 
            this.HeightNUD.Location = new System.Drawing.Point( 94, 32 );
            this.HeightNUD.Maximum = new decimal( new int[] {
            64,
            0,
            0,
            0} );
            this.HeightNUD.Minimum = new decimal( new int[] {
            4,
            0,
            0,
            0} );
            this.HeightNUD.Name = "HeightNUD";
            this.HeightNUD.Size = new System.Drawing.Size( 82, 20 );
            this.HeightNUD.TabIndex = 2;
            this.HeightNUD.Value = new decimal( new int[] {
            8,
            0,
            0,
            0} );
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point( 6, 16 );
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size( 35, 13 );
            this.label1.TabIndex = 1;
            this.label1.Text = "Width";
            // 
            // WidthNUD
            // 
            this.WidthNUD.Location = new System.Drawing.Point( 6, 32 );
            this.WidthNUD.Maximum = new decimal( new int[] {
            64,
            0,
            0,
            0} );
            this.WidthNUD.Minimum = new decimal( new int[] {
            4,
            0,
            0,
            0} );
            this.WidthNUD.Name = "WidthNUD";
            this.WidthNUD.Size = new System.Drawing.Size( 82, 20 );
            this.WidthNUD.TabIndex = 0;
            this.WidthNUD.Value = new decimal( new int[] {
            8,
            0,
            0,
            0} );
            // 
            // CreateBTN
            // 
            this.CreateBTN.Enabled = false;
            this.CreateBTN.Location = new System.Drawing.Point( 158, 76 );
            this.CreateBTN.Name = "CreateBTN";
            this.CreateBTN.Size = new System.Drawing.Size( 88, 23 );
            this.CreateBTN.TabIndex = 2;
            this.CreateBTN.Text = "Create";
            this.CreateBTN.UseVisualStyleBackColor = true;
            this.CreateBTN.Click += new System.EventHandler( this.CreateBTN_Click );
            // 
            // CancelBTN
            // 
            this.CancelBTN.Location = new System.Drawing.Point( 252, 76 );
            this.CancelBTN.Name = "CancelBTN";
            this.CancelBTN.Size = new System.Drawing.Size( 88, 23 );
            this.CancelBTN.TabIndex = 3;
            this.CancelBTN.Text = "Cancel";
            this.CancelBTN.UseVisualStyleBackColor = true;
            this.CancelBTN.Click += new System.EventHandler( this.CancelBTN_Click );
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add( this.DungTypeLB );
            this.groupBox3.Location = new System.Drawing.Point( 12, 12 );
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size( 140, 237 );
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Dungeon Types";
            // 
            // DungTypeLB
            // 
            this.DungTypeLB.FormattingEnabled = true;
            this.DungTypeLB.Location = new System.Drawing.Point( 6, 19 );
            this.DungTypeLB.Name = "DungTypeLB";
            this.DungTypeLB.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.DungTypeLB.Size = new System.Drawing.Size( 128, 212 );
            this.DungTypeLB.TabIndex = 0;
            this.DungTypeLB.SelectedIndexChanged += new System.EventHandler( this.DungTypeLB_SelectedIndexChanged );
            // 
            // NewDungeonDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 352, 261 );
            this.Controls.Add( this.groupBox3 );
            this.Controls.Add( this.CancelBTN );
            this.Controls.Add( this.CreateBTN );
            this.Controls.Add( this.groupBox1 );
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "NewDungeonDialog";
            this.Text = "New Dungeon";
            this.Load += new System.EventHandler( this.NewDungeonDialog_Load );
            this.groupBox1.ResumeLayout( false );
            this.groupBox1.PerformLayout();
            ( (System.ComponentModel.ISupportInitialize) ( this.HeightNUD ) ).EndInit();
            ( (System.ComponentModel.ISupportInitialize) ( this.WidthNUD ) ).EndInit();
            this.groupBox3.ResumeLayout( false );
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown HeightNUD;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown WidthNUD;
        private System.Windows.Forms.Button CreateBTN;
        private System.Windows.Forms.Button CancelBTN;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListBox DungTypeLB;
    }
}