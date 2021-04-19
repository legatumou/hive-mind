public class NodeData
{
    public MyGridProgram myGrid;
    public Gyro gyroHandle;
    public Docking dockingHandle;
    public Navigation navHandle;
    public Communication commHandle;
    public Core coreHandle;

    public int id { get; set; }
    public long keepalive { get; set; }
    public long entityId { get; set; }
    public float battery { get; set; }
    public double speed { get; set; }
    public string type { get; set; }
    public string status { get; set; }
    public int usedInventorySpace { get; set; }
    public Vector3D position;
    public Vector3D connectorAnchorTopPosition;
    public Vector3D connectorAnchorBottomPosition;
    public List<Vector3D> gyroPosition;
    public int masterConnectorId { get; set; }
    public int creatorId = 0;
    public bool movingFromCreator = false;

    public NodeData(int id)
    {
        this.id = id;
        this.battery = 0;
        this.usedInventorySpace = 0;
        this.speed = 0.0;
        this.status = "init";
        this.keepalive = Communication.getTimestamp();
    }

    public void initiate() {}

    public void moveAwayFromCreator(int id) {
        this.movingFromCreator = true;
        this.creatorId = id;
    }

    public void initNavigation(MyGridProgram myGrid) {
        this.myGrid = myGrid;
        this.navHandle = new Navigation(myGrid);
        this.gyroHandle = new Gyro(myGrid);
        this.dockingHandle = new Docking(myGrid);
        this.navHandle.setDockingHandle(this.dockingHandle);
        this.navHandle.setGyroHandle(this.gyroHandle);
        this.navHandle.setCommunicationHandle(this.commHandle);
        this.dockingHandle.setNavHandle(this.navHandle);
        if (this.isMasterNode()) {
            this.dockingHandle.amAMaster = true;
        }
    }

    public bool hasMaster() {
        if (Communication.masterDrone == null) {
            this.status = "requesting-master";
            this.commHandle.sendMasterRequest();
            this.navHandle.clearPath();
            return false;
        }
        if (this.status == "requesting-master") {
            this.status = "master-accepted";
        }
        return true;
    }

    public void setCoreHandle(Core core) {
        this.coreHandle = core;
    }

    public void setCommHandle(Communication commHandle) {
        this.commHandle = commHandle;
    }

    public bool isMasterNode() {
        return (this.type == "mothership");
    }

    public void turnOnDrones() {
        List<IMyProgrammableBlock> blocks = new List<IMyProgrammableBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks);
        foreach (IMyProgrammableBlock block in blocks) {
            if (block.CustomName.Contains("[Drone]")) {
                if (block.Enabled == false) {
                    block.Enabled = true;
                }
            }
        }
    }

    public int getInventoryUsedSpacePercentage() {
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(blocks);
        IMyInventoryOwner inventoryOwner;
        IMyInventory containerInventory;
        double usedVolume = 0;
        double totalVolume = 0;

        for (int i = 0; i < blocks.Count; i++) {
            inventoryOwner = (IMyInventoryOwner) blocks[i];
            containerInventory = inventoryOwner.GetInventory(0);
            usedVolume += (double) containerInventory.CurrentVolume;
            totalVolume += (double) containerInventory.MaxVolume;
        }

        return (int) ((usedVolume/totalVolume) * 100);
    }

    public List<MyInventoryItem> getInventoryContents() {
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(blocks);
        IMyInventoryOwner inventoryOwner;
        IMyInventory containerInventory;
        List<MyInventoryItem> items = new List<MyInventoryItem>();
        List<MyInventoryItem> returnList = new List<MyInventoryItem>();

        for (int i = 0; i < blocks.Count; i++) {
            inventoryOwner = (IMyInventoryOwner) blocks[i];
            containerInventory = inventoryOwner.GetInventory(0);
            containerInventory.GetItems(items);
            foreach (MyInventoryItem item in items) {
                returnList.Add(item);
            }
        }

        return returnList;
    }

    public Vector3D getTarget() {
        return new Vector3D();
    }

    public void execute() {
        Display.print("[Error] Unknown drone type.\n");
    }

    public void process(MyGridProgram grid)
    {
        this.myGrid = grid;
        List<IMySensorBlock> sensors = new List<IMySensorBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, c => c.BlockDefinition.ToString().ToLower().Contains("sensor"));
        foreach (IMySensorBlock sensor in sensors) {
            if (sensor.CustomName.Contains("[Drone]") && !sensor.CustomName.Contains("[Anchor]")) {
                this.navHandle.updateNearbyCollisionData(sensor);
                // @TODO: Handle collision data.
            }
        }
    }

    public NodeData findFriends() {
        // Find a friendly drone.
        double closestDistance = 100000.0;
        double distance;
        NodeData closest = new NodeData(0);

        for (int i = 0; i < Communication.connectedNodesData.Count; i++) {
            NodeData node = Communication.connectedNodesData[i];
            distance = this.navHandle.getDistanceFrom(node.position, this.navHandle.getShipPosition());
            if (distance < closestDistance && distance > 50) { // not too close ;)
                closestDistance = distance;
                closest = node;
            }
        }

        return closest;
    }
}
