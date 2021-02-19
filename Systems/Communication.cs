public class Communication
{
    public static List<int> connectedNodes = new List<int>();
    public static List<NodeData> connectedNodesData = new List<NodeData>();
    public static DrillingDrone currentNode;
    public static IMyProgrammableBlock coreBlock;


    private long lastPing = 0;
    private long lastDataUpdate = 0;
    private MyGridProgram myGrid;

    public Communication(MyGridProgram myGrid) {
        this.myGrid = myGrid;
    }

    public static long getTimestamp() {
        long epochTicks = new DateTime(1970, 1, 1).Ticks;
        return ((DateTime.UtcNow.Ticks - epochTicks) / TimeSpan.TicksPerSecond);
    }

    public void sendPing() {
        if (this.lastPing == 0 || Communication.getTimestamp() - this.lastPing > 15) {
            Display.print("Pinging...");
            this.broadcastMessage("drone-ping-" + Communication.currentNode.id);
            this.lastPing = Communication.getTimestamp();
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

    public void sendNodeData() {
        if (this.lastDataUpdate == 0 || Communication.getTimestamp() - this.lastDataUpdate > 10) {
            string[] data = {
                Communication.currentNode.battery.ToString("R"),
                Communication.currentNode.speed.ToString("R"),
                Communication.currentNode.type,
                Communication.currentNode.status,
                /*this.myGrid.Me.EntityId.ToString("R")*/"0"
            };
            this.broadcastMessage("drone-data-" + Communication.currentNode.id + "_" + string.Join("_", data) );
            this.lastDataUpdate = Communication.getTimestamp();
        }
    }

    public void broadcastMessage(string messageOut) {
        string tag1 = "drone-channel";
        this.myGrid.IGC.SendBroadcastMessage(tag1, messageOut);
    }

    public void setupAntenna() {
        string tag1 = "drone-channel";
        this.myGrid.IGC.RegisterBroadcastListener(tag1);
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
        this.myGrid.IGC.GetBroadcastListeners( listens );

        for( int i=0; i<listens.Count; ++i ) {
            while( listens[i].HasPendingMessage ) {
                var msg = listens[i].AcceptMessage();
                if( msg.Data.ToString().Substring(0, 10) == "drone-ping" ) {
                    int id = int.Parse(msg.Data.ToString().Substring(11));
                    this.handleResponsePing(id);
                } else if ( msg.Data.ToString().Substring(0, 10) == "drone-data" ) {
                    string data = msg.Data.ToString().Substring(11);
                    this.handleResponseData(data);
                }
            }
        }
    }

    public void handleResponseData(string data)
    {
        string[] dataSplitted = data.Split('_');
        if (dataSplitted.Count() >= 2) {
            int id = int.Parse(dataSplitted[0]);
            int nodeIndex = this.getNodeIndexById(id);
            if (nodeIndex == -1) {
                Communication.connectedNodes.Add(id);
                NodeData node = new NodeData(id);
                node.initNavigation(this.myGrid);
                Communication.connectedNodesData.Add(node);
                nodeIndex = this.getNodeIndexById(id);
            }
            Communication.connectedNodesData[nodeIndex].battery = float.Parse(dataSplitted[1]); // battery status
            Communication.connectedNodesData[nodeIndex].battery = float.Parse(dataSplitted[1]); // battery status
            Communication.connectedNodesData[nodeIndex].speed = float.Parse(dataSplitted[2]); // battery status
            Communication.connectedNodesData[nodeIndex].type = dataSplitted[3]; // node type
            Communication.connectedNodesData[nodeIndex].status = dataSplitted[4]; // status
            Communication.connectedNodesData[nodeIndex].entityId = long.Parse(dataSplitted[5]); // entityId
        }
    }

    public void handleResponsePing(int id)
    {
        this.myGrid.Echo("Response ping validation: " + id);
        if (!Communication.connectedNodes.Contains(id)) {
            this.myGrid.Echo("Adding drone: " + id);
            Communication.connectedNodes.Add(id);
            NodeData node = new NodeData(id);
            node.initNavigation(this.myGrid);
            Communication.connectedNodesData.Add(node);
            Display.print("--> New drone connected: " + id);
            this.myGrid.Echo("New drone connected: " + id);
        } else {
            this.myGrid.Echo("Updating drone: " + id);
            Communication.connectedNodesData[this.getNodeIndexById(id)].keepalive = Communication.getTimestamp();
        }
    }
}
