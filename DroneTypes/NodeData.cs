public class NodeData
{
    public MyGridProgram myGrid;

    public int id { get; set; }
    public long keepalive { get; set; }
    public long entityId { get; set; }
    public float battery { get; set; }
    public double speed { get; set; }
    public string type { get; set; }
    public string status { get; set; }
    public int usedInventorySpace { get; set; }
    public Navigation navHandle;
    public Core coreHandle;

    public NodeData(int id)
    {
        this.id = id;
        this.battery = 0;
        this.speed = 0.0;
        this.type = "...";
        this.status = "init";
        this.keepalive = Communication.getTimestamp();
    }

    public void updateDroneType() {
        if (Core.coreBlock != null) {
            string customData = Core.coreBlock.CustomData;
            this.type = customData; // @TODO: Proper custom data handling required.
        } else {
            this.type = "N/A";
        }
    }

    public static DrillingDrone getDroneClass(int nodeId) {
        return new DrillingDrone(nodeId); // @TODO: Fix this.
        /*
        DrillingDrone nodeClass;
        string type = NodeData.getDroneType();
        if (type == "mining") {
            nodeClass = new DrillingDrone(nodeId);
        } else if (type == "combat") {
            nodeClass = new CombatDrone(nodeId);
        } else {
            nodeClass = new ReplicatorDrone(nodeId);
        }

        return nodeClass;*/
    }

    public void initNavigation(MyGridProgram myGrid) {
        this.myGrid = myGrid;
        this.navHandle = new Navigation(myGrid);
        this.navHandle.updateRemoteControls();
    }

    public void setCoreHandle(Core core) {
        this.coreHandle = core;
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

    public Vector3D getTarget() {
        return new Vector3D();
    }

    public void execute() {
        this.myGrid.Echo("Unknown drone type.\n");
    }

    public void process(MyGridProgram grid)
    {
        this.myGrid = grid;
        List<IMySensorBlock> sensors = new List<IMySensorBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, c => c.BlockDefinition.ToString().ToLower().Contains("sensor"));
        foreach (IMySensorBlock sensor in sensors) {
            if (sensor.CustomName.Contains("[Drone]")) {
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
            distance = this.navHandle.getDistanceFrom(node.getShipPosition(), Core.coreBlock.GetPosition());
            if (distance < closestDistance && distance > 50) { // not too close ;)
                closestDistance = distance;
                closest = node;
            }
        }

        return closest;
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
