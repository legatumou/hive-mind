
int nodeId;
IMyTextPanel LCDPanel;
Communication commHandle;
Core coreHandle;

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
    Display.print("");
    handleListeners();
    handleKeepalives();
    coreHandle.updateDroneData();
    commHandle.sendPing();
    commHandle.sendNodeData();
    coreHandle.execute();
}

public Program()
{
    // @TODO, Validations to make sure all blocks are connected.
    nodeId = generateRandomId();
    Display.myGrid = this;
    Display.fetchOutputDevices();
    commHandle = new Communication(this);
    coreHandle = new Core(this);
    Navigation navHandle = new Navigation(this);
    navHandle.updateRemoteControls();
    Communication.currentNode = new ReplicatorDrone(nodeId);
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
    commHandle.setupAntenna();
    setCustomData("drone-id-" + nodeId);
}

public void setCustomData(string data)
{
    if (Communication.coreBlock != null) {
        Communication.coreBlock.CustomData = data;
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
