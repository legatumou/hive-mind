
int nodeId;
long lastPing = 0;
long lastDataUpdate = 0;
long lastPositionUpdate = 0;
IMyTextPanel LCDPanel;
Vector3D lastPosition = new Vector3D(0,0,0);

public void Save()
{
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means.
    //
    // This method is optional and can be removed if not
    // needed.
}

public void Main()
{
    Communication.currentNode.execute();
    Display.print("");
    handleListeners();
    handleKeepalives();
    sendPing();
    sendNodeData();
    updateDroneData();
    handleCommands();
}

public Program()
{
    // @TODO, Validations to make sure all blocks are connected.
    nodeId = generateRandomId();
    Display.myGrid = this;
    Display.fetchOutputDevices();
    Navigation navHandle = new Navigation(this);
    navHandle.updateRemoteControls();
    Communication.currentNode = new Replicator(nodeId);
    Communication.currentNode.type = "mining";
    Communication.currentNode.setNavigation(navHandle);
    Communication.currentNode.myGrid = this;
    Communication.coreBlock = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName("[Drone] Core");
    LCDPanel = GridTerminalSystem.GetBlockWithName("[Drone] LCD") as IMyTextPanel;
    if (LCDPanel != null) {
        LCDPanel.CustomData = "" + nodeId;
    }
    Echo("Loading drone, ID: " + nodeId);
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    setupAntenna();
    setCustomData("drone-id-" + nodeId);
}

public void handleCommands()
{
    Communication.currentNode.process(this);
}

public void setCustomData(string data)
{
    if (Communication.coreBlock != null) {
        Communication.coreBlock.CustomData = data;
    }
}

public void sendPing()
{
    if (lastPing == 0 || Communication.getTimestamp() - lastPing > 15) {
        Display.print("Pinging...");
        broadcastMessage("drone-ping-" + nodeId);
        lastPing = Communication.getTimestamp();
    }
}

public void sendNodeData()
{
    if (lastDataUpdate == 0 || Communication.getTimestamp() - lastDataUpdate > 10) {
        string[] data = { Communication.currentNode.battery.ToString("R"), Communication.currentNode.speed.ToString("R"), Communication.currentNode.type, Communication.currentNode.status };
        broadcastMessage("drone-data-" + nodeId + "_" + string.Join("_", data) );
        lastDataUpdate = Communication.getTimestamp();
    }
}

public void handleKeepalives()
{
    for (int i = 0; i < Communication.connectedNodes.Count; i++) {
        if (Communication.getTimestamp() - Communication.connectedNodesData[i].keepalive > 30) {
            // Disconnect if over 30 sec timeout.
            Communication.connectedNodes.RemoveAt(i);
            Communication.connectedNodesData.RemoveAt(i);
        }
    }
}

public string getDetailedInfoValue(IMyBatteryBlock block, string name)
{
    string value = "";
    string[] lines = block.DetailedInfo.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    for (int i = 0; i < lines.Length; i++)
    {
        string[] line = lines[i].Split(':');
        if (line[0].Equals(name))
        {
            value = line[1].Substring(1);
            break;
        }
    }
    return value;
}

public int getPowerAsInt(string text)
{
    if (String.IsNullOrWhiteSpace(text)) {
        return 0;
    }
    string[] values = text.Split(' ');
    if (values[1].Equals("kW")) {
        return (int) (float.Parse(values[0])*1000f);
    } else if (values[1].Equals("kWh")) {
        return (int) (float.Parse(values[0])*1000f);
    } else if (values[1].Equals("MW")) {
        return (int) (float.Parse(values[0])*1000000f);
    } else if (values[1].Equals("MWh")) {
        return (int) (float.Parse(values[0])*1000000f);
    } else if (values[1].Equals("GW")) {
        return (int) (float.Parse(values[0])*1000000000f);
    } else if (values[1].Equals("GWh")) {
        return (int) (float.Parse(values[0])*1000000000f);
    } else {
        return (int) float.Parse(values[0]);
    }
}


public void updateDroneData() {
    List<IMyBatteryBlock> vBatteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(vBatteries, c => c.BlockDefinition.ToString().ToLower().Contains("battery"));

    float maxStorage = 0;
    float storage = 0;

    foreach (IMyBatteryBlock block in vBatteries) {
        maxStorage += getPowerAsInt(getDetailedInfoValue(block, "Max Stored Power"));
        storage += getPowerAsInt(getDetailedInfoValue(block, "Stored power"));
    }
    Communication.currentNode.battery = (storage / maxStorage) * 100;

    if (lastPositionUpdate == 0 || Communication.getTimestamp() - lastPositionUpdate > 0) {
        lastPositionUpdate = Communication.getTimestamp();
        Vector3D currentPosition = Me.GetPosition();
        Communication.currentNode.speed = ((currentPosition-lastPosition)*60).Length();
        lastPosition = currentPosition;
    }
}

public int getNodeIndexById(int id)
{
    for (int i = 0; i < Communication.connectedNodes.Count; i++) {
        if (Communication.connectedNodes[i] == id) {
            return i;
        }
    }

    return -1;
}

public void handleListeners()
{
    var listens = new List<IMyBroadcastListener>();
    IGC.GetBroadcastListeners( listens );

    for( int i=0; i<listens.Count; ++i ) {
        while( listens[i].HasPendingMessage ) {
            var msg = listens[i].AcceptMessage();
            if( msg.Data.ToString().Substring(0, 10) == "drone-ping" ) {
                int id = int.Parse(msg.Data.ToString().Substring(11));
                handleResponsePing(id);
            } else if ( msg.Data.ToString().Substring(0, 10) == "drone-data" ) {
                string data = msg.Data.ToString().Substring(11);
                handleResponseData(data);
            }
        }
    }
}

public void handleResponseData(string data)
{
    string[] dataSplitted = data.Split('_');
    if (dataSplitted.Count() >= 2) {
        int id = int.Parse(dataSplitted[0]);
        int nodeIndex = getNodeIndexById(id);
        if (nodeIndex == -1) {
            Communication.connectedNodes.Add(id);
            Communication.connectedNodesData.Add(new NodeData(id));
            nodeIndex = getNodeIndexById(id);
        }
        Communication.connectedNodesData[nodeIndex].battery = float.Parse(dataSplitted[1]); // battery status
        Communication.connectedNodesData[nodeIndex].speed = float.Parse(dataSplitted[2]); // battery status
        Communication.connectedNodesData[nodeIndex].type = dataSplitted[3]; // node type
        Communication.connectedNodesData[nodeIndex].status = dataSplitted[4]; // status
    }
}

public void handleResponsePing(int id)
{
    Echo("Response ping validation: " + id);
    if (!Communication.connectedNodes.Contains(id)) {
        Echo("Adding drone: " + id);
        Communication.connectedNodes.Add(id);
        Communication.connectedNodesData.Add(new NodeData(id));
        Display.print("--> New drone connected: " + id);
        Echo("New drone connected: " + id);
    } else {
        Echo("Updating drone: " + id);
        Communication.connectedNodesData[getNodeIndexById(id)].keepalive = Communication.getTimestamp();
    }
}

public int generateRandomId()
{
    Random rnd = new Random();
    return rnd.Next(0, 10000);
}

public void broadcastMessage(string messageOut) {
    // Create a tag. Our friend will use this in his script in order to receive our messages.
    string tag1 = "drone-channel";
    // Through the IGC variable we issue the broadcast method. IGC is "pre-made",
    // so we don't have to declare it ourselves, just go ahead and use it.
    IGC.SendBroadcastMessage(tag1, messageOut);
}

public void setupAntenna()
{
    // Create a tag. Our friend will use this in his script in order to receive our messages.
    string tag1 = "drone-channel";

    // To create a listener, we use IGC to access the relevant method.
    // We pass the same tag argument we used for our message.
    IGC.RegisterBroadcastListener(tag1);
}


// Classes

public class Display
{
    public static List<IMyTextPanel> LCD = new List<IMyTextPanel>();
    public static List<IMyTextPanel> TextPanels = new List<IMyTextPanel>();
    public static List<IMyCockpit> Cockpits = new List<IMyCockpit>();
    public static MyGridProgram myGrid;

    public static void fetchOutputDevices()
    {
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(Display.TextPanels, c => c.BlockDefinition.ToString().ToLower().Contains("text"));
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Display.Cockpits, c => c.BlockDefinition.ToString().ToLower().Contains("cockpit"));
    }

    public static void print(string extraMsg)
    {
        string msg = Display.generateMessage(extraMsg);

        // TextPanels
        foreach (IMyTextPanel panel in Display.TextPanels) {
            if (panel.CustomName.Contains("[Drone]")) {
                panel.WriteText(msg, false);
            }
        }

        Display.myGrid.Echo(msg);
    }

    public static string generateMessage(string msg)
    {
        string message = "";
        List<IMyBatteryBlock> vBatteries = new List<IMyBatteryBlock>();
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(vBatteries, c => c.BlockDefinition.ToString().ToLower().Contains("battery"));
        message += "=== Drone Overview (ID: " + Communication.currentNode.id + ") ===\n";
        message += "Battery: " + Math.Round(Communication.currentNode.battery) + "% (" + vBatteries.Count + " batteries found)\n";
        message += "Speed: " + Math.Round((Communication.currentNode.speed / 100), 3) + "\n";
        message += "Status: " + Communication.currentNode.status + ", Connected: " + Communication.connectedNodes.Count + "\n";
        if (Communication.currentNode.nearbyEntities != null && Communication.currentNode.nearbyEntities.Count() > 0) {
            message += " ==> Nearby entities (" + Communication.currentNode.nearbyEntities.Count() + " found)\n";
            for (int i = 0; i < Communication.currentNode.nearbyEntities.Count; i++) {
                if (i > 10) break;
                message += " => " + Communication.currentNode.nearbyEntities[i].name + " (Distance: " + Communication.currentNode.nearbyEntities[i].distance + ")" + "\n";
            }
        }
        message += msg + "\n";
        message += "=== Drones connected ===\n";

        for (int i = 0; i < Communication.connectedNodes.Count; i++) {
            message += " ==> Drone ID: " + Communication.connectedNodes[i] + "\n";
            message += " => Battery" + Math.Round(Communication.connectedNodesData[i].battery) + "%" + "\t";
            message += " => Type: " + Communication.connectedNodesData[i].type + "\t";
            message += " => Status: " + Communication.connectedNodesData[i].status + "\n";
            if (Communication.connectedNodesData[i].nearbyEntities != null && Communication.connectedNodesData[i].nearbyEntities.Count() > 0) {
                message += " => Nearby entities (" + Communication.connectedNodesData[i].nearbyEntities.Count() + " found)\n";
                for (int n = 0; n < Communication.currentNode.nearbyEntities.Count; n++) {
                    if (n > 10) break;
                    message += " => " + Communication.connectedNodesData[i].nearbyEntities[n].name + " (Distance: " + Communication.connectedNodesData[i].nearbyEntities[n].distance + ")" + "\n";
                }
            }
        }

        return message;
    }
}

public class Navigation
{
    public MyGridProgram myGrid;
    public List<IMyRemoteControl> remotes { get; set; }
    public Vector3D lastWaypoint;

    public Navigation(MyGridProgram myGrid) {
        this.myGrid = myGrid;
    }

    public void updateRemoteControls() {
        this.remotes = new List<IMyRemoteControl>();
        List<IMyRemoteControl> handles = new List<IMyRemoteControl>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(handles, c => c.BlockDefinition.ToString().ToLower().Contains("remote"));
        foreach (IMyRemoteControl handle in handles) {
            if (handle.CustomName.Contains("[Drone]")) {
                this.remotes.Add(handle);
            }
        }
    }

    public void setAutopilotStatus(bool autoPilotStatus) {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.SetAutoPilotEnabled(autoPilotStatus);
        }
    }

    public void move(Vector3D coords, string waypointName) {
        this.clearPath();
        this.lastWaypoint = coords;
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.AddWaypoint(coords, waypointName);
            remote.SetAutoPilotEnabled(true);
        }
    }

    public void clearPath() {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.ClearWaypoints();
        }
    }
}

public class Communication
{
    public static List<int> connectedNodes = new List<int>();
    public static List<NodeData> connectedNodesData = new List<NodeData>();
    public static Replicator currentNode;
    public static IMyProgrammableBlock coreBlock;

    public static long getTimestamp() {
        long epochTicks = new DateTime(1970, 1, 1).Ticks;
        return ((DateTime.UtcNow.Ticks - epochTicks) / TimeSpan.TicksPerSecond);
    }
}

public class DetectedEntity
{
    public DetectedEntity()
    {

    }

    public long id { get; set; }
    public double distance { get; set; }
    public string name { get; set; }
    public Vector3D position { get; set; }
    public MyDetectedEntityType type { get; set; }
}

public class NodeData
{
    public NodeData(int id)
    {
        this.id = id;
        this.battery = 0;
        this.speed = 0.0;
        this.type = "N/A";
        this.status = "init";
        this.keepalive = Communication.getTimestamp();
        this.nearbyEntities = new List<DetectedEntity>();
    }
    public MyGridProgram myGrid;

    public int id { get; set; }
    public long keepalive { get; set; }
    public float battery { get; set; }
    public double speed { get; set; }
    public string type { get; set; }
    public string status { get; set; }
    public List<DetectedEntity> nearbyEntities { get; set; }
    public Navigation navHandle;

    public void setNavigation(Navigation navHandle) {
        this.navHandle = navHandle;
    }

    public double getDistanceFrom(Vector3D pos, Vector3D pos2) {
        return Math.Round( Vector3D.Distance( pos, pos2 ), 2 );
    }

    public Vector3D getShipPosition() {
        List<IMySensorBlock> sensors = new List<IMySensorBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, c => c.BlockDefinition.ToString().ToLower().Contains("sensor"));
        foreach (IMySensorBlock sensor in sensors) {
            if (sensor.CustomName.Contains("[Drone]")) {
                return sensor.GetPosition();
            }
        }
        return new Vector3D();
    }

    public void updateNearbyCollision(IMySensorBlock sensor)
    {
        if (!sensor.LastDetectedEntity.IsEmpty()) {
            MyDetectedEntityInfo entity = sensor.LastDetectedEntity;
            if (!this.nearbyEntities.Any(val => val.id == entity.EntityId)) {
                DetectedEntity tmp = new DetectedEntity();
                tmp.id = entity.EntityId;
                tmp.name = entity.Name;
                tmp.position = entity.Position;
                tmp.distance = this.getDistanceFrom(entity.Position, sensor.GetPosition());
                tmp.type = entity.Type;
                this.nearbyEntities.Add(tmp);
            } else {
                for (int i = 0; i < this.nearbyEntities.Count; i++) {
                    DetectedEntity nearEntity = this.nearbyEntities[i];
                    if (nearEntity.id == entity.EntityId) {
                        DetectedEntity tmp = new DetectedEntity();
                        tmp.id = entity.EntityId;
                        tmp.name = entity.Name;
                        tmp.position = entity.Position;
                        tmp.distance = this.getDistanceFrom(entity.Position, sensor.GetPosition());
                        tmp.type = entity.Type;
                        this.nearbyEntities[i] = tmp;
                        break;
                    }
                }
            }
        } else {
            this.nearbyEntities = new List<DetectedEntity>();
        }
    }

    public Vector3D getTarget() {
        return new Vector3D();
    }

    public void execute(){}

    public void process(MyGridProgram grid)
    {
        this.myGrid = grid;
        List<IMySensorBlock> sensors = new List<IMySensorBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, c => c.BlockDefinition.ToString().ToLower().Contains("sensor"));
        foreach (IMySensorBlock sensor in sensors) {
            if (sensor.CustomName.Contains("[Drone]")) {
                this.updateNearbyCollision(sensor);
            }
        }
    }

    public bool findFriends() {
        // Find a friendly drone.
        double closestDistance = 100000.0;
        double distance;
        NodeData closest = new NodeData(0);

        for (int i = 0; i < Communication.connectedNodesData.Count; i++) {
            NodeData node = Communication.connectedNodesData[i];
            distance = this.getDistanceFrom(node.getShipPosition(), Communication.coreBlock.GetPosition());
            if (distance < closestDistance && distance > 50) { // not too close ;)
                closestDistance = distance;
                closest = node;
            }
        }
        if (closest.id > 0) {
            this.status = "running-to-friend";
            this.navHandle.move(closest.getShipPosition(), "running-to-friend");
            return true;
        }
        return false;
    }

    public void startDrills() {
        List<IMyShipDrill> drills = new List<IMyShipDrill>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills);
        foreach (IMyShipDrill drill in drills) {
            drill.Enabled = true;
        }
    }

    public void haltDrills() {
        List<IMyShipDrill> drills = new List<IMyShipDrill>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills);
        foreach (IMyShipDrill drill in drills) {
            drill.Enabled = false;
        }
    }
}

public class Replicator : NodeData
{
    long lastIdleCommand = 0;

    public Replicator(int id) : base(id) {}

    public void handleIdle() {
        bool foundFriend = this.findFriends();
        if (this.lastIdleCommand == 0) this.lastIdleCommand = Communication.getTimestamp();

        if (foundFriend == false && (Communication.getTimestamp() - this.lastIdleCommand) > 30) {
            this.lastIdleCommand = Communication.getTimestamp();
            // Find ore
            this.status = "finding-asteroids";
            Vector3D newPos = Communication.coreBlock.GetPosition();
            // Random position
            Random rnd = new Random();
            newPos.X += (int) rnd.Next(0, 1000);
            newPos.Y += (int) rnd.Next(0, 1000);
            newPos.Z += (int) rnd.Next(0, 1000);
            this.navHandle.move(newPos, "cruising");
        }
    }

    public void execute() {
        DetectedEntity target = this.getTarget();

        if (target.id > 0) {
            // Move to closest ore.
            this.navHandle.move(target.position, "navigate-to-ore");
            this.status = "target-acquired";
            this.startDrills();
        } else {
            this.haltDrills();
            this.handleIdle();
        }
    }

    public DetectedEntity getTarget()
    {
        DetectedEntity closest = new DetectedEntity();
        double closestDistance = 3000;
        double targetDistance;
        this.myGrid.Echo("Nearby entities: " + this.nearbyEntities.Count + "\n");
        foreach (DetectedEntity entity in this.nearbyEntities) {
            // Filter out non asteroids.
            if (entity.name != "Asteroid") continue;
            targetDistance = this.getDistanceFrom(this.getShipPosition(), entity.position);
            if (targetDistance < closestDistance) {
                closest = entity;
                closestDistance = targetDistance;
            }
        }
        return closest;
    }
}
