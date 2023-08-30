using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections.Generic;

using Dungen.Generator;

namespace Dungen.Client;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _texture;
    private Texture2D _tLine;
    private DungeonLayout _dungeon;
    private SpriteFont _font;
    private float _doorWidth = 10f;
    private float _doorGapMin = 2f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        _dungeon = null;
    }

    protected override void Initialize()
    {
        _font = Content.Load<SpriteFont>("Fonts/Playfair");

        DungeonGraph graph = InitializeDungeon();
        _dungeon = GenerateDungeon(graph);

        base.Initialize();
    }

    private DungeonGraph InitializeDungeon() {
        float x = 50/2;
        float y = 30/2;
        
        // Rectangular normal room 
        //
        RoomBlueprint normalRoomT1 = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(x, y), 
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)}));

        x = 30/2;
        y = 50/2;
        
        // Rectangular normal room rotated 90d
        //
        RoomBlueprint normalRoomT3 = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(x, y), 
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)}));

        x = 50/2;
        y = 50/2;

        // Square normal room 
        //
        RoomBlueprint normalRoomT4 = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(x, y), 
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)}));

        x = 80/2;
        y = 80/2;

        // Square boss room 
        //
        RoomBlueprint areanaRoomT = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(x, y), 
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)}));

        x = 50/2;
        y = 50/2;

        // Hexagonal (L shaped) normal room with rotations and scaling support
        //
        RoomBlueprint normalRoomT2 = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(0, y), 
                    new Vector2F(0, 0),
                    new Vector2F(x, 0),
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)}));

        x = 70/2;
        y = (_doorWidth + (_doorGapMin * 2))/2;

        // Short rectangular Corridor (door's width) supporting doors at either end and two rotations
        //
        RoomBlueprint corridorRoomT = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(x, y), 
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)})/*,
            doors: new DoorContraint()
                .AddConstraintLine(new Vector2F(x, y), new Vector2F(x, -y))
                .AddConstraintLine(new Vector2F(-x, -y), new Vector2F(-x, y))*/);

        x = (_doorWidth + (_doorGapMin * 2))/2;
        y = 70/2;

        // Short rectangular Corridor (door's width) rotated 90d
        //
        RoomBlueprint corridorRoomT2 = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(x, y), 
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)})/*,
            doors: new DoorContraint()
                .AddConstraintLine(new Vector2F(-x, y), new Vector2F(x, y))
                .AddConstraintLine(new Vector2F(x, -y), new Vector2F(-x, -y))*/);

        RoomDefinition entrance = new RoomDefinition(
            blueprints: new List<RoomBlueprint>() {normalRoomT1},
            type: RoomType.Entrance);

        RoomDefinition normal = new RoomDefinition( 
            blueprints: new List<RoomBlueprint>() {
                normalRoomT1, 
                normalRoomT2, 
                normalRoomT3, 
                normalRoomT4},
            type: RoomType.Normal);

        RoomDefinition arena = new RoomDefinition(
            blueprints: new List<RoomBlueprint>() {areanaRoomT},
            type: RoomType.Arena);

        RoomDefinition corridor = new RoomDefinition(
            blueprints: new List<RoomBlueprint>() {corridorRoomT, corridorRoomT2},
            type: RoomType.Corridor);

        DungeonGraph graph = new DungeonGraph();

        graph.AddRoom(0, entrance);
        graph.AddRoom(1, normal);
        graph.AddRoom(2, normal);
        graph.AddRoom(3, normal);
        graph.AddRoom(4, normal);
        graph.AddRoom(5, normal);
        graph.AddRoom(6, normal);
        graph.AddRoom(7, normal);
        graph.AddRoom(8, normal);
        graph.AddRoom(9, normal);
        graph.AddRoom(10, normal);
        graph.AddRoom(11, arena);
        graph.AddRoom(12, normal);
        graph.AddRoom(13, normal);
        graph.AddRoom(14, normal);
        graph.AddRoom(15, normal);
        graph.AddRoom(16, entrance);
        graph.AddRoom(17, normal);
        graph.AddRoom(18, corridor);

        graph.AddConnection(0, 2);
        graph.AddConnection(2, 5);
        graph.AddConnection(5, 10, Direction.Uni);  // Demonstrating 1-way loop
        graph.AddConnection(10, 9, Direction.Uni);  //
        graph.AddConnection(9, 1, Direction.Uni);   //
        graph.AddConnection(1, 2, Direction.Uni);   //
        graph.AddConnection(5, 6);
        graph.AddConnection(0, 3);
        graph.AddConnection(3, 6);
        graph.AddConnection(6, 11);
        graph.AddConnection(11, 14);
        graph.AddConnection(14, 15);
        graph.AddConnection(15, 17);
        graph.AddConnection(17, 11);
        graph.AddConnection(17, 18);
        graph.AddConnection(18, 12);
        graph.AddConnection(12, 7);
        graph.AddConnection(7, 4);
        graph.AddConnection(4, 8);
        graph.AddConnection(8, 13);
        graph.AddConnection(13, 16);
        graph.AddConnection(16, 12);

        return graph;
    }

    private DungeonLayout GenerateDungeon(DungeonGraph graph) {
        DungeonLayout dungeon = null;
        try {
            DungeonGeneratorProps props = new DungeonGeneratorProps();
            props.DoorWidth = _doorWidth;
            props.DoorToCornerMinGap = _doorGapMin;
            props.Graph = graph;
            props.TargetSolutions = 1;

            DungeonGenerator generator = new DungeonGenerator(props);

            // Compute config spaces, validate planar graph, etc.
            //
            generator.Initialize(); 

            // Tries to generate n dungeons
            //
            if (generator.TryGenerate()) {
                var sceneCenter = new Vector2F(
                    _graphics.GraphicsDevice.Viewport.Bounds.Width / 2,
                    _graphics.GraphicsDevice.Viewport.Bounds.Height / 2);

                // Warnin: Not a clone
                //
                dungeon = generator.Vend();

                // Transform to scene center
                //
                foreach (Room room in dungeon.Rooms) 
                    room.Translate(sceneCenter);
            }
            else {
                Console.WriteLine("Not able to find a solution to the input graph.");
            }
        }
        catch (Exception e) { 
            Console.WriteLine(e); 
        }

        return dungeon;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _tLine = new Texture2D(GraphicsDevice, 1, 1);
        _tLine.SetData(new Color[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        var screenCenter = new Vector2(
            _graphics.GraphicsDevice.Viewport.Bounds.Width / 2,
            _graphics.GraphicsDevice.Viewport.Bounds.Height / 2);

        _spriteBatch.Begin();

        if (_dungeon != null && _dungeon.Rooms.Length > 0) 
            DrawDungeon(_dungeon, Color.Black);
        else 
            DrawString("Failed.", screenCenter, 1.5f);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    void DrawDungeon(DungeonLayout dungeon, Color color, int width = 1) {
        foreach (Room room in dungeon.Rooms) {
            DrawRoom(room, color, width);
        }
    }

    void DrawString(string s, Vector2 position, float scale) {
        Vector2 textCenter = _font.MeasureString(s) / 2;

        _spriteBatch.DrawString(
            _font, 
            s, 
            position,
            Color.Brown,
            0,
            textCenter,
            scale,
            SpriteEffects.None,
            0.5f);
    }

    void DrawRoom(Room room, Color color, int width = 1) {
        Vector2F pTmp = room.GetCenter();
        DrawString(
            String.Format("{0} {1}", room.Number.ToString(), room.Type.ToString().Substring(0, 2)),
            new Vector2(pTmp.x, pTmp.y),
            0.8f);

        foreach (BoundaryLine line in room.Boundary) {
            Vector2 start = new Vector2((int)line.Start.x, (int)line.Start.y);
            Vector2 end = new Vector2((int)line.End.x, (int)line.End.y);

            Color c = color;
            if (line.IsDoor) {
                Door door = room.GetDoorForLine(ValueTuple.Create<Vector2F, Vector2F>(line.Start, line.End));

                if (door.DefaultAccess == DefaultDoorAccess.Inaccessible) {
                    Vector2F lineCenter = (line.Start + line.End) / 2;

                    float angle = -90 * (float)(Math.PI/180);

                    Vector2F rotStart = 
                        new Vector2F(
                            ((line.Start.x - lineCenter.x) * (float)Math.Cos(angle)) + 
                                ((line.Start.y - lineCenter.y) * (float)Math.Sin(angle)) + lineCenter.x,
                            (-(line.Start.x - lineCenter.x) * (float)Math.Sin(angle)) + 
                                ((line.Start.y - lineCenter.y) * (float)Math.Cos(angle)) + lineCenter.y);

                    Vector2F rotEnd = 
                        new Vector2F(
                            ((line.End.x - lineCenter.x) * (float)Math.Cos(angle)) + 
                                ((line.End.y - lineCenter.y) * (float)Math.Sin(angle)) + lineCenter.x,
                            (-(line.End.x - lineCenter.x) * (float)Math.Sin(angle)) + 
                                ((line.End.y - lineCenter.y) * (float)Math.Cos(angle)) + lineCenter.y);


                    Vector2 tmpStart = new Vector2((int)rotStart.x, (int)rotStart.y);
                    Vector2 tmpEnd = new Vector2((int)rotEnd.x, (int)rotEnd.y);

                    DrawLine(_spriteBatch, //draw line
                        tmpStart, //start of line
                        tmpEnd, //end of line
                        Color.Red,
                        width
                    );

                    float dA = Vector2F.Magnitude2(rotStart - lineCenter);
                    float dB = Vector2F.Magnitude2(rotEnd - lineCenter);

                    Vector2 head = tmpStart;
                    if (dA < dB) 
                        head = tmpEnd;

                    DrawLine(_spriteBatch, //draw line
                        head, //start of line
                        head, //end of line
                        Color.Red,
                        width+3
                    );
                }
                
                continue;
            }

            DrawLine(_spriteBatch, //draw line
                start, //start of line
                end, //end of line
                c,
                width
            );
        }
    }

    void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, int width = 1)
    {
        Vector2 edge = end - start;

        edge = edge.X == 0 && edge.Y == 0 ? new Vector2(1, 1) : edge;

        // calculate angle to rotate line
        float angle =
            (float)Math.Atan2(edge.Y , edge.X);

        sb.Draw(_tLine,
            new Rectangle(// rectangle defines shape of line and position of start of line
                (int)start.X,
                (int)start.Y,
                (int)edge.Length(), //sb will strech the texture to fill this rectangle
                width), //width of line, change this to make thicker line
            null,
            color, //colour of line
            angle,     //angle of line (calulated above)
            new Vector2(0, 0), // point in line about which to rotate
            SpriteEffects.None,
            0);
    }
}
