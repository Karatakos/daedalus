namespace Dungen;

public class RoomTemplateFactory {
    private static RoomTemplateFactory Instance { get; set; }

    private HashSet<Room> _templates;

    private Dictionary<RoomType, List<Room>> _templatesByType;

    private RoomTemplateFactory() {
        _templates = new HashSet<Room>();
        _templatesByType = new Dictionary<RoomType, List<Room>>();
    }

    public static void Register(RoomDefinition definition) {
        if (Instance == null)
            Instance = new RoomTemplateFactory();

        foreach (RoomBlueprint blueprint in definition.Blueprints) {
            Room tmp = new Room(blueprint, definition.Type);
            if (!Instance._templatesByType.ContainsKey(definition.Type))
                Instance._templatesByType.Add(definition.Type, new List<Room>());

            if (!Instance._templates.Contains(tmp))
                Instance._templates.Add(tmp);

            if (!Instance._templatesByType[definition.Type].Contains(tmp))
                Instance._templatesByType[definition.Type].Add(tmp);
        }
    }

    public static Room VendRandomTemplate(int roomId, RoomType type, Room exclude = null) {
        if (Instance == null || !Instance._templatesByType.TryGetValue(type, out List<Room> tmp))
            throw new Exception("Room definition provided has not been registed for any vertex");

        List<Room> rooms = new List<Room>();
        if (exclude != null) {
            foreach (Room r in tmp) 
                rooms.Add(r);
        }   
        else
            rooms = tmp;

        Random rnd = new Random();
        int index = rnd.Next(0, rooms.Count);
        
        // Associate the corresponding vertex ID in the graph
        //
        Room newRoom = new Room(rooms[index]); 
        newRoom.Number = roomId;

        return newRoom;
    }

    public static Room[] GetAllTemplates() {
        if (Instance == null)
            throw new Exception("No templates have been initialized");

        // TODO: Remove collection and replace with _templatesByType.Distinct() call
        //
        return Instance._templates.ToArray();
    }
}