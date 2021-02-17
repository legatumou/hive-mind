
List<int> connectedNodes = new List<int>();
List<NodeData> connectedNodesData = new List<NodeData>();
int nodeId;
long lastPing = 0;
long lastDataUpdate = 0;
long lastPositionUpdate = 0;
Vector3D lastPosition = new Vector3D(0,0,0);
NodeData currentNodeData;
IMyTextPanel LCDPanel;
IMyProgrammableBlock coreBlock;
Vector3D position = new Vector3D(0,0,0);

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
    printMessage("");
    handleListeners();
    handleKeepalives();
    sendPing();
    sendNodeData();
    updateDroneData();
}

public Program()
{
    nodeId = generateRandomId();
    currentNodeData = new NodeData {id = nodeId, battery = 0, type = "mining", status = "running"};
    coreBlock = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName("[Drone] Core");
    LCDPanel = GridTerminalSystem.GetBlockWithName("[Drone] LCD") as IMyTextPanel;
    LCDPanel.CustomData = "" + nodeId;
    Echo("Loading drone, ID: " + nodeId);
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    setupAntenna();
    setCustomData("drone-id-" + nodeId);
}

public void setCustomData(string data)
{
    if (coreBlock != null) {
        coreBlock.CustomData = data;
    }
}

public void printMessage(string msg)
{
    string message = "";
    coreBlock = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName("[Drone] Core");
    if (LCDPanel != null && int.Parse(LCDPanel.CustomData) == nodeId) {
        List<IMyBatteryBlock> vBatteries = new List<IMyBatteryBlock>();
        GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(vBatteries, c => c.BlockDefinition.ToString().ToLower().Contains("battery"));
        message += "======== Drone Overview (ID: " + nodeId + ") ========\n";
        message += "Battery: " + Math.Round(currentNodeData.battery) + "% (" + vBatteries.Count + " batteries found)\n";
        message += "Speed: " + Math.Round(currentNodeData.speed) + "\n";
        message += "Connected: " + connectedNodes.Count + "\n";
        message += msg + "\n";
        message += "=== Drones connected ===\n";

        for (int i = 0; i < connectedNodes.Count; i++) {
            message += " => Drone ID: " + connectedNodes[i] + " (" + Math.Round(connectedNodesData[i].battery) + "% Battery), Type: " + connectedNodesData[i].type + ", Status: " + connectedNodesData[i].status + "\n";
        }
        LCDPanel.WritePublicText(message, false);
        message = "";
    }

    message += "Drone Overview (ID: " + nodeId + ")\n";
    message += "Connected: " + connectedNodes.Count + "\n";
    message += msg + "\n";
    message += "=== Drones connected ===\n";

    for (int i = 0; i < connectedNodes.Count; i++) {
        message += " => Drone ID: " + connectedNodes[i] + "\n";
    }
    Echo(message);
}

public void sendPing()
{
    if (lastPing == 0 || getTimestamp() - lastPing > 15) {
        printMessage("Pinging...");
        broadcastMessage("drone-ping-" + nodeId);
        lastPing = getTimestamp();
    }
}

public void sendNodeData()
{
    if (lastDataUpdate == 0 || getTimestamp() - lastDataUpdate > 10) {
        string[] data = { currentNodeData.battery.ToString("R"), currentNodeData.speed.ToString("R"), currentNodeData.type, currentNodeData.status };
        broadcastMessage("drone-data-" + nodeId + "_" + string.Join("_", data) );
        lastDataUpdate = getTimestamp();
    }
}

public long getTimestamp()
{
    long epochTicks = new DateTime(1970, 1, 1).Ticks;
    return ((DateTime.UtcNow.Ticks - epochTicks) / TimeSpan.TicksPerSecond);
}

public void handleKeepalives()
{
    for (int i = 0; i < connectedNodes.Count; i++) {
        if (getTimestamp() - connectedNodesData[i].keepalive > 30) {
            // Disconnect if over 30 sec timeout.
            connectedNodes.RemoveAt(i);
            connectedNodesData.RemoveAt(i);
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
    currentNodeData.battery = (storage / maxStorage) * 100;

    if (lastPositionUpdate == 0 || getTimestamp() - lastPositionUpdate > 0) {
        lastPositionUpdate = getTimestamp();
        Vector3D currentPosition = Me.GetPosition();
        currentNodeData.speed = ((currentPosition-lastPosition)*60).Length();
        lastPosition = currentPosition;
    }
}

public void getTarget(IMySensorBlock sensor)
{
    var entity = sensor.LastDetectedEntity;
    if (entity != null) {
        var position = sensor.LastDetectedEntity.GetPosition();
        // do stuff
    } else {
        
    }
}

public int getNodeIndexById(int id)
{
    for (int i = 0; i < connectedNodes.Count; i++) {
        if (connectedNodes[i] == id) {
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
        if (nodeIndex >= 0) {
            connectedNodesData[nodeIndex].battery = float.Parse(dataSplitted[1]); // battery status
            connectedNodesData[nodeIndex].speed = float.Parse(dataSplitted[2]); // battery status
            connectedNodesData[nodeIndex].type = dataSplitted[3]; // node type
            connectedNodesData[nodeIndex].status = dataSplitted[4]; // status
            Echo("Update data " + nodeIndex + ": " + data);
        } else {
            Echo("Node not found " + nodeIndex);
        }
    }
}

public void handleResponsePing(int id)
{
    if (!connectedNodes.Contains(id)) {
        connectedNodes.Add(id);
        connectedNodesData.Add(new NodeData {id = id, keepalive = getTimestamp(), battery = 0, type = "N/A", status = "Unknown"});
        printMessage("--> New drone connected: " + id);
        Echo("New drone connected: " + id);
    } else {
        connectedNodesData[getNodeIndexById(id)].keepalive = getTimestamp();
    }
}

public int generateRandomId()
{
    Random rnd = new Random();
    return rnd.Next(0, 100000);
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

class NodeData
{
    public int id { get; set; }
    public long keepalive { get; set; }
    public float battery { get; set; }
    public double speed { get; set; }
    public string type { get; set; }
    public string status { get; set; }

}
