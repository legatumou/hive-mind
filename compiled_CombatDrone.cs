
public class Drone : NodeData
{
    public Drone(int id) : base(id) {}

    public void handleIdle() {
        NodeData targetFriend = this.findFriends();

        if (targetFriend.id > 0) {
            this.status = "running-to-friend";
            this.navHandle.move(targetFriend.getShipPosition(), "running-to-friend");
        } else {
            // Find allies
            this.status = "finding-friends";
            Vector3D newPos = Core.coreBlock.GetPosition();
            // Random position
            Random rnd = new Random();
            newPos.X += (int) rnd.Next(-10000, 10000);
            newPos.Y += (int) rnd.Next(-10000, 10000);
            newPos.Z += (int) rnd.Next(-10000, 10000);
            this.navHandle.move(newPos, "cruising");
        }
    }

    public void execute() {
        DetectedEntity target = this.getTarget();

        if (target.id > 0) {
            // Move to closest ore.
            Vector3D targetPos = target.position;

            // Add some random movement.
            Random rnd = new Random();
            targetPos.X += (int) rnd.Next(-10, 10);
            targetPos.Y += (int) rnd.Next(-10, 10);
            targetPos.Z += (int) rnd.Next(-10, 10);

            // Execute movement
            this.navHandle.move(targetPos, "navigate-to-ore");
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
        string[] targetList = {"SmallGrid", "LargeGrid", "CharacterHuman", "CharacterOther"};
        double closestDistance = 3000;
        double targetDistance;
        foreach (DetectedEntity entity in this.navHandle.nearbyEntities) {
            // Filter out non asteroids.
            if (!targetList.Any(entity.name.Contains)) continue;
            targetDistance = this.navHandle.getDistanceFrom(this.getShipPosition(), entity.position);
            if (targetDistance < closestDistance) {
                closest = entity;
                closestDistance = targetDistance;
            }
        }
        return closest;
    }
}
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

    public NodeData(int id)
    {
        this.id = id;
        this.battery = 0;
        this.usedInventorySpace = 0;
        this.speed = 0.0;
        this.status = "init";
        this.keepalive = Communication.getTimestamp();
    }

    public void initiate() {

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

    public void setCoreHandle(Core core) {
        this.coreHandle = core;
    }

    public void setCommHandle(Communication commHandle) {
        this.commHandle = commHandle;
    }

    public bool isMasterNode() {
        return (this.type == "replicator");
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
            distance = this.navHandle.getDistanceFrom(node.position, this.navHandle.getShipPosition());
            if (distance < closestDistance && distance > 50) { // not too close ;)
                closestDistance = distance;
                closest = node;
            }
        }

        return closest;
    }
}
bool runMainLoop = false;
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
    if (runMainLoop == true) {
        commHandle.handleListeners();
        commHandle.handleKeepalives();
        coreHandle.updateDroneData();
        commHandle.sendPing();
        commHandle.sendNodeData();
        commHandle.sendNearbyEntityList();
        coreHandle.execute();
        Display.display();
    }
}

public Program()
{
    nodeId = generateRandomId();
    Echo("Loading drone, ID: " + nodeId);
    Display.myGrid = this;
    Display.fetchOutputDevices();
    commHandle = new Communication(this);
    initCore(nodeId);
    LCDPanel = GridTerminalSystem.GetBlockWithName("[Drone] LCD") as IMyTextPanel;
    if (LCDPanel != null) {
        LCDPanel.CustomData = "" + nodeId;
    }
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    commHandle.setupAntenna();

    if (this.validation()) {
        Display.print("Systems online.");
        runMainLoop = true;
    } else {
        Display.print("[Drone] Core is missing!");
    }
}

public void initCore(int nodeId) {
    coreHandle = new Core(this);
    Communication.currentNode = new Drone(nodeId);
    Communication.currentNode.setCoreHandle(coreHandle);
    Communication.currentNode.setCommHandle(commHandle);
    Communication.currentNode.initNavigation(this);
    Communication.currentNode.navHandle.updateRemoteControls();
    coreHandle.setCoreBlock();
    Communication.currentNode.initiate();
}

public bool validation() {
    if (
        Core.coreBlock == null
    ) {
        return false;
    }
    return true;
}

public int generateRandomId()
{
    Random rnd = new Random();
    return rnd.Next(0, 10000);
}
public class Communication
{
    public static List<Drone> slaves = new List<Drone>();
    public static List<int> connectedNodes = new List<int>();
    public static List<Drone> connectedNodesData = new List<Drone>();
    public static Drone currentNode;
    public static Drone masterDrone;


    public Gyro gyroHandle;
    private long lastPing = 0;
    private long lastDataUpdate = 0;
    private CommunicationDataStructure dataStructure;
    private MyGridProgram myGrid;
    private long lastRequest = 0;
    private long lastEntityDataUpdate = 0;

    public Communication(MyGridProgram myGrid) {
        this.myGrid = myGrid;
        this.dataStructure = new CommunicationDataStructure();
        this.gyroHandle = new Gyro(myGrid);

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
            if (Communication.getTimestamp() - Communication.connectedNodesData[i].keepalive > 60) {
                // Disconnect if over 60 sec timeout.

                for (int n = 0; n < Communication.slaves.Count; n++) {
                    if (Communication.slaves[n].id == Communication.connectedNodesData[i].id) {
                        Communication.slaves.RemoveAt(n);
                    }
                }
                if (Communication.masterDrone != null && Communication.masterDrone.id == Communication.connectedNodesData[i].id) {
                    Communication.masterDrone = null;
                }
                Communication.connectedNodes.RemoveAt(i);
                Communication.connectedNodesData.RemoveAt(i);

            }
        }
    }

    public void sendNodeData() {
        if (this.lastDataUpdate == 0 || Communication.getTimestamp() - this.lastDataUpdate > 10) {
            Vector3D pos = this.myGrid.Me.GetPosition();
            IMyShipConnector connector = Communication.currentNode.dockingHandle.getAvailableConnector();
            Vector3D connectorPos = new Vector3D(0, 0, 0);
            Vector3D connectorAnchorTopPosition = new Vector3D(0, 0, 0);
            Vector3D connectorAnchorBottomPosition = new Vector3D(0, 0, 0);
            // @TODO: Data exchange needs improvements.
            if (connector != null) {
                connectorPos = connector.GetPosition();
                connectorAnchorTopPosition = Communication.currentNode.dockingHandle.getAnchorPosition(1);
                connectorAnchorBottomPosition = Communication.currentNode.dockingHandle.getAnchorPosition(2);
            }
            string[] data = {
                Communication.currentNode.battery.ToString("R"),
                Communication.currentNode.speed.ToString("R"),
                Communication.currentNode.type,
                Communication.currentNode.status,
                /*this.myGrid.Me.EntityId.ToString("R")*/"0",
                pos.X.ToString("R"),
                pos.Y.ToString("R"),
                pos.Z.ToString("R"),
                connectorAnchorTopPosition.X.ToString("R"),
                connectorAnchorTopPosition.Y.ToString("R"),
                connectorAnchorTopPosition.Z.ToString("R"),
                connectorAnchorBottomPosition.X.ToString("R"),
                connectorAnchorBottomPosition.Y.ToString("R"),
                connectorAnchorBottomPosition.Z.ToString("R"),
                Communication.currentNode.dockingHandle.isDockingInProgress() ? "1" : "0",
                Communication.currentNode.usedInventorySpace.ToString(),
                Communication.currentNode.dockingHandle.dockingWithDrone.ToString()

            };
            this.broadcastMessage("drone-generic-data-" + Communication.currentNode.id + "_" + string.Join("_", data) );
            //Display.print("Broadcast: " + "drone-data-" + Communication.currentNode.id + '_' + string.Join('_', data));
            this.lastDataUpdate = Communication.getTimestamp();
        }
    }

    public void sendNearbyEntityList() {
        if (this.lastEntityDataUpdate == 0 || Communication.getTimestamp() - this.lastEntityDataUpdate > 35) {
            this.dataStructure.newPackage();
            this.dataStructure.addRawData("drone-data-nearby"); // @TODO: This is just so it would work with old data structure as well, once all have been moved to the new one, remove this.
            this.dataStructure.addData("id", Communication.currentNode.id.ToString());

            DetectedEntity entity;
            for (int i = 0; i < Communication.currentNode.navHandle.nearbyEntities.Count; i++) {
                entity = Communication.currentNode.navHandle.nearbyEntities[i];
                if (entity.id > 0) {
                    this.dataStructure.addRawData(
                        "Entity=" + entity.name + "=" +
                        DetectedEntity.getEntityTypeInteger(entity.type).ToString() + "=" +
                        entity.id.ToString() + "=" + entity.position.X.ToString() + "=" +
                        entity.position.Y.ToString() + "=" + entity.position.Z.ToString() + "=" +
                        entity.lastSeen.ToString()
                    );
                }
            }
            this.broadcastMessage(this.dataStructure.generateOutput());
            this.lastEntityDataUpdate = Communication.getTimestamp();
        }
    }

    public void sendMasterRequest() {
        if (this.lastRequest == 0 || Communication.getTimestamp() - this.lastRequest > 15) {
            Display.print("Requesting a master...");
            this.broadcastMessage("drone-master-request-" + Communication.currentNode.id);
            this.lastRequest = Communication.getTimestamp();
        }
    }

    public void sendDockingRequest() {
        if (this.lastRequest == 0 || Communication.getTimestamp() - this.lastRequest > 15) {
            Display.print("Requesting docking permissions");
            this.broadcastMessage("drone-docking-request-" + Communication.masterDrone.id + '_' + Communication.currentNode.id);
            this.lastRequest = Communication.getTimestamp();
        }
    }

    public void sendDockingAccepted(int slaveId) {
        Display.print("Accepting a docking request.");
        this.broadcastMessage("drone-docking-accept-" + slaveId + '_' + Communication.currentNode.id);
    }

    public void sendMasterAcceptance(int slaveId) {
        Display.print("Accepting a slave...");
        this.broadcastMessage("drone-master-accept-" + slaveId + '_' + Communication.currentNode.id);
    }

    public void sendStopDocking(string reason, int nodeId) {
        Display.print("Halting docking (" + reason + "): " + nodeId);
        this.dataStructure.newPackage();
        this.dataStructure.addRawData("drone-halt-docking"); // @TODO: This is just so it would work with old data structure as well, once all have been moved to the new one, remove this.
        this.dataStructure.addData("id", nodeId.ToString());
        this.dataStructure.addData("reason", reason);
        this.broadcastMessage(this.dataStructure.generateOutput());
    }

    public void sendDockingLockRequest(int status) {
        if (this.lastRequest == 0 || Communication.getTimestamp() - this.lastRequest > 10) {
            Display.print("Requesting a dock lock.");
            this.broadcastMessage("drone-request-dock-lock-" + Communication.masterDrone.id + '_' + status);
            this.lastRequest = Communication.getTimestamp();
        }
    }

    public void sendDockingStep(int step) {
        if (this.lastRequest == 0 || Communication.getTimestamp() - this.lastRequest > 10) {
            this.dataStructure.newPackage();
            this.dataStructure.addRawData("drone-docking-step"); // @TODO: This is just so it would work with old data structure as well, once all have been moved to the new one, remove this.
            this.dataStructure.addData("id", Communication.masterDrone.id.ToString());
            this.dataStructure.addData("slaveId", Communication.currentNode.id.ToString());
            this.dataStructure.addData("step", step.ToString());
            this.broadcastMessage(this.dataStructure.generateOutput());
        }
    }

    public void broadcastMessage(string messageOut) {
        string tag1 = "drone-channel";

        string[] dataSplitted = messageOut.Split('_');
        if (dataSplitted.Count() > 0) {
            Display.printDebug("Outgoing msg: " + dataSplitted[0]);
        } else {
            Display.printDebug("Outgoing msg: " + dataSplitted);
        }
        this.myGrid.IGC.SendBroadcastMessage(tag1, messageOut);
    }

    public void setupAntenna() {
        string tag1 = "drone-channel";
        this.myGrid.IGC.RegisterBroadcastListener(tag1);
    }

    public int getNodeIndexById(int id) {
        for (int i = 0; i < Communication.connectedNodes.Count; i++) {
            if (Communication.connectedNodes[i] == id) {
                return i;
            }
        }

        return -1;
    }

    public void handleListeners() {
        var listens = new List<IMyBroadcastListener>();
        this.myGrid.IGC.GetBroadcastListeners( listens );

        for( int i=0; i<listens.Count; ++i ) {
            while( listens[i].HasPendingMessage ) {
                var msg = listens[i].AcceptMessage();

                // Debug log incoming message
                string[] dataSplitted = msg.Data.ToString().Split('_');
                if (dataSplitted.Count() > 0) {
                    Display.printDebug("Incoming msg: " + dataSplitted[0]);
                } else {
                    Display.printDebug("Incoming msg: " + dataSplitted);
                }

                if( msg.Data.ToString().Substring(0, "drone-ping".Length) == "drone-ping" ) {
                    int id = int.Parse(msg.Data.ToString().Substring("drone-ping".Length + 1));
                    this.handleResponsePing(id);
                } else if ( msg.Data.ToString().Substring(0, "drone-generic-data".Length) == "drone-generic-data" ) {
                    string data = msg.Data.ToString().Substring("drone-generic-data".Length + 1);
                    this.handleResponseData(data);
                } else if ( msg.Data.ToString().Substring(0, "drone-docking-request".Length) == "drone-docking-request" ) {
                    string data = msg.Data.ToString().Substring("drone-docking-request".Length + 1);
                    this.handleDockingRequest(data);
                } else if ( msg.Data.ToString().Substring(0, "drone-master-request".Length) == "drone-master-request" ) {
                    int data = int.Parse(msg.Data.ToString().Substring("drone-master-request".Length + 1));
                    this.handleMasterRequest(data);
                } else if ( msg.Data.ToString().Substring(0, "drone-master-accept".Length) == "drone-master-accept" ) {
                    string data = msg.Data.ToString().Substring("drone-master-accept".Length + 1);
                    this.handleMasterAcceptance(data);
                } else if ( msg.Data.ToString().Substring(0, "drone-docking-accept".Length) == "drone-docking-accept" ) {
                    string data = msg.Data.ToString().Substring("drone-docking-accept".Length + 1);
                    this.handleDockingAccepted(data);
                } else if ( msg.Data.ToString().Substring(0, "drone-request-dock-lock".Length) == "drone-request-dock-lock" ) {
                    string data = msg.Data.ToString().Substring("drone-request-dock-lock".Length + 1);
                    this.handleDockLockRequest(data);
                } else if ( msg.Data.ToString().Substring(0, "drone-data-nearby".Length) == "drone-data-nearby" ) {
                    string data = msg.Data.ToString().Substring("drone-data-nearby".Length);
                    this.handleDataNearby(this.dataStructure.getFormattedInput(data));
                } else if ( msg.Data.ToString().Substring(0, "drone-halt-docking".Length) == "drone-halt-docking" ) {
                    string data = msg.Data.ToString().Substring("drone-halt-docking".Length);
                    this.handleHaltDocking(this.dataStructure.getFormattedInput(data));
                } else if ( msg.Data.ToString().Substring(0, "drone-docking-step".Length) == "drone-docking-step" ) {
                    string data = msg.Data.ToString().Substring("drone-docking-step".Length);
                    this.handleDockingStep(this.dataStructure.getFormattedInput(data));
                }
            }
        }
    }

    public void handleDockingStep(List<CommunicationDataStructureValue> responseData) {
        int id = 0, slaveId = 0, step = 0;
        foreach (CommunicationDataStructureValue data in responseData) {
            if (data.getName() == "id") {
                id = int.Parse(data.getValue());
            } else if (data.getName() == "slaveId") {
                slaveId = int.Parse(data.getValue());
            } else if (data.getName() == "step") {
                step = int.Parse(data.getValue());
            }
        }
        if (step <= 2 && id != 0 && id == Communication.currentNode.id) {
            if (slaveId != 0 && Communication.currentNode.dockingHandle.dockingWithDrone != 0 && Communication.currentNode.dockingHandle.dockingWithDrone != slaveId) {
                this.sendStopDocking("out-of-order", slaveId);
            }
        }
    }

    public void handleHaltDocking(List<CommunicationDataStructureValue> responseData) {
        int id = 0;
        string reason = "unknown-connection";
        foreach (CommunicationDataStructureValue data in responseData) {
            if (data.getName() == "id") {
                id = int.Parse(data.getValue());
            } else if (data.getName() == "reason") {
                reason = data.getValue();
            }
        }
        if (id != 0 && id == Communication.currentNode.id) {
            Display.printDebug("[Incoming] Halt docking reason " + reason);
            Communication.currentNode.dockingHandle.dockingWithDrone = 0; // No need to send signal back.
            Communication.currentNode.dockingHandle.haltDocking(reason);
        }
    }

    public void handleDataNearby(List<CommunicationDataStructureValue> responseData) {
        int id, entityType;
        long entityId, lastSeen;
        Vector3D position;
        string entityName;
        Drone myDrone = Communication.currentNode;
        DetectedEntity nearbyEntity;
        foreach (CommunicationDataStructureValue data in responseData) {
            if (data.getName() == "id") {
                id = int.Parse(data.getValue());
            } else if (data.getName() == "Entity") {
                entityName = data.getValue();
                entityType = int.Parse(data.getAdditional(0).getValue());
                entityId = long.Parse(data.getAdditional(1).getValue());
                position = new Vector3D(
                    double.Parse(data.getAdditional(2).getValue()),
                    double.Parse(data.getAdditional(3).getValue()),
                    double.Parse(data.getAdditional(4).getValue())
                );
                lastSeen = long.Parse(data.getAdditional(5).getValue());

                nearbyEntity = new DetectedEntity();
                nearbyEntity.id = entityId;
                nearbyEntity.name = entityName;
                nearbyEntity.distance = myDrone.navHandle.getDistanceFrom(myDrone.navHandle.getShipPosition(), position);
                nearbyEntity.type = DetectedEntity.getEntityType(entityType);
                nearbyEntity.position = position;
                nearbyEntity.lastSeen = lastSeen;
                Communication.currentNode.navHandle.addNearbyEntity(nearbyEntity);
            }
        }
    }

    public void handleDockLockRequest(string data) {
        if (Communication.currentNode.type != "replicator") return; // Replicators handle docking requests
        string[] dataSplitted = data.Split('_');
        if (dataSplitted.Count() == 2) {
            int id = int.Parse(dataSplitted[0]);
            if (Communication.currentNode.id != id) return; // If not my id
            int status = int.Parse(dataSplitted[1]);
            Communication.currentNode.dockingHandle.setPistonState((bool) (status == 1));
            Communication.currentNode.dockingHandle.setConnectorState((bool) (status == 1));
        }
    }

    public void handleDockingAccepted(string data) {
        if (Communication.currentNode.type == "replicator") return; // Replicators handle docking requests
        string[] dataSplitted = data.Split('_');
        if (dataSplitted.Count() == 2) {
            int id = int.Parse(dataSplitted[0]);
            if (Communication.currentNode.id != id) return; // If not my id
            int masterId = int.Parse(dataSplitted[1]);
            int nodeIndex = this.getNodeIndexById(masterId);
            if (nodeIndex == -1) {
                Communication.connectedNodes.Add(masterId);
                Drone node = new Drone(masterId);
                node.initNavigation(this.myGrid);
                Communication.connectedNodesData.Add(node);
                nodeIndex = this.getNodeIndexById(masterId);
            }
            Communication.currentNode.dockingHandle.dockingWithDrone = masterId;
            Communication.currentNode.dockingHandle.approveDocking();
        }
    }

    public void handleDockingRequest(string data) {
        if (Communication.currentNode.type != "replicator") return; // Replicators handle docking requests
        string[] dataSplitted = data.Split('_');
        if (dataSplitted.Count() == 2) {
            int id = int.Parse(dataSplitted[0]);
            if (Communication.currentNode.id != id) return; // If not my id
            int slaveId = int.Parse(dataSplitted[1]);
            int nodeIndex = this.getNodeIndexById(slaveId);
            if (nodeIndex == -1) {
                Communication.connectedNodes.Add(slaveId);
                Drone node = new Drone(slaveId);
                node.initNavigation(this.myGrid);
                Communication.connectedNodesData.Add(node);
                nodeIndex = this.getNodeIndexById(slaveId);
            }
            if (Communication.currentNode.dockingHandle.dockingInProgress == true && slaveId != Communication.currentNode.dockingHandle.dockingWithDrone) {
                Display.print("Docking request denied (In progress).");
            } else {
                Display.print("Accepting docking request.");
                this.sendDockingAccepted(slaveId);
                Communication.currentNode.dockingHandle.dockingWithDrone = slaveId;
                Communication.currentNode.dockingHandle.initDocking();
            }
        } else {
            Display.print("[ERROR] Docking request invalid. (" + data + ")");
        }
    }

    public void handleMasterAcceptance(string data) {
        if (Communication.currentNode.type == "replicator") return; // Replicators are the masters.
        string[] dataSplitted = data.Split('_');
        if (dataSplitted.Count() == 2) {
            int id = int.Parse(dataSplitted[0]);
            if (Communication.currentNode.id != id) return; // If not my id
            int masterId = int.Parse(dataSplitted[1]);
            int nodeIndex = this.getNodeIndexById(masterId);
            if (nodeIndex == -1) {
                Communication.connectedNodes.Add(masterId);
                Drone node = new Drone(masterId);
                node.initNavigation(this.myGrid);
                Communication.connectedNodesData.Add(node);
                nodeIndex = this.getNodeIndexById(masterId);
                Communication.masterDrone = node;
            } else {
                Communication.masterDrone = Communication.connectedNodesData[nodeIndex];
            }
        }

    }

    public void handleMasterRequest(int id) {
        if (Communication.currentNode.type != "replicator") return; // Replicators are the masters.
        if (this.isSlaveConnected(id)) {
            this.sendMasterAcceptance(id);
            Display.print("Slave already accepted, accepting again. (ID: " + id + ")");
        } else {
            Drone node;
            int nodeIndex = this.getNodeIndexById(id);
            if (nodeIndex == -1) {
                Communication.connectedNodes.Add(id);
                node = new Drone(id);
                node.initNavigation(this.myGrid);
                Communication.connectedNodesData.Add(node);
            } else {
                node = Communication.connectedNodesData[nodeIndex];
            }
            Communication.slaves.Add(node);
            this.sendMasterAcceptance(id);
            Display.print("Accepting a slave. (ID: " + id + ")");
        }
    }

    public bool isSlaveConnected(int slaveId) {
        foreach (Drone drone in Communication.slaves) {
            if (drone.id == slaveId) {
                return true;
            }
        }

        return false;
    }

    public void handleResponseData(string data)
    {
        int fieldCount = 18;
        string[] dataSplitted = data.Split('_');
        if (dataSplitted.Count() == fieldCount) {
            int id = int.Parse(dataSplitted[0]);
            int nodeIndex = this.getNodeIndexById(id);
            if (nodeIndex == -1) {
                Communication.connectedNodes.Add(id);
                Drone node = new Drone(id);
                node.initNavigation(this.myGrid);
                Communication.connectedNodesData.Add(node);
                nodeIndex = this.getNodeIndexById(id);
            }
            Communication.connectedNodesData[nodeIndex].battery = float.Parse(dataSplitted[1]); // battery status
            Communication.connectedNodesData[nodeIndex].speed = float.Parse(dataSplitted[2]); // speed
            Communication.connectedNodesData[nodeIndex].type = dataSplitted[3]; // node type
            Communication.connectedNodesData[nodeIndex].status = dataSplitted[4]; // status
            Communication.connectedNodesData[nodeIndex].entityId = long.Parse(dataSplitted[5]); // entityId
            // Position
            double X = double.Parse(dataSplitted[6]);
            double Y = double.Parse(dataSplitted[7]);
            double Z = double.Parse(dataSplitted[8]);
            Communication.connectedNodesData[nodeIndex].position = new Vector3D(X, Y, Z);

            // Connector position
            X = double.Parse(dataSplitted[9]);
            Y = double.Parse(dataSplitted[10]);
            Z = double.Parse(dataSplitted[11]);
            Communication.connectedNodesData[nodeIndex].connectorAnchorTopPosition = new Vector3D(X, Y, Z);

            // Connector anchor position
            X = double.Parse(dataSplitted[12]);
            Y = double.Parse(dataSplitted[13]);
            Z = double.Parse(dataSplitted[14]);
            Communication.connectedNodesData[nodeIndex].connectorAnchorBottomPosition = new Vector3D(X, Y, Z);

            // Docking.dockingInProgress
            Communication.connectedNodesData[nodeIndex].dockingHandle.dockingInProgress = int.Parse(dataSplitted[15]) == 1;

            // drone.usedInventorySpace
            Communication.connectedNodesData[nodeIndex].usedInventorySpace = int.Parse(dataSplitted[16]);

            // drone.dockingHandle.dockingWithDrone
            Communication.connectedNodesData[nodeIndex].dockingHandle.dockingWithDrone = int.Parse(dataSplitted[17]);

            // Update if also master.
            if (Communication.masterDrone != null && id == Communication.masterDrone.id) {
                Communication.masterDrone = Communication.connectedNodesData[nodeIndex];
            }
        } else {
            Display.print("[Error] Invalid request, possibly outdated drone. (FieldCount: " + dataSplitted.Count() + " / " + fieldCount + ")");
        }
    }

    public void handleResponsePing(int id)
    {
        if (!Communication.connectedNodes.Contains(id)) {
            Display.print("Adding drone: " + id);
            Communication.connectedNodes.Add(id);
            Drone node = new Drone(id);
            node.initNavigation(this.myGrid);
            Communication.connectedNodesData.Add(node);
            Display.print("--> New drone connected: " + id);
            Display.print("New drone connected: " + id);
        } else {
            Communication.connectedNodesData[this.getNodeIndexById(id)].keepalive = Communication.getTimestamp();
        }
    }
}
public class CommunicationDataStructure
{
    private List<CommunicationDataStructureValue> package;

    public CommunicationDataStructure() {

    }

    public void newPackage() {
        this.package = new List<CommunicationDataStructureValue>();
    }

    public void add(CommunicationDataStructureValue value) {
        this.package.Add(value);
    }

    public void addData(string configName, string value) {
        this.package.Add(new CommunicationDataStructureValue(configName, value, false));
    }

    public void addRawData(string value) {
        this.package.Add(new CommunicationDataStructureValue("", value, true));
    }

    public string generateOutput() {
        List<string> output = new List<string>();
        foreach (CommunicationDataStructureValue data in this.package) {
            output.Add(data.getFormatted());
        }

        return string.Join("_", output);
    }

    public List<CommunicationDataStructureValue> getFormattedInput(string input) {
        this.package = new List<CommunicationDataStructureValue>();

        string[] dataSplitted = input.Split('_');
        if (dataSplitted.Count() > 0) {
            CommunicationDataStructureValue tmpValue;
            foreach (string entry in dataSplitted) {
                tmpValue = new CommunicationDataStructureValue("", "", false);
                tmpValue.importData(entry);
                this.add(tmpValue);
            }
        } else {
            this.addRawData(input);
        }

        return this.package;
    }
}
public class CommunicationDataStructureValue
{
    private string key = "";
    private string value = "";
    public bool isRawData = false;
    private List<CommunicationDataStructureValue> additionalData = new List<CommunicationDataStructureValue>();

    public CommunicationDataStructureValue(string configName, string value, bool isRawData) {
        this.key = configName;
        this.value = value;
        this.isRawData = isRawData;
    }

    public string getName() {
        return this.key;
    }

    public string getValue() {
        return this.value;
    }

    public int getAdditionalColumnCount() {
        return this.additionalData.Count;
    }

    public string getFormatted() {
        if (this.isRawData) {
            return this.getValue();
        } else {
            return this.getName() + "=" + this.getValue();
        }
    }

    public CommunicationDataStructureValue getAdditional(int index) {
        if (this.additionalData.Count > index && this.additionalData[index] != null) {
            return this.additionalData[index];
        }
        return new CommunicationDataStructureValue("", "0", true);
    }

    public void importData(string data) {
        string[] dataSplitted = data.Split('=');
        if (dataSplitted.Count() >= 2) {
            this.key = dataSplitted[0];
            this.value = dataSplitted[1];
            if (dataSplitted.Count() > 2) {
                CommunicationDataStructureValue extra;
                for (int i = 2; i < dataSplitted.Count(); i++) {
                    extra = new CommunicationDataStructureValue("", "", true);
                    extra.importData(dataSplitted[i]);
                    this.additionalData.Insert(this.additionalData.Count, extra);
                }
            }
        } else {
            this.value = data;
            this.isRawData = true;
        }
    }
}
public class Core
{
    private long lastPositionUpdate = 0;
    private Vector3D lastPosition = new Vector3D(0,0,0);
    private MyGridProgram myGrid;
    public static IMyProgrammableBlock coreBlock;

    public Core(MyGridProgram myGrid) {
        this.myGrid = myGrid;
    }

    public void execute() {
        Communication.currentNode.process(this.myGrid);
        Communication.currentNode.execute();
    }

    public void setCoreBlock() {
        Core.coreBlock = (IMyProgrammableBlock) this.myGrid.GridTerminalSystem.GetBlockWithName("[Drone] Core");
    }


    // @TODO: Move bottom methods.

    public void updateDroneData() {
        List<IMyBatteryBlock> vBatteries = new List<IMyBatteryBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(vBatteries, c => c.BlockDefinition.ToString().ToLower().Contains("battery"));

        float maxStorage = 0;
        float storage = 0;

        foreach (IMyBatteryBlock block in vBatteries) {
            maxStorage += this.getPowerAsInt(this.getDetailedInfoValue(block, "Max Stored Power"));
            storage += this.getPowerAsInt(this.getDetailedInfoValue(block, "Stored power"));
        }
        Communication.currentNode.battery = (storage / maxStorage) * 100;
        Communication.currentNode.usedInventorySpace = Communication.currentNode.getInventoryUsedSpacePercentage();

        if (this.lastPositionUpdate == 0 || Communication.getTimestamp() - this.lastPositionUpdate > 0) {
            this.lastPositionUpdate = Communication.getTimestamp();
            Vector3D currentPosition = this.myGrid.Me.GetPosition();
            Communication.currentNode.speed = ((currentPosition-this.lastPosition)*60).Length();
            this.lastPosition = currentPosition;
        }
    }

    public string getDetailedInfoValue(IMyBatteryBlock block, string name) {
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

    public int getPowerAsInt(string text) {
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
}

public class DetectedEntity
{
    public DetectedEntity()
    {

    }

    public long id { get; set; }
    public double distance { get; set; }
    public string name { get; set; }
    public long lastSeen { get; set; }
    public Vector3D position { get; set; }
    public MyDetectedEntityType type { get; set; }
    public MyDetectedEntityInfo entityInfo { get; set; }

    public static int getEntityTypeInteger(MyDetectedEntityType type) {
        if (type == MyDetectedEntityType.None) {
            return 0;
        } else if (type == MyDetectedEntityType.Unknown) {
            return 1;
        } else if (type == MyDetectedEntityType.SmallGrid) {
            return 2;
        } else if (type == MyDetectedEntityType.LargeGrid) {
            return 3;
        } else if (type == MyDetectedEntityType.CharacterHuman) {
            return 4;
        } else if (type == MyDetectedEntityType.CharacterOther) {
            return 5;
        } else if (type == MyDetectedEntityType.FloatingObject) {
            return 6;
        } else if (type == MyDetectedEntityType.Asteroid) {
            return 7;
        } else if (type == MyDetectedEntityType.Planet) {
            return 8;
        } else if (type == MyDetectedEntityType.Meteor) {
            return 9;
        } else if (type == MyDetectedEntityType.Missile) {
            return 10;
        } else {
            return 11;
        }
    }

    public static MyDetectedEntityType getEntityType(int type) {
        if (type == 0) {
            return MyDetectedEntityType.None;
        } else if (type == 1) {
            return MyDetectedEntityType.Unknown;
        } else if (type == 2) {
            return MyDetectedEntityType.SmallGrid;
        } else if (type == 3) {
            return MyDetectedEntityType.LargeGrid;
        } else if (type == 4) {
            return MyDetectedEntityType.CharacterHuman;
        } else if (type == 5) {
            return MyDetectedEntityType.CharacterOther;
        } else if (type == 6) {
            return MyDetectedEntityType.FloatingObject;
        } else if (type == 7) {
            return MyDetectedEntityType.Asteroid;
        } else if (type == 8) {
            return MyDetectedEntityType.Planet;
        } else if (type == 9) {
            return MyDetectedEntityType.Meteor;
        } else if (type == 10) {
            return MyDetectedEntityType.Meteor;
        } else {
            return MyDetectedEntityType.Unknown;
        }
    }
}

public class Display
{
    public static List<IMyTextPanel> LCD = new List<IMyTextPanel>();
    public static List<IMyTextPanel> TextPanels = new List<IMyTextPanel>();
    public static List<IMyCockpit> Cockpits = new List<IMyCockpit>();
    public static MyGridProgram myGrid;
    public static List<string> printQueue = new List<string>();
    public static List<string> debugPrintQueue = new List<string>();
    public static long lastDisplayRefresh = 0;

    public static bool debug = true; // @TODO: Should be some kind of config or some shit.

    public static void fetchOutputDevices() {
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(Display.TextPanels, c => c.BlockDefinition.ToString().ToLower().Contains("text"));
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Display.Cockpits, c => c.BlockDefinition.ToString().ToLower().Contains("cockpit"));
    }

    public static void print(string extraMsg) {
        Display.printQueue.Add(extraMsg);
    }

    public static void printDebug(string extraMsg) {
        Display.debugPrintQueue.Add(extraMsg);
    }

    public static void display() {
        if (Display.lastDisplayRefresh == 0 || Communication.getTimestamp() - Display.lastDisplayRefresh >= 3) {
            Display.lastDisplayRefresh = Communication.getTimestamp();

            // Main info panel
            string msg = Display.generateMessage(string.Join("\n", Display.printQueue));
            // TextPanels
            foreach (IMyTextPanel panel in Display.TextPanels) {
                if (panel.CustomName.Contains("[Drone]") && !panel.CustomName.Contains("[Debug]")) {
                    panel.WriteText(msg, false);
                }
            }

            // Debug data.
            string debugMsg = Display.generateDebugMessage(string.Join("\n", Display.debugPrintQueue));
            // TextPanels
            foreach (IMyTextPanel panel in Display.TextPanels) {
                if (panel.CustomName.Contains("[Drone]") && panel.CustomName.Contains("[Debug]")) {
                    panel.WriteText(debugMsg, false);
                }
            }
            // Merge it?
            if (Display.debug == true) {
                msg += "\n" + debugMsg;
            }

            Display.myGrid.Echo(msg);
            Display.printQueue = new List<string>();
            Display.debugPrintQueue = new List<string>();
        }
    }

    public static string generateDebugMessage(string msg) {
        string message = "";
        message += "=== DEBUG DATA ===\n";
        message += msg + "\n";

        return message;
    }

    public static string generateMessage(string msg) {
        string message = "";
        Drone myDrone = Communication.currentNode;
        List<IMyBatteryBlock> vBatteries = new List<IMyBatteryBlock>();
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(vBatteries, c => c.BlockDefinition.ToString().ToLower().Contains("battery"));
        message += "=== Drone Overview (ID: " + myDrone.id + " " + myDrone.type + ") ===\n";
        message += "Battery: " + Math.Round(myDrone.battery) + "% (" + vBatteries.Count + " batteries found)\n";
        message += "Speed: " + Math.Round((myDrone.speed / 100), 3) + " | ";
        if (myDrone.type == "replicator") {
            message += "Slaves: " + Communication.slaves.Count + " | \t";
        } else {
            message += "Space used: " + myDrone.usedInventorySpace + "%\n";
            if (Communication.masterDrone != null) {
                message += "MasterID: " + Communication.masterDrone.id + "\n";
            }
        }
        if (myDrone.dockingHandle.dockingInProgress) {
            long dockingTimeTaken = Communication.getTimestamp() - myDrone.dockingHandle.dockingStart;
            message += "Docking in progress (" + dockingTimeTaken + "s)\n";
        }

        if (myDrone.dockingHandle.connectionStart > 0) {
            message += "Docking connection established. \t";
        }
        message += "Status: " + myDrone.status  + "\n";
        if (myDrone.navHandle.nearbyEntities != null && myDrone.navHandle.nearbyEntities.Count() > 0) {
            message += " ==> Nearby entities (" + myDrone.navHandle.nearbyEntities.Count() + " found)\n";
            for (int i = 0; i < myDrone.navHandle.nearbyEntities.Count; i++) {
                if (i > 10) break;
                message += " => " + myDrone.navHandle.nearbyEntities[i].name + " (Distance: " + myDrone.navHandle.nearbyEntities[i].distance + ")" + "\n";
            }
        }
        message += msg + "\n";
        message += "=== Drones connected (" + Communication.connectedNodes.Count + ") ===\n";
        double distance;
        for (int i = 0; i < Communication.connectedNodesData.Count; i++) {
            distance = Communication.currentNode.navHandle.getDistanceFrom(Communication.connectedNodesData[i].position, myDrone.navHandle.getShipPosition());
            message += " ==> Drone ID: " + Communication.connectedNodesData[i].id + "\n";
            message += " Battery: " + Math.Round(Communication.connectedNodesData[i].battery) + "%" + "\t";
            message += " | Type: " + Communication.connectedNodesData[i].type + "\t";
            message += " | Status: " + Communication.connectedNodesData[i].status + "\n";
            message += " | Storage: " + Communication.connectedNodesData[i].usedInventorySpace + "% \t";
            message += " | Distance: " + distance + "m\n";
            if (Communication.connectedNodesData[i].navHandle.nearbyEntities != null && Communication.connectedNodesData[i].navHandle.nearbyEntities.Count > 0) {
                message += " => Nearby entities (" + Communication.connectedNodesData[i].navHandle.nearbyEntities.Count + " found)\n";
                for (int n = 0; n < myDrone.navHandle.nearbyEntities.Count; n++) {
                    if (n > 5) break;
                    message += " => " + Communication.connectedNodesData[i].navHandle.nearbyEntities[n].name + " (Distance: " + Communication.connectedNodesData[i].navHandle.nearbyEntities[n].distance + ")" + "\n";
                }
            }
        }

        return message;
    }
}

public class Docking
{
    public MyGridProgram myGrid;

    public bool amAMaster = false;
    public bool hasDockingPermission = false;
    public bool dockingInProgress = false;
    public bool enableLock = false;
    public long dockingStart = 0;
    public long connectionStart = 0;
    public bool pistonOpen = true;
    public int dockingStep = 2;
    public int dockingWithDrone = 0;
    public int queuePos = 0;
    public Navigation navHandle;

    public Docking(MyGridProgram myGrid) {
        this.myGrid = myGrid;
    }

    public void setNavHandle(Navigation navHandle) {
        this.navHandle = navHandle;
    }

    public IMyShipConnector getAvailableConnector() {
        List<IMyShipConnector> blocks = new List<IMyShipConnector>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks);
        foreach (IMyShipConnector connector in blocks) {
            if (connector.CustomName.Contains("[Drone]")) {
                return connector;
            }
        }
        return null;
    }

    public void initDocking() {
        this.dockingInProgress = true;
        this.enableLock = true;
        this.hasDockingPermission = false;
        this.dockingStep = 2;
        this.queuePos = 0;
        this.connectionStart = 0;
        this.dockingStart = Communication.getTimestamp();
        this.setConnectorState(true);
    }

    public void haltDocking(string reason = "unknown") {
        this.dockingInProgress = false;
        this.hasDockingPermission = false;
        this.enableLock = false;
        this.dockingStart = 0;
        this.connectionStart = 0;
        this.dockingStep = 2;

        IMyShipConnector connector = this.getAvailableConnector();
        if (connector != null) {
            this.setConnectorState(false);
        }

        this.setPistonState(false);

        if (this.dockingWithDrone != 0) {
            // Send halt dock signal.
            this.navHandle.commHandle.sendStopDocking(reason, this.dockingWithDrone);
            this.dockingWithDrone = 0;
        }
    }

    public bool isDockingInProgress() {
        return this.dockingInProgress;
    }

    public void setPistonState(bool state) {
        List<IMyPistonBase> blocks = new List<IMyPistonBase>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(blocks);
        this.pistonOpen = state;
        foreach (IMyPistonBase block in blocks) {
            if (block.CustomName.Contains("[Drone]")) {
                if (state == true) {
                    block.Extend();
                } else {
                    block.Retract();
                }
            }
        }
    }

    public void handleDockedStep() {
        this.navHandle.gyroHandle.disableOverride();
        Communication.currentNode.status = "docked";
        if (Communication.getTimestamp() - this.connectionStart > 30) {
            this.haltDocking("docking-timeout");
        }
    }

    public void handleFinalStep() {
        Communication.currentNode.status = "docking-step-final";
        this.navHandle.setAutopilotStatus(false);
        if (this.connectionStart > 0) {
            this.handleDockedStep();
        } else {
            if (Communication.masterDrone.dockingHandle.dockingInProgress) {
                IMyShipConnector connector = this.getAvailableConnector();
                if (connector.Status != MyShipConnectorStatus.Connected) {
                    this.setConnectorState(true);
                    this.navHandle.commHandle.sendDockingLockRequest(1);
                    this.navHandle.gyroHandle.rotateShip(3); // Rotate near connector.
                }
            } else {
                Vector3D targetPos = this.getDockingPosition(1);
                double distance = this.navHandle.getDistanceFrom(this.navHandle.getShipPosition(), targetPos);
                if (distance > 2) {
                    this.haltDocking("out-of-docking-bounds");
                }
            }
        }
    }

    public void handleStepOne() {
        Vector3D targetPos = this.getNextDockingPosition();
        Communication.currentNode.status = "docking-step-1";
        this.navHandle.move(targetPos, "docking");
        this.navHandle.setCollisionStatus(true);
        this.navHandle.setCollisionStatus(false);
        this.navHandle.gyroHandle.disableOverride();
    }

    public void handleStepTwo () {
        Communication.currentNode.status = "docking-step-2";
        Vector3D targetPos = this.getNextDockingPosition();
        this.navHandle.move(targetPos, "docking");
        this.navHandle.setCollisionStatus(true);
        this.navHandle.setCollisionStatus(false);
        this.navHandle.commHandle.sendDockingLockRequest(0);
        this.navHandle.gyroHandle.disableOverride();

    }

    public void handleStepQueueing () {
        this.navHandle.gyroHandle.disableOverride();
        if (this.navHandle.commHandle != null) {
            Communication.currentNode.status = "waiting-dock-permissions";
            this.navHandle.commHandle.sendDockingRequest();
            Vector3D queuePos = this.getQueuePosition();
            double distance = this.getDistanceFrom(this.navHandle.getShipPosition(), queuePos);
            if (distance > 50) {
                this.navHandle.move(queuePos, "going-to-queue");
                this.navHandle.setCollisionStatus(false);
                this.navHandle.setCollisionStatus(true);
                this.navHandle.setAutopilotStatus(true);
            } else {
                this.navHandle.commHandle.sendDockingStep(this.dockingStep + 1);
                this.navHandle.setCollisionStatus(true);
                this.navHandle.setAutopilotStatus(false);
            }
        } else {
            Communication.currentNode.status = "failed-communication";
            Display.print("[Error] Communication module failure.");
        }
    }

    public void setConnectorState(bool state) {
        IMyShipConnector connector = this.getAvailableConnector();
        connector.Enabled = state;
    }

    public Vector3D getAnchorPosition(int anchorId) {
        Vector3D anchorPosition = new Vector3D(0, 0, 0);
        List<IMySensorBlock> blocks = new List<IMySensorBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(blocks);
        foreach (IMySensorBlock block in blocks) {
            if (block.CustomName.Contains("[Drone]") && block.CustomName.Contains("[Anchor" + anchorId + "]")) {
                anchorPosition = block.GetPosition();
                break;
            }
        }

        return anchorPosition;
    }

    public Vector3D getAnchoredConnectorPosition(Vector3D anchorBottomPosition, Vector3D anchorTopPosition, double distance) {
        Vector3D result = new Vector3D(0, 0, 0);
        result.X = anchorBottomPosition.X + ((anchorBottomPosition.X - anchorTopPosition.X) * (distance/39));
        result.Y = anchorBottomPosition.Y + ((anchorBottomPosition.Y - anchorTopPosition.Y) * (distance/39));
        result.Z = anchorBottomPosition.Z + ((anchorBottomPosition.Z - anchorTopPosition.Z) * (distance/39));
        return result;
    }

    public Vector3D getQueuePosition() {
        Vector3D targetPos = this.getAnchoredConnectorPosition(Communication.masterDrone.connectorAnchorBottomPosition, Communication.masterDrone.connectorAnchorTopPosition, 1000);

        return targetPos;
    }

    public Vector3D getDockingPosition(int step) {
        Vector3D targetPos = this.getAnchoredConnectorPosition(Communication.masterDrone.connectorAnchorBottomPosition, Communication.masterDrone.connectorAnchorTopPosition, step * 50);

        return targetPos;
    }

    public Vector3D getNextDockingPosition() {
        // Reset if no joy
        if (this.dockingStep <= 0) {
            this.dockingStep = 2;
        }
        Vector3D targetPos = this.getAnchoredConnectorPosition(Communication.masterDrone.connectorAnchorBottomPosition, Communication.masterDrone.connectorAnchorTopPosition, this.dockingStep * 50);
        double distance = this.getDistanceFrom(this.navHandle.getShipPosition(), targetPos);
        if (distance < 2) {
            this.dockingStep--;
            this.navHandle.commHandle.sendDockingStep(this.dockingStep);
            targetPos = this.getAnchoredConnectorPosition(Communication.masterDrone.connectorAnchorBottomPosition, Communication.masterDrone.connectorAnchorTopPosition, this.dockingStep * 50);
        }

        return targetPos;
    }

    public double getDistanceFrom(Vector3D pos, Vector3D pos2) {
        return Math.Round( Vector3D.Distance( pos, pos2 ), 2 );
    }

    public void handleLockingMechanism() {
        IMyShipConnector connector = this.getAvailableConnector();
        if (connector != null) {
            if (this.dockingInProgress == true) {
                if (!connector.Enabled) {
                    connector.Enabled = true;
                }
                if (connector.Status != MyShipConnectorStatus.Connected) {
                    if (this.enableLock == true) {
                        connector.PullStrength = 100;
                        if (connector.Status == MyShipConnectorStatus.Connectable) {
                            connector.Connect();
                            if (this.connectionStart == 0) {
                                this.connectionStart = Communication.getTimestamp();
                            }
                        }
                    }
                }
            }
        }
    }

    public void approveDocking() {
        this.hasDockingPermission = true;
    }

    public void handleDockingProcedure() {
        if (this.dockingInProgress == true) {
            // Make sure drone still exists.
            if (Communication.currentNode.dockingHandle.dockingWithDrone != 0) {
                int nodeIndex = this.navHandle.commHandle.getNodeIndexById(Communication.currentNode.dockingHandle.dockingWithDrone);
                if (nodeIndex == -1) {
                    this.haltDocking("drone-not-found");
                }
            }

            // Last resort timeout
            if (Communication.getTimestamp() - this.dockingStart > 300) { // After 5 minutes of docking attempts, you should abandon the drone.
                this.haltDocking("last-resort-timeout");
            }
            if (Communication.getTimestamp() - this.dockingStart > 10) {
                if (this.connectionStart > 10) {
                    IMyShipConnector connector = this.getAvailableConnector();
                    if (connector != null && connector.Status != MyShipConnectorStatus.Connected) {
                        this.haltDocking("no-connection");
                    }
                }
            }
        } else {
            IMyShipConnector connector = this.getAvailableConnector();
            if (connector != null && connector.Status == MyShipConnectorStatus.Connected || (this.connectionStart > 0 && Communication.getTimestamp() - this.connectionStart > 3)) {
                this.haltDocking("docking-not-in-progress");
            }
        }
    }
}

public class Gyro
{
    public MyGridProgram myGrid;

    public Gyro(MyGridProgram myGrid) {
        this.myGrid = myGrid;
    }

    public IMyGyro getFirstGyro() {
        List<IMyGyro> blocks = new List<IMyGyro>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks);
        foreach (IMyGyro block in blocks) {
            return block;
        }
        return null;
    }

    public MatrixD getOrientation() {
        return Core.coreBlock.WorldMatrix.GetOrientation();
    }

    /*public Vector3D alignWithTarget(Vector3D Target) {
        Vector3D V3Dcenter = RemCon.GetPosition();
        Vector3D V3Dfow = RemCon.WorldMatrix.Forward;
        Vector3D V3Dup = RemCon.WorldMatrix.Up;
        Vector3D V3Dleft = RemCon.WorldMatrix.Left;

        Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);

        double TargetPitch = Math.Acos(Vector3D.Dot(V3Dfow, Vector3D.Reject(Vector3D.Normalize(RemCon.GetNaturalGravity()),V3Dleft))) - (Math.PI/2);

        double TargetRoll = Math.Acos(Vector3D.Dot(V3Dleft, Vector3D.Reject(Vector3D.Normalize(-RemCon.GetNaturalGravity()),V3Dfow))) - (Math.PI / 2);

        return new Vector3D(0, -TargetPitch, TargetRoll);
    }*/


    public void rotateShip(float amount) {
        List<IMyGyro> blocks = new List<IMyGyro>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks);
        foreach (IMyGyro block in blocks) {
            if (!block.CustomName.Contains("[Drone]")) continue;
            if (!block.GyroOverride) {
                block.ApplyAction("Override");
            }
            block.SetValueFloat("Power", 100);
            //block.SetValueFloat("Yaw", (float) orientation.X);
            //block.SetValueFloat("Pitch", (float) orientation.Y);
    		block.SetValueFloat("Roll", amount);
        }
    }

    public void disableOverride() {
        List<IMyGyro> blocks = new List<IMyGyro>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks);
        foreach (IMyGyro block in blocks) {
            if (!block.CustomName.Contains("[Drone]")) continue;
            if (block.GyroOverride) {
                block.ApplyAction("Override");
            }
            block.SetValueFloat("Yaw", 0);
            block.SetValueFloat("Pitch", 0);
    		block.SetValueFloat("Roll", 0);
        }
    }

}

public class Navigation
{
    public MyGridProgram myGrid;
    public List<IMyRemoteControl> remotes { get; set; }
    public List<IMyThrust> thrusters { get; set; }
    public Vector3D lastWaypoint;
    private long lastMovementCommand = 0;
    public List<DetectedEntity> nearbyEntities { get; set; }
    public Docking dockingHandle;
    public Communication commHandle;
    public Gyro gyroHandle;


    public Navigation(MyGridProgram myGrid) {
        this.myGrid = myGrid;
        this.nearbyEntities = new List<DetectedEntity>();
    }

    public void setDockingHandle(Docking dockingHandle) {
        this.dockingHandle = dockingHandle;
    }

    public void setGyroHandle(Gyro gyroHandle) {
        this.gyroHandle = gyroHandle;
    }

    public void setCommunicationHandle(Communication commHandle) {
        this.commHandle = commHandle;
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

    public void updateThrusters() {
        this.thrusters = new List<IMyThrust>();
        List<IMyThrust> handles = new List<IMyThrust>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyThrust>(handles, c => c.BlockDefinition.ToString().ToLower().Contains("thruster"));
        foreach (IMyThrust handle in handles) {
            this.thrusters.Add(handle);
        }
    }

    public void setAutopilotStatus(bool autoPilotStatus) {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.SetAutoPilotEnabled(autoPilotStatus);
        }
    }

    public void setCollisionStatus(bool status) {
        foreach (IMyRemoteControl remote in this.remotes) {
            if (status == true) {
                remote.SetValueBool("CollisionAvoidance", true);
                remote.ApplyAction("CollisionAvoidance_On");
            } else {
                remote.SetValueBool("CollisionAvoidance", false);
                remote.ApplyAction("CollisionAvoidance_Off");
            }
        }
    }

    public void overrideThruster(string direction, float valueFloat) {
        /*foreach (IMyThrust thruster in this.thrusters) {
            if (thruster.GetProperty("Name").GetValue().Contains("(" + direction + ")")) {
                thruster.SetValueFloat("Override", valueFloat);
            }
        }*/
    }

    public double getDistanceFrom(Vector3D pos, Vector3D pos2) {
        return Math.Round( Vector3D.Distance( pos, pos2 ), 2 );
    }

    public void move(Vector3D coords, string waypointName) {
        if (coords.X == 0 && coords.Y == 0 && coords.Z == 0) {
            Display.print("[Error] Unable to set waypoint: " + waypointName);
            return; // Hard lock, in case of an issue @TODO: When you get logging system up and running, log these as exceptions.
        }
        if (this.lastMovementCommand == 0 || (Communication.getTimestamp() - this.lastMovementCommand) > 20) {
            this.lastMovementCommand = Communication.getTimestamp();
            this.clearPath();
            this.lastWaypoint = coords;
            Display.print("Waypoint set: " + waypointName);
            foreach (IMyRemoteControl remote in this.remotes) {
                remote.AddWaypoint(coords, waypointName);
                remote.SetAutoPilotEnabled(true);
            }
        }
    }

    public void setDirection(string direction) {
        foreach (IMyRemoteControl remote in this.remotes) {
            /*if (direction == "Forward") {
                remote.Direction = remote.Forward;
            } else if (direction == "Backward") {
                remote.Direction = remote.Backward;
            } else if (direction == "Left") {
                remote.Direction = remote.Left;
            } else if (direction == "Right") {
                remote.Direction = remote.Right;
            } else if (direction == "Up") {
                remote.Direction = remote.Up;
            } else if (direction == "Down") {
                remote.Direction = remote.Down;
            }*/
            remote.GetActionWithName(direction).Apply(remote);
        }
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

    public void returnToMaster() {
        if (Communication.masterDrone == null) {
            Communication.currentNode.status = "no-master";
            return; // Do nothing.
        }
        // Remove overrides
        this.overrideThruster("Forward", 0);
        double targetDistance = this.getDistanceFrom(this.getShipPosition(), Communication.masterDrone.position);
        if (targetDistance > 200) {
            Communication.currentNode.status = "returning";
            Vector3D queuePos = this.dockingHandle.getQueuePosition();
            this.move(queuePos, "returning");
            this.setCollisionStatus(false);
        } else {
            Communication.currentNode.status = "docking-step-unknown";
            if (Communication.masterDrone.connectorAnchorTopPosition.X != 0) {
                if (this.dockingHandle.hasDockingPermission == true) {
                    if (this.dockingHandle.dockingStep == 0) {
                        this.dockingHandle.handleFinalStep();
                    } else if (this.dockingHandle.dockingStep == 1) {
                        this.dockingHandle.handleStepOne();
                    } else {
                        this.dockingHandle.handleStepTwo();
                    }
                } else {
                    this.dockingHandle.handleStepQueueing();
                }
            } else {
                this.clearPath();
                Communication.currentNode.status = "waiting-for-connector";
            }
        }
    }

    public void addNearbyEntity(DetectedEntity entity) {
        bool found = false;
        long lastSeenByMe = 0;
        DetectedEntity myEnt;
        for (int i = 0; i < this.nearbyEntities.Count; i++) {
            myEnt = this.nearbyEntities[i];
            if (myEnt.id == entity.id) {
                if (myEnt.lastSeen > entity.lastSeen) {
                    entity.lastSeen = myEnt.lastSeen;
                }
                this.nearbyEntities[i] = entity;
                found = true;
            }
        }
        if (found == false) {
            this.nearbyEntities.Add(entity);
        }
    }

    public void updateNearbyCollisionData(IMySensorBlock sensor)
    {
        if (!sensor.LastDetectedEntity.IsEmpty()) {
            MyDetectedEntityInfo entity = sensor.LastDetectedEntity;
            if (!this.nearbyEntities.Any(val => val.id == entity.EntityId)) {
                DetectedEntity tmp = new DetectedEntity();
                tmp.id = entity.EntityId;
                tmp.name = entity.Name;
                tmp.position = entity.Position;
                tmp.lastSeen = Communication.getTimestamp();
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
                        tmp.entityInfo = entity;
                        tmp.lastSeen = Communication.getTimestamp();
                        tmp.distance = this.getDistanceFrom(entity.Position, sensor.GetPosition());
                        tmp.type = entity.Type;
                        this.nearbyEntities[i] = tmp;
                        break;
                    }
                }
            }
        } else {
            List<DetectedEntity> tmp = new List<DetectedEntity>();

            for (int i = 0; i < this.nearbyEntities.Count; i++) {
                DetectedEntity nearEntity = this.nearbyEntities[i];
                if (Communication.getTimestamp() - nearEntity.lastSeen < 300) {
                    tmp.Add(nearEntity);
                }
            }

            this.nearbyEntities = tmp;
        }
    }

    public void clearPath() {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.ClearWaypoints();
        }
    }
}
