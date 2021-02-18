public class Communication
{
    public static List<int> connectedNodes = new List<int>();
    public static List<NodeData> connectedNodesData = new List<NodeData>();
    public static ReplicatorDrone currentNode;
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

    public void sendNodeData() {
        if (this.lastDataUpdate == 0 || Communication.getTimestamp() - this.lastDataUpdate > 10) {
            string[] data = { Communication.currentNode.battery.ToString("R"), Communication.currentNode.speed.ToString("R"), Communication.currentNode.type, Communication.currentNode.status };
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
}
