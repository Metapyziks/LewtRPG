using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lewt.Client.UI;
using Lewt.Shared.Entities;
using OpenTK;
using Lewt.Shared.Rendering;
using Lewt.Shared.Stats;
using Lewt.Client.Networking;
using Lewt.Client;
namespace Lewt
{
    class CharacterCreation : UIWindow
    {
        private NameCreation myNamePage;
        private AttributeCreation myAttribPage;

        private CharacterCreationOutput myOutput;

        public CharacterCreation() {
            Title = "Attributes and Skills";
            CanClose = false;
            CanResize = true;
            myOutput = new CharacterCreationOutput( GameClient.CharacterBaseAttribPoints,
                GameClient.CharacterBaseSkillPoints );
            NamePage();
        }

        public void DoneName() {
            AttribPage();
        }
        public void DoneAttrib() {
            GameClient.SendCharacterCreationInfo( myOutput );
            Close();
        }
        public void NamePage() {
            Title = "Choose a name";

            if ( myAttribPage != null )
                myAttribPage.Hide();

            if ( myNamePage == null ) {

                myNamePage = new NameCreation( this, myOutput );
                AddChild( myNamePage );

            }

            myNamePage.Show();
            myNamePage.Focus();
            SetSize( myNamePage.Width + PaddingLeft + PaddingRight, myNamePage.Height + PaddingTop + PaddingBottom );

            Centre();
        }
        public void AttribPage() {
            Title = "Attributes Selection";

            if ( myNamePage != null )
                myNamePage.Hide();

            if ( myAttribPage == null ) {
                myAttribPage = new AttributeCreation( this, myOutput );
                AddChild( myAttribPage );
            }

            myAttribPage.Show();
            SetSize( myAttribPage.Width + PaddingLeft + PaddingRight, myAttribPage.Height + PaddingTop + PaddingBottom );
            Centre();
        
        }

    }
    class NameCreation : UIObject
    {
        private CharacterCreation myParent;
        private CharacterCreationOutput myOutput;
        private UITextBox myTextbox;

        public NameCreation( CharacterCreation parent, CharacterCreationOutput output ) {
            myParent = parent;
            myOutput = output;

            var label = new UILabel( Font.Large, new Vector2(4, 24) )
            {
                Text = "Name:"
            };
            AddChild( label );

            myTextbox = new UITextBox( new Vector2( 200 - 70 - 2, 20 ), new Vector2( 70, 20 ) )
            {
                Text = "Player"
            };
            AddChild( myTextbox );

            var button = new UIButton( new Vector2( 100 , 20 ), new Vector2( 100-4, 80-4 ) )
            {
                Text = "Next",
                CentreText = true
            };
            button.Click += new MouseButtonEventHandler( button_Click );
            AddChild( button );

            CanResize = true;
            SetSize( 200, 100 );

            myTextbox.Focus();
        }

        void button_Click( object sender, OpenTK.Input.MouseButtonEventArgs e ) {
            myOutput.PlayerName = myTextbox.Text;
            myParent.DoneName();
        }
    }
    class AttributeCreation : UIObject
    {
        private int myUnusedPoints;
        private CharacterCreationOutput myOutput;
        private UILabel myPointsLabel;
        private CharacterCreation myParent;
        private SkillRow[] skillRows;

        public int UnusedPoints {
            get {
                return myUnusedPoints;
            }
            set {
                myUnusedPoints = value;
                myPointsLabel.Text = "Points left: "+myUnusedPoints.ToString();
            }
        }

        public AttributeCreation( CharacterCreation parent, CharacterCreationOutput output ) {
            myParent = parent;
            myOutput = output;



            float y = 4;

            myPointsLabel = new UILabel( Font.Large, new Vector2( 4, y ) );
            AddChild( myPointsLabel );
            UnusedPoints = GameClient.CharacterUnusedAttribPoints;

            y += 30;

            foreach ( CharAttribute attrib in CharAttribute.GetAll() ) {
                var row = new AttribRow( this, attrib, myOutput );
                row.Top = y;
                row.Left = 4;
                y += 30;
                AddChild( row );
            }

            y += 20;

            var skills = CharSkill.GetAll();
            skillRows = new SkillRow[ skills.Length ];

            int i = 0;
            foreach ( CharSkill skill in skills ) {
                var row = new SkillRow( new Vector2( 4, y ), skill, myOutput );
                y += 25;
                skillRows[ i ] = row;
                i++;
                AddChild( row );
            }

            var button = new UIButton( new Vector2( 150, 20 ), new Vector2( 300-150-4, y ) )
            {
                Text = "Create Character",
                CentreText = true
            };
            AddChild( button );
            button.Click += new MouseButtonEventHandler( button_Click );

            var back = new UIButton( new Vector2( 50, 20 ), new Vector2( 4, y ) )
            {
                Text = "Back",
                CentreText = true
            };
            back.Click += new MouseButtonEventHandler( back_Click );
            AddChild( back );

            y += 20;

            Width = 300;
            Height = y + 4 + PaddingTop + PaddingBottom ;

        }

        void back_Click( object sender, OpenTK.Input.MouseButtonEventArgs e ) {
            myParent.NamePage();
        }

        void button_Click( object sender, OpenTK.Input.MouseButtonEventArgs e ) {
            myParent.DoneAttrib();
        }

        private void RefreshSkills() {
            foreach ( var row in skillRows ) {
                row.Refresh();
            }
        }
        private bool IncreaseAttrib( CharAttribute attrib, int value ) {
            if ( UnusedPoints > 0 && value < 100 ) {
                UnusedPoints -= 5;
                myOutput.SetAttributePoints( attrib, value + 5 );
                RefreshSkills();
                return true;
            }
            return false;
        }
        private bool DecreaseAttrib( CharAttribute attrib, int value ) {
            if ( value > 5 ) {
                UnusedPoints += 5;
                myOutput.SetAttributePoints( attrib, value - 5 );
                RefreshSkills();
                return true;
            }
            return false;
        }


        private class AttribRow : UIObject
        {
            private AttributeCreation myParent;
            private CharacterCreationOutput myOutput;
            private CharAttribute myAttrib;

            private UIButton myMinusButton;
            private UIButton myPlusButton;
            private UILabel myLabel;
            private UILabel myValueLabel;
            private const int LeftColumn = 200;
            private const int RightColumn = 80;
            private int myValue = 0;

            public int Value {
                get {
                    return myValue;
                }
                private set {
                    myValue = value;
                    myValueLabel.Text = myValue.ToString();
                }

            }

            public AttribRow( AttributeCreation parent, CharAttribute attrib, CharacterCreationOutput output ) {
                myParent = parent;
                myOutput = output;
                
                myAttrib = attrib;

                myMinusButton = new UIButton( new Vector2( 20, 20 ), new Vector2( LeftColumn, 0 ) )
                {
                    Text = "-",
                    CentreText = true
                };

                myPlusButton = new UIButton( new Vector2( 20, 20 ), new Vector2( LeftColumn+RightColumn-20, 0 ) )
                {
                    Text = "+",
                    CentreText = true
                };


                myLabel = new UILabel( Font.Large, new Vector2( 0, 5 ) )
                {
                    Text = attrib.ToString()
                };
                myValueLabel = new UILabel( Font.Large, new Vector2( LeftColumn + RightColumn/2-10, 5 ) )
                {
                    //Text = value.ToString()
                };

                AddChild( myMinusButton );
                AddChild( myPlusButton );
                AddChild( myLabel );
                AddChild( myValueLabel );

                Value = myOutput.GetAttributePoints( myAttrib );

                myMinusButton.Click += new MouseButtonEventHandler( myMinusButton_Click );
                myPlusButton.Click += new MouseButtonEventHandler( myPlusButton_Click );
            }

            void myPlusButton_Click( object sender, OpenTK.Input.MouseButtonEventArgs e ) {
                if ( myParent.IncreaseAttrib( myAttrib, Value ) )
                    Value += 5;
            }

            void myMinusButton_Click( object sender, OpenTK.Input.MouseButtonEventArgs e ) {
                if ( myParent.DecreaseAttrib( myAttrib, Value ) )
                    Value -= 5;
            }
        }

        private class SkillRow: UILabel
        {
            private CharSkill mySkill;
            protected CharacterCreationOutput myOutput;

            public SkillRow( Vector2 position, CharSkill skill, CharacterCreationOutput output )
                : base( Font.Large, position ) {

                    myOutput = output;
                mySkill = skill;
                Refresh();

            }

            public void Refresh() {
                Text = mySkill.ToString();
                while ( Text.Length < 26 )
                    Text += " ";

                int baseSkill = myOutput.GetBaseSkillPoints( mySkill );
                int newSkill = myOutput.GetTotalSkillPoints( mySkill );
                int diff = newSkill - baseSkill;
                Text += newSkill + " " + (diff != 0 ? diff.ToString("(+#);(-#);") : "");
            }
        }
    }

    
}
