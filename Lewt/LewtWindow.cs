using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lewt.Shared.World;
using Lewt.Shared.Rendering;
using Lewt.Shared.Entities;
using Lewt.Client.Networking;
using Lewt.Client.UI;
using Lewt.Server.Networking;

using ResourceLib;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Lewt.Shared.Items;

namespace Lewt
{
    public enum LewtKey
    {
        WalkUp,
        WalkDown,
        WalkLeft,
        WalkRight,

        Attack,
        Block,
        Cast,
        Use,

        Menu,
        Inventory,
        Chat
    }

    public class LewtWindow : GameWindow
    {   
#if DEBUG
        private DateTime myLastFPSShow;

        private int myFrames;
        private double myMapRenderTime;
        private double mySprRenderTime;
        private double myMapRenderTime50;
        private double mySprRenderTime50;

        private UILabel myFPSDisplay;
        private UILabel myTimeDisplay;
#endif

        private bool myMapRendererSetUp;
        private bool myViewingOverworld;
        private bool myInGame;
        private bool myHostingLocal;
        private bool myJoinedLocalServer;
        private bool myMenuBtnPressed;
        private bool myUseBtnPressed;
        private bool myWaitingForInventory;
        private UIObject myUIRoot;
        private UIMenu myMainMenu;
        private UIButton mySinglePlayerButton;
        private UIButton myMultiPlayerButton;
        private UIMessageBox myMsgBox;
        private UIChatBox myChatBox;
        private InventoryView myInventoryView;
        private WorldMap myWorldMap;
        private StatBar myHPBar;
        private StatBar myManaBar;
        private CharacterCreation myCreation;

        private Dictionary<LewtKey, Key[]> myKeyBinds;

        public UIObject[] Children
        {
            get
            {
                return myUIRoot.Children;
            }
        }

        public LewtWindow()
            : this( 640, 512 )
        {
            
        }

        public LewtWindow( int width, int height )
            : base( width, height, GraphicsMode.Default, Res.Get<String>( "game_window_title", "Lewt RPG" ) )
        {
            VSync = VSyncMode.Off;

            Res.MountArchive( Res.LoadArchive( "Data" + System.IO.Path.DirectorySeparatorChar + "cl_lewtui.rsa" ) );

            SpriteRenderer.SetUp( width, height );

            Font font = Font.Large;

            myUIRoot = new UIObject( new Vector2( width, height ) );
#if DEBUG
            myFPSDisplay = new UILabel( font, 1.0f );
            myFPSDisplay.Position = new Vector2( 4, 4 );
            myFPSDisplay.Colour = Color4.White;
            AddChild( myFPSDisplay );

            myTimeDisplay = new UILabel( font, 1.0f );
            myTimeDisplay.Position = new Vector2( 4, 4 + font.CharHeight * 3 );
            myTimeDisplay.Colour = Color4.White;
            AddChild( myTimeDisplay );
#endif

            myChatBox = new UIChatBox( Width, new Vector2( 0, Height - 28 ) );
            AddChild( myChatBox );

            myMsgBox = new UIMessageBox( "", "Loading", false )
            {
                Height = 256,
                CentreText = false,
                IsVisible = false
            };
            AddChild( myMsgBox );

            GameServer.ServerMessage += delegate( object sender3, ServerMessageEventArgs e3 )
            {
                myMsgBox.Text += e3.Message.TrimEnd() + "\n";
            };
            GameClient.ClientMessageReceived += delegate( object sender3, ClientMessageEventArgs e3 )
            {
                myMsgBox.Text += e3.Message.TrimEnd() + "\n";
            };

            myMainMenu = new UIMenu( new Vector2( 256, 256 ) )
            {
                Title = "Lewt RPG " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version,
                CanClose = false
            };

            mySinglePlayerButton = myMainMenu.CreateButton( "Single Player", delegate( object sender, MouseButtonEventArgs e )
            {
                myHostingLocal = true;

                myMainMenu.Hide();

                myMsgBox.Text = "Starting single player game, please wait...\n";
                myMsgBox.Show();
                myMsgBox.Focus();
                myMsgBox.Centre();
                
                GameClient.Nickname = "Local Player";

                Lewt.Shared.Networking.LocalClientServer.Reset();
                GameServer.SinglePlayerStart();
            } );
            myMultiPlayerButton = myMainMenu.CreateButton( "Multi Player", delegate( object sender, MouseButtonEventArgs e )
            {
                UIMenu multiMenu = new UIMenu( new Vector2( 192, 256 ) )
                {
                    Title = "Multi Player Menu",
                    CanClose = false
                };
                AddChild( multiMenu );

                myMainMenu.Disable();

                multiMenu.CreateButton( "Join", delegate( object sender2, MouseButtonEventArgs e2 )
                {
                    multiMenu.Disable();

                    IPPrompt ipPrompt = new IPPrompt( delegate( String address, int port )
                    {
                        multiMenu.Close();
                        myMainMenu.Hide();

                        myMsgBox.Text = "Joining game, please wait...\n";
                        myMsgBox.Show();
                        myMsgBox.Focus();
                        myMsgBox.Centre();

                        GameClient.Nickname = "Player";
                        GameClient.Connect( address, port );
                    } );
                    AddChild( ipPrompt );
                    ipPrompt.Centre();

                    ipPrompt.Closed += delegate( object sender3, EventArgs e3 )
                    {
                        multiMenu.Enable();
                    };

                    ipPrompt.FocusOnInput();
                } );
                multiMenu.CreateButton( "Host LAN", delegate( object sender2, MouseButtonEventArgs e2 )
                {
                    multiMenu.Disable();

                    HostPrompt hostPrompt = new HostPrompt( delegate( int maxPlayers, int port )
                    {
                        multiMenu.Close();
                        myMainMenu.Hide();

                        myHostingLocal = true;

                        myMsgBox.Text = "Hosting and joining game, please wait...\n";
                        myMsgBox.Show();
                        myMsgBox.Focus();
                        myMsgBox.Centre();

                        GameClient.Nickname = "Player";
                        Lewt.Shared.Networking.LocalClientServer.Reset();
                        GameServer.Start( maxPlayers, port, true );
                    } ) { Title = "Host LAN Game" };
                    AddChild( hostPrompt );
                    hostPrompt.Centre();

                    hostPrompt.Closed += delegate( object sender3, EventArgs e3 )
                    {
                        multiMenu.Enable();
                    };
                } );
                multiMenu.CreateButton( "Host Internet", delegate( object sender2, MouseButtonEventArgs e2 )
                {
                    multiMenu.Disable();

                    HostPrompt hostPrompt = new HostPrompt( delegate( int maxPlayers, int port )
                    {
                        multiMenu.Close();
                        myMainMenu.Hide();

                        myHostingLocal = true;

                        myMsgBox.Text = "Hosting and joining game, please wait...\n";
                        myMsgBox.Show();
                        myMsgBox.Focus();
                        myMsgBox.Centre();

                        GameClient.Nickname = "Player";
                        Lewt.Shared.Networking.LocalClientServer.Reset();
                        GameServer.Start( maxPlayers, port, false );
                    } ) { Title = "Host Internet Game" };
                    AddChild( hostPrompt );
                    hostPrompt.Centre();

                    hostPrompt.Closed += delegate( object sender3, EventArgs e3 )
                    {
                        multiMenu.Enable();
                    };
                } );
                multiMenu.CreateButton( "Cancel", delegate( object sender2, MouseButtonEventArgs e2 )
                {
                    multiMenu.Close();
                } );

                multiMenu.Closed += delegate( object sender2, EventArgs e2 )
                {
                    myMainMenu.Enable();
                };

                multiMenu.AutoSize();
                multiMenu.Centre();
            } );
            myMainMenu.CreateButton( "Settings" );
            myMainMenu.CreateButton( "Quit", delegate( object sender, MouseButtonEventArgs e )
            {
                Close();
            } );

            AddChild( myMainMenu );
            myMainMenu.AutoSize();
            myMainMenu.Centre();

            Mouse.ButtonDown += delegate( object sender, MouseButtonEventArgs e )
            {
                myUIRoot.SendMouseButtonEvent( new Vector2( e.X, e.Y ), e );
            };

            Mouse.ButtonUp += delegate( object sender, MouseButtonEventArgs e )
            {
                myUIRoot.SendMouseButtonEvent( new Vector2( e.X, e.Y ), e );
            };

            Mouse.Move += delegate( object sender, MouseMoveEventArgs e )
            {
                myUIRoot.SendMouseMoveEvent( new Vector2( e.X, e.Y ), e );
            };

#if DEBUG
            myLastFPSShow = DateTime.Now;
#endif

            GameClient.ClientMessageReceived += new ClientMessageEventHandler( ClientMessageHandler );

            myKeyBinds = new Dictionary<LewtKey, Key[]>
            {
                { LewtKey.WalkUp,       new Key[]{ Key.W, Key.Up } },
                { LewtKey.WalkDown,     new Key[]{ Key.S, Key.Down } },
                { LewtKey.WalkLeft,     new Key[]{ Key.A, Key.Left } },
                { LewtKey.WalkRight,    new Key[]{ Key.D, Key.Right } },
                { LewtKey.Attack,       new Key[]{ Key.X } },
                { LewtKey.Block,        new Key[]{ Key.Z } },
                { LewtKey.Cast,         new Key[]{ Key.C } },
                { LewtKey.Use,          new Key[]{ Key.E } },
                { LewtKey.Chat,         new Key[]{ Key.T } },
                { LewtKey.Inventory,    new Key[]{ Key.I } },
                { LewtKey.Menu,         new Key[]{ Key.Escape } }
            };
        }

        public bool IsKeyPressed( LewtKey lewtKey )
        {
            if ( myKeyBinds.ContainsKey( lewtKey ) )
                foreach ( Key key in myKeyBinds[ lewtKey ] )
                    if ( Keyboard[ key ] )
                        return true;

            return false;
        }

        protected void OnLocalServerReady()
        {
            myJoinedLocalServer = true;

            GameServer.AttemptAddLocalClient();
            GameClient.ConnectLocal();
        }

        protected void OnJoinGame()
        {
            myMsgBox.Hide();

            if ( !myMapRendererSetUp )
            {
                MapRenderer.CameraX = 20;
                MapRenderer.CameraY = 16;

                MapRenderer.CameraScale = 1.0f;

                MapRenderer.SetUp( Width, Height );
                myMapRendererSetUp = true;
            }

            mySinglePlayerButton.Disable();
            myMultiPlayerButton.Disable();

            myMainMenu.InsertButton( 0, "Continue", delegate( object sender, MouseButtonEventArgs e )
            {
                myMainMenu.Hide();
            } );
            myMainMenu.InsertButton( 1, "Leave Map", delegate( object sender, MouseButtonEventArgs e )
            {
                GameClient.SendPlayerLeaveMap();
                myMainMenu.Hide();

                RemoveChild( myManaBar );
                RemoveChild( myHPBar );

                MapRenderer.CameraScale = 1.0f;

                AddChild( myWorldMap );

                myInGame = false;
                myViewingOverworld = true;
            } );
            myMainMenu.InsertButton( 2, "Leave Game", delegate( object sender, MouseButtonEventArgs e )
            {
                myMainMenu.RemoveButton( 0 );
                myMainMenu.RemoveButton( 0 );
                myMainMenu.RemoveButton( 0 );
                myMainMenu.RemoveButton( 0 );
                myMainMenu.AutoSize();
                myMainMenu.Centre();

                mySinglePlayerButton.Enable();
                myMultiPlayerButton.Enable();

                RemoveChild( myHPBar );
                RemoveChild( myManaBar );

                myInGame = false;
                myJoinedLocalServer = false;

                GameClient.Disconnect();

                if ( GameServer.Running )
                    GameServer.Stop();
            } );
            myMainMenu.InsertButton( 3, "View Stats", delegate( object sender, MouseButtonEventArgs e )
            {
                myMainMenu.Disable();

                CharAttribDisplay popup = new CharAttribDisplay( GameClient.PlayerEntity );
                AddChild( popup );
                popup.Centre();

                popup.Closed += delegate( object sender2, EventArgs e2 )
                {
                    myMainMenu.Enable();
                };
            } ).Disable();

            myMainMenu.AutoSize();
            myMainMenu.Centre();

            myWorldMap = new WorldMap( new Vector2( Width, Height ), GameClient.Overworld );
            myWorldMap.DungeonSelected += delegate( object sender, DungeonSelectedEventArgs e )
            {
                GameClient.SendMapRequest( e.Dungeon.ID );

                // GameClient.SendMapRequest( 0xFFFF );

                RemoveChild( myWorldMap );
            };
            AddChild( myWorldMap );

            myViewingOverworld = true;
        }

        protected void OnEnterMap()
        {
            MapRenderer.CameraScale = 1.0f;

            myHPBar = new StatBar( GameClient.PlayerEntity, StatBarType.HitPoints, Height / 3.0f - 16.0f )
            {
                Position = new Vector2( Width - 24.0f, Height * 2.0f / 3.0f + 8.0f )
            };
            AddChild( myHPBar );

            myManaBar = new StatBar( GameClient.PlayerEntity, StatBarType.ManaLevel, Height / 3.0f - 16.0f )
            {
                Position = new Vector2( Width - 44.0f, Height * 2.0f / 3.0f + 8.0f )
            };
            AddChild( myManaBar );

            GameClient.PlayerEntity.Killed += new KilledEventHandler( PlayerEntity_Killed );
            GameClient.PlayerEntity.StartedTrading += new StartedTradingEventHandler( PlayerEntity_StartedTrading );
            GameClient.PlayerEntity.InventoryShown += new InventoryShownEventHandler( PlayerEntity_InventoryShown );
            
            myInGame = true;
        }

        void PlayerEntity_Killed( object sender, KilledEventArgs e ) {
            var dieMenu = new DieMenu( (Player) sender );
            AddChild( dieMenu );
            dieMenu.Centre();
        }

        void PlayerEntity_StartedTrading( object sender, StartedTradingEventArgs e )
        {
            ShowInventory( e.PlayerInventory, e.EntityInventory );
        }

        void PlayerEntity_InventoryShown( object sender, InventoryShownEventArgs e )
        {
            ShowInventory( e.Inventory );
            myWaitingForInventory = false;
        }

        public void AddChild( UIObject child )
        {
            myUIRoot.AddChild( child );
        }

        public void RemoveChild( UIObject child )
        {
            myUIRoot.RemoveChild( child );
        }

        private void ClientMessageHandler( object sender, ClientMessageEventArgs e )
        {
            myChatBox.AddMessage( e.Message );
        }

        protected override void OnRenderFrame( FrameEventArgs e )
        {
            MakeCurrent();
            GL.Clear( ClearBufferMask.ColorBufferBit );

#if DEBUG
            DateTime start;
#endif

            if ( myInGame )
            {
                MapRenderer.CameraX = (float) GameClient.PlayerEntity.OriginX;
                MapRenderer.CameraY = (float) GameClient.PlayerEntity.OriginY;
#if DEBUG
                start = DateTime.Now;
#endif
                GameClient.Map.Render( GameClient.Map.IsInterior );
#if DEBUG
                myMapRenderTime50 += ( DateTime.Now - start ).TotalMilliseconds;

                if ( myFrames + 1 >= 50 ) {
                    myMapRenderTime = myMapRenderTime50 / 50;
                    myMapRenderTime50 = 0;
                }
#endif
            }
            
#if DEBUG
            start = DateTime.Now;
#endif
            SpriteRenderer.Begin();
            if ( myInGame )
            {
                foreach ( ClientInfo client in GameClient.Clients )
                    if ( client.PlayerEntity != null )
                        client.PlayerEntity.RenderName();
            }

            myUIRoot.Render();
            SpriteRenderer.End();
#if DEBUG
            mySprRenderTime50 += ( DateTime.Now - start ).TotalMilliseconds;
            if ( myFrames + 1 >= 50 ) {
                mySprRenderTime = mySprRenderTime50 / 50;
                mySprRenderTime50 = 0;
            }

            if ( ++myFrames >= 50 )
                myFrames = 0;
#endif

            SwapBuffers();
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            myUIRoot.SendKeyPressEvent( e );
        }
        
        protected override void OnUpdateFrame( FrameEventArgs e )
        {
            if ( myHostingLocal && !myJoinedLocalServer && GameServer.Running )
                OnLocalServerReady();

            if ( !myViewingOverworld && GameClient.IsViewingOverworld )
                OnJoinGame();

            if ( !myInGame && GameClient.IsPlaying )
                OnEnterMap();

            if ( GameClient.CreateCharacter ) {
                GameClient.CreateCharacter = false;

                myCreation = new CharacterCreation();
                AddChild( myCreation );
                myCreation.Show();
                myCreation.Focus();
                myCreation.Centre();
            }

            if ( !GameClient.Connected || !GameClient.IsPlaying )
                return;

            if ( !myCreation.IsVisible && !myChatBox.IsFocused && !myMainMenu.IsVisible && myInventoryView == null )
            {
                WalkDirection newDir = WalkDirection.Still;

                if ( IsKeyPressed( LewtKey.WalkLeft ) )
                    newDir = WalkDirection.Left;
                else if ( IsKeyPressed( LewtKey.WalkUp ) )
                    newDir = WalkDirection.Up;
                else if ( IsKeyPressed( LewtKey.WalkRight ) )
                    newDir = WalkDirection.Right;
                else if ( IsKeyPressed( LewtKey.WalkDown ) )
                    newDir = WalkDirection.Down;

                if ( newDir != GameClient.PlayerEntity.WalkDirection )
                {
                    if ( newDir == WalkDirection.Still )
                    {
                        GameClient.PlayerEntity.StopWalking();
                        GameClient.SendPlayerStopMoving();
                    }
                    else
                    {
                        GameClient.PlayerEntity.StartWalking( newDir );
                        GameClient.SendPlayerStartMoving( newDir );
                    }
                }

                if ( IsKeyPressed( LewtKey.Cast ) && GameClient.PlayerEntity.CanCast )
                {
                    GameClient.PlayerEntity.Cast();
                    GameClient.SendPlayerCast( GameClient.PlayerEntity.CastAngle );
                }

                if ( IsKeyPressed( LewtKey.Use ) )
                {
                    if ( !myUseBtnPressed )
                    {
                        myUseBtnPressed = true;
                        Entity closest = GameClient.PlayerEntity.GetNearestUseableEntity();

                        if ( closest != null )
                            GameClient.SendPlayerUse( closest );
                    }
                }
                else
                    myUseBtnPressed = false;

                if ( IsKeyPressed( LewtKey.Menu ) )
                {
                    if ( !myMenuBtnPressed )
                    {
                        myMenuBtnPressed = true;
                        myMainMenu.Show();
                    }
                }
                else
                    myMenuBtnPressed = false;

                if ( IsKeyPressed( LewtKey.Inventory ) && !myWaitingForInventory )
                {
                    myWaitingForInventory = true;
                    GameClient.SendPlayerViewInventory();
                }

                if ( IsKeyPressed( LewtKey.Chat ) && !Lewt.Server.Networking.GameServer.SinglePlayer )
                    myChatBox.StartTyping();
            }
            else if ( IsKeyPressed( LewtKey.Menu ) )
            {
                if ( !myMenuBtnPressed )
                {
                    myMenuBtnPressed = true;

                    if ( myChatBox.IsFocused )
                        myChatBox.UnFocus();
                    else if ( myMainMenu.IsVisible )
                        myMainMenu.Hide();
                    else if ( myInventoryView != null )
                        myInventoryView.Close();
                }
            }
            else
                myMenuBtnPressed = false;

#if DEBUG
            if ( ( DateTime.Now - myLastFPSShow ).TotalSeconds >= 0.5 )
            {
                myFPSDisplay.Text = "Map Render Time: " + myMapRenderTime.ToString( "F" ) + "ms\nSprite Render Time: " + mySprRenderTime.ToString( "F" ) + "ms\nEntities: " + GameClient.Map.Entities.Length.ToString();
                myLastFPSShow = DateTime.Now;
            }

            myTimeDisplay.Text = "Client time: " + GameClient.Map.TimeSeconds.ToString( "F" );
#endif

            GameClient.Map.Think( e.Time );
        }

        private void ShowInventory( Inventory playerInventory, Inventory otherInventory = null )
        {
            if ( GameClient.PlayerEntity.WalkDirection != WalkDirection.Still )
            {
                GameClient.PlayerEntity.StopWalking();
                GameClient.SendPlayerStopMoving();
            }

            if( otherInventory == null )
                myInventoryView = new InventoryView( playerInventory, new Vector2( 276.0f, 204.0f ) );
            else
                myInventoryView = new InventoryView( playerInventory, otherInventory, new Vector2( 550.0f, 204.0f ) );
            
            AddChild( myInventoryView );
            myInventoryView.Centre();

            myInventoryView.Closed += delegate( Object sender, EventArgs e2 )
            {
                myInventoryView = null;
            };
        }
    }
}
