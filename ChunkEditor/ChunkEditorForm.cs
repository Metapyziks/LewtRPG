using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

using ResourceLib;

using Lewt.Shared;
using Lewt.Shared.World;
using Lewt.Shared.Rendering;
using Lewt.Shared.Entities;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using System.IO;

namespace ChunkEditor
{
    public partial class ChunkEditorForm : Form
    {
        private EditorMap myCurrentMap;
        private bool myDrawing;
        private bool myDragging;
        private Point myDragMid;
        private Vector2d myEntDragOrigin;
        private Random myRand;

        private Point myBoxOrigin;
        private Point myBoxEnd;

        private Texture myDragImage;
        private Sprite myDragSprite;
        
        private Sprite myTileSprite;

        private List<ConnectorArrow>[] myConArrows;
        private DungeonClass[] myMapTypes;

        private String myLastSavePath;
        private Entity mySelectedEntity;

        public ChunkEditorForm( String map = "" )
        {
            InitializeComponent();

            myRand = new Random();

            Res.RegisterManager( new RTextureManager() );
            Res.RegisterManager( new RScriptManager() );

            String dataLoc = Assembly.GetCallingAssembly().Location;
            int nameLength = Assembly.GetCallingAssembly().GetName().Name.Length + 4;
            dataLoc = dataLoc.Substring( 0, dataLoc.Length - nameLength ) + "Data" + Path.DirectorySeparatorChar;
            Res.MountArchive( Res.LoadArchive( dataLoc + "sh_lewtcontent.rsa" ) );
            Res.MountArchive( Res.LoadArchive( dataLoc + "cl_lewtcontent.rsa" ) );

            MapRenderer.CameraScale = 2.0f;
            MapRenderer.CullHiddenFaces = false;

            foreach ( Type t in Assembly.GetAssembly( typeof( Entity ) ).GetTypes() )
                if ( t.DoesExtend( typeof( Entity ) ) && t.GetCustomAttributes( typeof( PlaceableInEditorAttribute ), true ).Length > 0 )
                    EntityClassCB.Items.Add( t );

            foreach ( Type t in Scripts.GetTypes( typeof( Entity ) ) )
                if ( t.GetCustomAttributes( typeof( PlaceableInEditorAttribute ), true ).Length > 0 )
                    EntityClassCB.Items.Add( t );

            EntityClassCB.SelectedIndex = 0;

            myLastSavePath = map;

            if ( map != "" && File.Exists( map ) )
                Open( map );
        }

        private void RenderMap()
        {
            GL.ClearColor( 0.0f, 0.0f, 0.0f, 1.0f );
            GL.Clear( ClearBufferMask.ColorBufferBit );

            if ( myCurrentMap != null )
            {
                float minX = ( myCurrentMap.Width <= 16 ? myCurrentMap.Width / 2 : 16 / MapRenderer.CameraScale );
                float minY = ( myCurrentMap.Height <= 16 ? myCurrentMap.Height / 2 : 16 / MapRenderer.CameraScale );

                if ( MapRenderer.CameraX < minX )
                    MapRenderer.CameraX = minX;
                if ( MapRenderer.CameraY < minY )
                    MapRenderer.CameraY = minY;

                float maxX = ( myCurrentMap.Width <= 16 ? myCurrentMap.Width / 2 : Math.Max( myCurrentMap.Width - 16 / MapRenderer.CameraScale, 16 / MapRenderer.CameraScale ) );
                float maxY = ( myCurrentMap.Height <= 16 ? myCurrentMap.Height / 2 : Math.Max( myCurrentMap.Height - 16 / MapRenderer.CameraScale, 16 / MapRenderer.CameraScale ) );

                if ( MapRenderer.CameraX > maxX )
                    MapRenderer.CameraX = maxX;
                if ( MapRenderer.CameraY > maxY )
                    MapRenderer.CameraY = maxY;

                myCurrentMap.Render( lightingToolStripMenuItem.Checked, true );
            }

            if ( myDrawing && TilePlaceRB.Checked && PlacementBoxRB.Checked )
            {
                int minX = Math.Min( myBoxOrigin.X, myBoxEnd.X );
                int minY = Math.Min( myBoxOrigin.Y, myBoxEnd.Y );
                int maxX = Math.Max( myBoxOrigin.X, myBoxEnd.X );
                int maxY = Math.Max( myBoxOrigin.Y, myBoxEnd.Y );

                int width = maxX - minX + 1;
                int height = maxY - minY + 1;

                myTileSprite.Size = new Vector2( width * 16.0f * MapRenderer.CameraScale, height * 16.0f * MapRenderer.CameraScale );
                myTileSprite.Position = new Vector2(
                ( minX - MapRenderer.CameraX ) * 16.0f * MapRenderer.CameraScale + 256,
                ( minY - MapRenderer.CameraY ) * 16.0f * MapRenderer.CameraScale + 256 );
            }

            SpriteRenderer.Begin();
            
            if( myCurrentMap != null && ( TilePlaceRB.Checked || ( EntityPlaceRB.Checked && SnapToTileCB.Checked ) ) )
                myTileSprite.Render();

            if ( myConArrows != null )
            {
                foreach( List<ConnectorArrow> edge in myConArrows )
                    foreach ( ConnectorArrow arr in edge )
                        arr.Render();
            }
            
            if ( myDragging )
                myDragSprite.Render();

            SpriteRenderer.End();

            ViewControl.SwapBuffers();
        }

        private void PlaceEntity()
        {
            Point viewOrigin = ViewControl.PointToScreen( new Point( 256, 256 ) );
            
            double x = ( ( Cursor.Position.X - viewOrigin.X ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraX );
            double y = ( ( Cursor.Position.Y - viewOrigin.Y ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraY );

            if ( SnapToTileCB.Checked )
            {
                x = Math.Floor( x ) + 0.5;
                y = Math.Floor( y ) + 0.5;
            }

            Type t = EntityClassCB.SelectedItem as Type;
            Entity ent = t.GetConstructor( new Type[ 0 ] ).Invoke( new object[ 0 ] ) as Entity;
            myCurrentMap.AddEntity( ent );
            ent.OriginX = x;
            ent.OriginY = y;

            if ( ent is Light )
                ( ent as Light ).Update();

            RenderMap();
        }

        private void StartDrawing()
        {
            myDrawing = true;
            myTileSprite.Colour = new Color4( 0.0f, 1.0f, 0.0f, 0.25f );

            if ( TilePlaceRB.Checked && PlacementBoxRB.Checked )
            {
                Point viewOrigin = ViewControl.PointToScreen( new Point( 256, 256 ) );

                myBoxOrigin = new Point()
                {
                    X = (int) Math.Floor( ( Cursor.Position.X - viewOrigin.X ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraX ),
                    Y = (int) Math.Floor( ( Cursor.Position.Y - viewOrigin.Y ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraY )
                };

                if ( myBoxOrigin.X < 0 )
                    myBoxOrigin.X = 0;
                if ( myBoxOrigin.X >= myCurrentMap.Width )
                    myBoxOrigin.X = myCurrentMap.Width - 1;
                if ( myBoxOrigin.Y < 0 )
                    myBoxOrigin.Y = 0;
                if ( myBoxOrigin.Y >= myCurrentMap.Height )
                    myBoxOrigin.Y = myCurrentMap.Height - 1;
            }

            DrawWithCursor();
            RenderMap();
        }

        private void StopDrawing()
        {
            myDrawing = false;
            myTileSprite.Colour = new Color4( 1.0f, 1.0f, 1.0f, 0.125f );
            myTileSprite.Size = new Vector2( 16.0f * MapRenderer.CameraScale, 16.0f * MapRenderer.CameraScale );

            if ( TilePlaceRB.Checked && PlacementBoxRB.Checked )
            {
                int minX = Math.Min( myBoxOrigin.X, myBoxEnd.X );
                int minY = Math.Min( myBoxOrigin.Y, myBoxEnd.Y );
                int maxX = Math.Max( myBoxOrigin.X, myBoxEnd.X );
                int maxY = Math.Max( myBoxOrigin.Y, myBoxEnd.Y );

                for( int x = minX; x <= maxX; ++ x )
                    for ( int y = minY; y <= maxY; ++y )
                    {
                        GameTile t = myCurrentMap.GetTile( x, y );

                        if ( SolidityCB.Checked )
                        {
                            if ( t.IsWall != WallRB.Checked )
                            {
                                t.IsWall = WallRB.Checked;
                                if ( myCurrentMap.IsInterior &&
                                    ( x == 0 || x == myCurrentMap.Width - 1 ||
                                     y == 0 || y == myCurrentMap.Height - 1 ) )
                                {
                                    if ( WallRB.Checked )
                                        RemoveConnector( x, y );
                                    else
                                        AddConnector( x, y );
                                }

                                myCurrentMap.ForceLightUpdate();
                            }
                        }

                        if ( SkinCB.Checked )
                            t.Skin = (byte) SkinIDNUD.Value;

                        if ( VariationCB.Checked )
                            t.Alt = (byte) ( DefRB.Checked ? 0 : VeraRB.Checked ? 1 : VerbRB.Checked ? 2 : VercRB.Checked ? 3 : myRand.Next( 0, 4 ) );
                    }
            }

            RenderMap();
        }

        private void DrawWithCursor()
        {
            Point viewOrigin = ViewControl.PointToScreen( new Point( 256, 256 ) );

            Point pos = new Point()
            {
                X = (int) Math.Floor( ( Cursor.Position.X - viewOrigin.X ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraX ),
                Y = (int) Math.Floor( ( Cursor.Position.Y - viewOrigin.Y ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraY )
            };

            if ( TilePlaceRB.Checked )
            {
                if ( PlacementFreeRB.Checked )
                {
                    GameTile t = myCurrentMap.GetTile( pos.X, pos.Y );

                    if ( SolidityCB.Checked )
                    {
                        t.IsWall = WallRB.Checked;
                        if ( myCurrentMap.IsInterior &&
                            ( pos.X == 0 || pos.X == myCurrentMap.Width - 1 ||
                            pos.Y == 0 || pos.Y == myCurrentMap.Height - 1 ) )
                        {
                            if ( WallRB.Checked )
                                RemoveConnector( pos.X, pos.Y );
                            else
                                AddConnector( pos.X, pos.Y );
                        }

                        myCurrentMap.ForceLightUpdate();
                    }

                    if ( SkinCB.Checked )
                        t.Skin = (byte) SkinIDNUD.Value;

                    if ( VariationCB.Checked )
                        t.Alt = (byte) ( DefRB.Checked ? 0 : VeraRB.Checked ? 1 : VerbRB.Checked ? 2 : VercRB.Checked ? 3 : myRand.Next( 0, 4 ) );
                }
                else
                {
                    myBoxEnd = pos;

                    if ( myBoxEnd.X < 0 )
                        myBoxEnd.X = 0;
                    if ( myBoxEnd.X >= myCurrentMap.Width )
                        myBoxEnd.X = myCurrentMap.Width - 1;
                    if ( myBoxEnd.Y < 0 )
                        myBoxEnd.Y = 0;
                    if ( myBoxEnd.Y >= myCurrentMap.Height )
                        myBoxEnd.Y = myCurrentMap.Height - 1;
                }
            }
        }

        private void StartPanningView()
        {
            myDragging = true;

            myDragMid = Cursor.Position;
            myDragSprite.X = myDragMid.X - ViewControl.PointToScreen( Point.Empty ).X - 10;
            myDragSprite.Y = myDragMid.Y - ViewControl.PointToScreen( Point.Empty ).Y - 10;
            Cursor.Hide();

            RenderMap();
        }

        private void StopPanningView()
        {
            myDragging = false;
            Cursor.Show();

            RenderMap();
        }

        private void PanViewWithCursor()
        {
            Point diff = Cursor.Position;
            diff.X -= myDragMid.X;
            diff.Y -= myDragMid.Y;

            if ( diff.X == 0 && diff.Y == 0 )
                return;

            MapRenderer.CameraX += diff.X / ( 16.0f * MapRenderer.CameraScale );
            MapRenderer.CameraY += diff.Y / ( 16.0f * MapRenderer.CameraScale );

            Cursor.Position = myDragMid;
        }

        private void AddConnector( int x, int y )
        {
            ConnectorArrow addedTo = null;

            if ( x == 0 || x == myCurrentMap.Width - 1 )
            {
                int arrno = x == 0 ? 0 : 2;

                foreach ( ConnectorArrow arr in myConArrows[ arrno ] )
                {
                    if ( arr.ConnectorInfo.Y == y + 1 )
                    {
                        arr.ConnectorInfo.Y -= 1;
                        arr.ConnectorInfo.Size++;
                        addedTo = arr;
                        break;
                    }
                    else if ( arr.ConnectorInfo.Y + arr.ConnectorInfo.Size == y )
                    {
                        arr.ConnectorInfo.Size++;
                        addedTo = arr;
                        break;
                    }
                    else if ( y < arr.ConnectorInfo.Y || y > arr.ConnectorInfo.Y + arr.ConnectorInfo.Size )
                        continue;

                    return;
                }

                if ( addedTo == null )
                    myConArrows[ arrno ].Add( new ConnectorArrow( x, y, true, 1 ) );
                else
                {
                    myConArrows[ arrno ].Remove( addedTo );
                    foreach ( ConnectorArrow arr in myConArrows[ arrno ] )
                    {
                        if ( arr.ConnectorInfo.Y + arr.ConnectorInfo.Size == addedTo.ConnectorInfo.Y )
                        {
                            arr.ConnectorInfo.Size += addedTo.ConnectorInfo.Size;
                            return;
                        }
                        else if ( arr.ConnectorInfo.Y == addedTo.ConnectorInfo.Y + addedTo.ConnectorInfo.Size )
                        {
                            arr.ConnectorInfo.Y -= addedTo.ConnectorInfo.Size;
                            arr.ConnectorInfo.Size += addedTo.ConnectorInfo.Size;
                            return;
                        }
                    }
                    myConArrows[ arrno ].Add( addedTo );
                }
            }
            else if ( y == 0 || y == myCurrentMap.Height - 1 )
            {
                int arrno = y == 0 ? 1 : 3;

                foreach ( ConnectorArrow arr in myConArrows[ arrno ] )
                {
                    if ( arr.ConnectorInfo.X == x + 1 )
                    {
                        arr.ConnectorInfo.X -= 1;
                        arr.ConnectorInfo.Size++;
                        addedTo = arr;
                        break;
                    }
                    else if ( arr.ConnectorInfo.X + arr.ConnectorInfo.Size == x )
                    {
                        arr.ConnectorInfo.Size++;
                        addedTo = arr;
                        break;
                    }
                    else if ( x < arr.ConnectorInfo.X || x > arr.ConnectorInfo.X + arr.ConnectorInfo.Size )
                        continue;

                    return;
                }

                if ( addedTo == null )
                    myConArrows[ arrno ].Add( new ConnectorArrow( x, y, false, 1 ) );
                else
                {
                    myConArrows[ arrno ].Remove( addedTo );
                    foreach ( ConnectorArrow arr in myConArrows[ arrno ] )
                    {
                        if ( arr.ConnectorInfo.X + arr.ConnectorInfo.Size == addedTo.ConnectorInfo.X )
                        {
                            arr.ConnectorInfo.Size += addedTo.ConnectorInfo.Size;
                            return;
                        }
                        else if ( arr.ConnectorInfo.X == addedTo.ConnectorInfo.X + addedTo.ConnectorInfo.Size )
                        {
                            arr.ConnectorInfo.X -= addedTo.ConnectorInfo.Size;
                            arr.ConnectorInfo.Size += addedTo.ConnectorInfo.Size;
                            return;
                        }
                    }
                    myConArrows[ arrno ].Add( addedTo );
                }
            }
        }

        private void RemoveConnector( int x, int y )
        {
            if ( x == 0 || x == myCurrentMap.Width - 1 )
            {
                int arrno = x == 0 ? 0 : 2;

                for ( int i = 0; i < myConArrows[ arrno ].Count; ++ i )
                {
                    ConnectorArrow arr = myConArrows[ arrno ][ i ];

                    if ( y < arr.ConnectorInfo.Y || y > arr.ConnectorInfo.Y + arr.ConnectorInfo.Size )
                        continue;

                    int end = arr.ConnectorInfo.Y + arr.ConnectorInfo.Size;

                    arr.ConnectorInfo.Size = y - arr.ConnectorInfo.Y;

                    if ( arr.ConnectorInfo.Size == 0 )
                        myConArrows[ arrno ].Remove( arr );

                    if( end - y - 1 > 0 )
                        myConArrows[ arrno ].Add( new ConnectorArrow( arr.ConnectorInfo.X, y + 1, true, end - y - 1 ) );

                    return;
                }
            }
            else if ( y == 0 || y == myCurrentMap.Height - 1 )
            {
                int arrno = y == 0 ? 1 : 3;

                for ( int i = 0; i < myConArrows[ arrno ].Count; ++i )
                {
                    ConnectorArrow arr = myConArrows[ arrno ][ i ];

                    if ( x < arr.ConnectorInfo.X || x > arr.ConnectorInfo.X + arr.ConnectorInfo.Size )
                        continue;

                    int end = arr.ConnectorInfo.X + arr.ConnectorInfo.Size;

                    arr.ConnectorInfo.Size = x - arr.ConnectorInfo.X;

                    if ( arr.ConnectorInfo.Size == 0 )
                        myConArrows[ arrno ].Remove( arr );

                    if ( end - x - 1 > 0 )
                        myConArrows[ arrno ].Add( new ConnectorArrow( x + 1, arr.ConnectorInfo.Y, false, end - x - 1 ) );

                    return;
                }
            }
        }

        private void SelectEntity()
        {
            Point viewOrigin = ViewControl.PointToScreen( new Point( 256, 256 ) );

            Vector2d pos = new Vector2d( 
                ( Cursor.Position.X - viewOrigin.X ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraX,
                ( Cursor.Position.Y - viewOrigin.Y ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraY );

            foreach ( Entity ent in myCurrentMap.Chunk.Entities )
            {
                if ( ent.IsIntersecting( pos ) )
                {
                    SelectEntity( ent );
                    return;
                }
            }

            DeselectEntity();
        }

        private void SelectEntity( Entity ent )
        {
            if ( mySelectedEntity != null )
                mySelectedEntity.Selected = false;

            mySelectedEntity = ent;
            ent.Selected = true;
            EntityPG.SelectedObject = ent;
            EntityPG.Enabled = true;
        }

        private void DeselectEntity()
        {
            if ( mySelectedEntity != null )
            {
                mySelectedEntity.Selected = false;
                mySelectedEntity = null;
                EntityPG.SelectedObject = null;
                EntityPG.Enabled = false;
            }
        }

        private void Save( String filePath = "" )
        {
            if ( filePath == "" )
            {
                filePath = GetSaveDestination();

                if ( filePath == "" )
                    return;
            }

            ChunkConnector[][] connectors = new ChunkConnector[ 4 ][];

            for( int i = 0; i < 4; ++ i )
            {
                connectors[ i ] = new ChunkConnector[ myConArrows[ i ].Count ];
                for ( int j = 0; j < myConArrows[ i ].Count; ++j )
                    connectors[ i ][ j ] = myConArrows[ i ][ j ].ConnectorInfo;
            }

            ChunkTemplate temp = new ChunkTemplate( myCurrentMap.Chunk, connectors, myMapTypes );

            temp.SaveToFile( filePath );

            myLastSavePath = filePath;
            Text = "Lewt Chunk Editor - " + filePath;
        }

        private String GetSaveDestination()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = ".cnk";
            dialog.AddExtension = true;
            dialog.Filter = "Chunk Files (.cnk)|*.cnk";
            dialog.Title = "Save Chunk";

            DialogResult res = dialog.ShowDialog();

            if ( res != DialogResult.OK )
                return "";

            return dialog.FileName;
        }

        private void Open( String filePath = "" )
        {
            if ( filePath == "" )
                filePath = GetLoadDestination();

            if ( filePath == "" )
                return;

            ChunkTemplate temp = new ChunkTemplate( filePath );

            myLastSavePath = filePath;
            Text = "Lewt Chunk Editor - " + filePath;

            if ( myCurrentMap != null )
                myCurrentMap.Dispose();

            myCurrentMap = new EditorMap( true, temp );

            myMapTypes = temp.MapTypes;
            
            myConArrows = new List<ConnectorArrow>[]
            {
                new List<ConnectorArrow>(),
                new List<ConnectorArrow>(),
                new List<ConnectorArrow>(),
                new List<ConnectorArrow>()
            };

            for ( int i = 0; i < 4; ++i )
            {
                ChunkConnector[] cons = temp.GetConnectors( (ConnectorFace) i );
                foreach( ChunkConnector con in cons )
                    myConArrows[ i ].Add( new ConnectorArrow( con.X, con.Y, con.Horizontal, con.Size ) );
            }

            CurToolGB.Enabled = true;
            TilePropsGB.Enabled = true;
            ChunkViewGB.Enabled = true;

            RenderMap();
        }

        private String GetLoadDestination()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".cnk";
            dialog.AddExtension = true;
            dialog.Filter = "Chunk Files (.cnk)|*.cnk";
            dialog.Title = "Save Chunk";

            DialogResult res = dialog.ShowDialog();

            if ( res != DialogResult.OK )
                return "";

            return dialog.FileName;
        }

        private void dungeonToolStripMenuItem_Click( object sender, EventArgs e )
        {
            NewDungeonDialog dialog = new NewDungeonDialog();

            if ( dialog.ShowDialog() == DialogResult.Cancel )
                return;

            if ( myCurrentMap != null )
                myCurrentMap.Dispose();

            myLastSavePath = "";
            Text = "Lewt Chunk Editor - untitled.cnk";

            CurToolGB.Enabled = true;
            TilePropsGB.Enabled = true;
            ChunkViewGB.Enabled = true;

            myCurrentMap = new EditorMap( true, dialog.ChunkWidth, dialog.ChunkHeight, dialog.ChunkSkin );

            myMapTypes = dialog.MapTypes;

            myConArrows = new List<ConnectorArrow>[]
            {
                new List<ConnectorArrow>(),
                new List<ConnectorArrow>(),
                new List<ConnectorArrow>(),
                new List<ConnectorArrow>()
            };
            
            RenderMap();
        }

        private void exteriorChunkToolStripMenuItem_Click( object sender, EventArgs e )
        {
            if ( myCurrentMap != null )
                myCurrentMap.Dispose();

            myLastSavePath = "";
            Text = "Lewt Chunk Editor - untitled.cnk";

            CurToolGB.Enabled = true;
            TilePropsGB.Enabled = true;
            ChunkViewGB.Enabled = true;

            myCurrentMap = new EditorMap( false, 16, 16, 0 );

            RenderMap();
        }

        private void ViewControl_Load( object sender, EventArgs e )
        {
            MapRenderer.SetUp( 512, 512 );
            SpriteRenderer.SetUp( 512, 512 );

            myDragImage = Res.Get<Texture>( "images_gui_dragcursor" );
            myDragSprite = new Sprite( myDragImage );

            myTileSprite = new Sprite( 16 * (int) MapRenderer.CameraScale, 16 * (int) MapRenderer.CameraScale, new Color4( 1.0f, 1.0f, 1.0f, 0.125f ) );
        }

        private void ViewControl_Paint( object sender, PaintEventArgs e )
        {
            RenderMap();
        }

        private void ViewControl_MouseDown( object sender, MouseEventArgs e )
        {
            if ( EntityPropsRB.Checked )
            {
                SelectEntity();
                if ( mySelectedEntity == null )
                    return;
            }

            if ( e.Button == MouseButtons.Left )
            {
                if ( EntityPlaceRB.Checked )
                    PlaceEntity();
                else if ( EntityPropsRB.Checked )
                {
                    myDragging = true;

                    Point viewOrigin = ViewControl.PointToScreen( new Point( 256, 256 ) );

                    myEntDragOrigin = new Vector2d( ( Cursor.Position.X - viewOrigin.X ) / ( 16.0 * MapRenderer.CameraScale ) + MapRenderer.CameraX - mySelectedEntity.OriginX,
                        ( Cursor.Position.Y - viewOrigin.Y ) / ( 16.0 * MapRenderer.CameraScale ) + MapRenderer.CameraY - mySelectedEntity.OriginY );

                    Cursor.Hide();
                }
                else
                    StartDrawing();
            }
            else
            {
                if ( EntityPropsRB.Checked )
                    EntityCM.Show( Cursor.Position );
                else
                    StartPanningView();
            }
        }

        private void ViewControl_MouseUp( object sender, MouseEventArgs e )
        {
            if ( e.Button == MouseButtons.Left )
            {
                if ( EntityPropsRB.Checked )
                {
                    myDragging = false;
                    Cursor.Show();
                }
                else
                    StopDrawing();
            }
            else
                StopPanningView();
        }

        private void ViewControl_MouseMove( object sender, MouseEventArgs e )
        {
            Point viewOrigin = ViewControl.PointToScreen( new Point( 256, 256 ) );

            Point pos = new Point()
            {
                X = (int) Math.Floor( ( Cursor.Position.X - viewOrigin.X ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraX ),
                Y = (int) Math.Floor( ( Cursor.Position.Y - viewOrigin.Y ) / ( 16 * MapRenderer.CameraScale ) + MapRenderer.CameraY )
            };

            myTileSprite.Position = new Vector2(
                ( pos.X - MapRenderer.CameraX ) * 16.0f * MapRenderer.CameraScale + 256,
                ( pos.Y - MapRenderer.CameraY ) * 16.0f * MapRenderer.CameraScale + 256 );

            if ( myDrawing )
                DrawWithCursor();

            if ( myDragging )
            {
                if ( EntityPropsRB.Checked )
                {
                    Vector2d dragPos = new Vector2d( ( Cursor.Position.X - viewOrigin.X ) / ( 16.0 * MapRenderer.CameraScale ) + MapRenderer.CameraX - myEntDragOrigin.X,
                        ( Cursor.Position.Y - viewOrigin.Y ) / ( 16.0 * MapRenderer.CameraScale ) + MapRenderer.CameraY - myEntDragOrigin.Y );

                    if ( SnapToTileCB.Checked )
                    {
                        dragPos.X = Math.Round( dragPos.X * 2.0 ) / 2.0;
                        dragPos.Y = Math.Round( dragPos.Y * 2.0 ) / 2.0;
                    }

                    mySelectedEntity.OriginX = dragPos.X;
                    mySelectedEntity.OriginY = dragPos.Y;

                    if ( mySelectedEntity is Light )
                        ( mySelectedEntity as Light ).Update();

                    myDragSprite.Position = new Vector2( e.X - 8.0f, e.Y - 8.0f );
                }
                else
                    PanViewWithCursor();
            }

            RenderMap();
        }

        private void SkinCB_CheckedChanged( object sender, EventArgs e )
        {
            SkinGB.Enabled = SkinCB.Checked;
        }

        private void SolidityCB_CheckedChanged( object sender, EventArgs e )
        {
            SolidityGB.Enabled = SolidityCB.Checked;
        }

        private void VariationCB_CheckedChanged( object sender, EventArgs e )
        {
            VariationGB.Enabled = VariationCB.Checked;
        }

        private void TilePlaceRB_CheckedChanged( object sender, EventArgs e )
        {
            TilePropsGB.Enabled = TilePlaceRB.Checked;
            TilePropsGB.Visible = true;
            EntityPlaceGB.Visible = false;
            EntityPropGB.Visible = false;
        }

        private void EntityPlaceRB_CheckedChanged( object sender, EventArgs e )
        {
            EntityPlaceGB.Enabled = EntityPlaceRB.Checked;
            TilePropsGB.Visible = false;
            EntityPlaceGB.Visible = true;
            EntityPropGB.Visible = false;
        }

        private void EntityPropsRB_CheckedChanged( object sender, EventArgs e )
        {
            EntityPropGB.Enabled = EntityPropsRB.Checked;
            TilePropsGB.Visible = false;
            EntityPlaceGB.Visible = false;
            EntityPropGB.Visible = true;
        }

        private void lightingToolStripMenuItem_Click( object sender, EventArgs e )
        {
            lightingToolStripMenuItem.Checked = !lightingToolStripMenuItem.Checked;
            smoothLightingToolStripMenuItem.Enabled = lightingToolStripMenuItem.Checked;
            RenderMap();
        }

        private void smoothLightingToolStripMenuItem_Click( object sender, EventArgs e )
        {
            smoothLightingToolStripMenuItem.Checked = Chunk.SmoothLighting = !smoothLightingToolStripMenuItem.Checked;
            myCurrentMap.ForceLightUpdate();
            RenderMap();
        }

        private void fileToolStripMenuItem_Click( object sender, EventArgs e )
        {
            saveToolStripMenuItem.Enabled = saveAsToolStripMenuItem.Enabled = myCurrentMap != null;
        }

        private void saveToolStripMenuItem_Click( object sender, EventArgs e )
        {
            Save( myLastSavePath );
        }

        private void saveAsToolStripMenuItem_Click( object sender, EventArgs e )
        {
            Save();
        }

        private void openToolStripMenuItem_Click( object sender, EventArgs e )
        {
            Open();
        }

        private void removeToolStripMenuItem_Click( object sender, EventArgs e )
        {
            if ( mySelectedEntity != null )
            {
                myCurrentMap.RemoveEntity( mySelectedEntity );
                if ( mySelectedEntity is Light )
                    myCurrentMap.UpdateLight( mySelectedEntity as Light, mySelectedEntity.IsRemoved );

                DeselectEntity();
            }
        }

        private void cloneToolStripMenuItem_Click( object sender, EventArgs e )
        {
            if ( mySelectedEntity != null )
            {
                Entity clone = Entity.Clone( mySelectedEntity );
                clone.OriginX += 0.5;
                clone.OriginY += 0.5;
                myCurrentMap.AddEntity( clone );
                if ( clone is Light )
                    myCurrentMap.UpdateLight( clone as Light );
                SelectEntity( clone );
            }
        }

        private void EntityPG_PropertyValueChanged( object s, PropertyValueChangedEventArgs e )
        {
            if ( mySelectedEntity != null )
            {
                if ( mySelectedEntity is Light )
                    myCurrentMap.ForceLightUpdate();

                RenderMap();
            }
        }
    }
}
