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
    public MyGridProgram myGrid;
    private long lastRequest = 0;
    private long lastEntityDataUpdate = 0;
    private long lastDockLockRequest = 0;
    private long lastDockingStep = 0;
    private bool debug = true;

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
            if (Communication.connectedNodesData[i].keepalive != 0 && Communication.getTimestamp() - Communication.connectedNodesData[i].keepalive > 60) {
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
            Vector3D pos = Communication.currentNode.navHandle.getShipPosition();
            string[] data = {
                Communication.currentNode.battery.ToString("R"),
                Communication.currentNode.speed.ToString("R"),
                Communication.currentNode.type,
                Communication.currentNode.status,
                /*this.myGrid.Me.EntityId.ToString("R")*/"0",
                pos.X.ToString("R"),
                pos.Y.ToString("R"),
                pos.Z.ToString("R"),
                Communication.currentNode.usedInventorySpace.ToString(),

            };
            this.broadcastMessage("drone-generic-data-" + Communication.currentNode.id + "_" + string.Join("_", data) );
            //Display.print("Broadcast: " + "drone-data-" + Communication.currentNode.id + '_' + string.Join('_', data));
            this.lastDataUpdate = Communication.getTimestamp();
        }
    }

    public void sendNearbyEntityList() {
        if (this.lastEntityDataUpdate == 0 || Communication.getTimestamp() - this.lastEntityDataUpdate > 35) {
            this.dataStructure.newPackage();
            this.dataStructure.addRawData("drone-data-nearby");
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

    public void sendConnectorData(int slaveId) {
        Display.printDebug("Sending connector info.");
        DockingProcedure procedure = Docking.getDroneDockingProcedure(slaveId);
        if (procedure != null) {
            AnchoredConnector connector = procedure.myConnector;
            if (connector != null && connector.isAnchored) {
                Vector3D pos;
                this.dataStructure.newPackage();
                this.dataStructure.addRawData("drone-connector-data");
                this.dataStructure.addData("id", Communication.currentNode.id.ToString());
                this.dataStructure.addData("slaveId", slaveId.ToString());
                this.dataStructure.addData("masterConnectorId", connector.connectorId.ToString());
                pos = connector.anchorTop.block.GetPosition();
                this.dataStructure.addData("connectorAnchorTopX", pos.X.ToString());
                this.dataStructure.addData("connectorAnchorTopY", pos.Y.ToString());
                this.dataStructure.addData("connectorAnchorTopZ", pos.Z.ToString());
                pos = connector.anchorBottom.block.GetPosition();
                this.dataStructure.addData("connectorAnchorBottomX", pos.X.ToString());
                this.dataStructure.addData("connectorAnchorBottomY", pos.Y.ToString());
                this.dataStructure.addData("connectorAnchorBottomZ", pos.Z.ToString());
                this.broadcastMessage(this.dataStructure.generateOutput());
                procedure.approveDocking();
            } else {
                Display.printDebug("[Error] No working connectors found. (Connectors: " + AnchoredConnector.anchoredConnectors.Count + ")");
            }
        } else {
            Display.printDebug("[Error] No docking procedure found.");
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
        this.dataStructure.addRawData("drone-halt-docking");
        this.dataStructure.addData("id", nodeId.ToString());
        this.dataStructure.addData("reason", reason);
        this.dataStructure.addData("dockingWithId", Communication.currentNode.id.ToString());
        this.broadcastMessage(this.dataStructure.generateOutput());
    }

    public void sendDockingLockRequest(int status) {
        if (this.lastDockLockRequest == 0 || Communication.getTimestamp() - this.lastDockLockRequest > 10) {
            Display.print("Requesting a dock lock.");
            this.broadcastMessage("drone-request-dock-lock-" + Communication.masterDrone.id + '_' + status + "_" + Communication.currentNode.id);
            this.lastDockLockRequest = Communication.getTimestamp();
        }
    }

    public void sendMasterFinishedSignal(int id) {
        this.dataStructure.newPackage();
        this.dataStructure.addRawData("drone-master-finished");
        this.dataStructure.addData("id", Communication.currentNode.id.ToString());
        this.broadcastMessage(this.dataStructure.generateOutput());
    }

    public void sendDockingStep(int step) {
        if (this.lastDockingStep == 0 || Communication.getTimestamp() - this.lastDockingStep > 10) {
            this.dataStructure.newPackage();
            this.dataStructure.addRawData("drone-docking-step");
            this.dataStructure.addData("id", Communication.masterDrone.id.ToString());
            this.dataStructure.addData("slaveId", Communication.currentNode.id.ToString());
            this.dataStructure.addData("connectorId", Communication.masterDrone.masterConnectorId.ToString());
            this.dataStructure.addData("step", step.ToString());
            this.broadcastMessage(this.dataStructure.generateOutput());
            this.lastDockingStep = Communication.getTimestamp();
        }
    }

    public void broadcastMessage(string messageOut) {
        string tag1 = "drone-channel";
        if (this.debug == true) {
            Display.printDebug("[OUT] " + messageOut);
        }

        string[] dataSplitted = messageOut.Split('_');
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
                if (this.debug == true) {
                    Display.printDebug("[IN] " + msg.Data.ToString());
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
                } else if ( msg.Data.ToString().Substring(0, "drone-connector-data".Length) == "drone-connector-data" ) {
                    string data = msg.Data.ToString().Substring("drone-connector-data".Length);
                    this.handleConnectorData(this.dataStructure.getFormattedInput(data));
                } else if ( msg.Data.ToString().Substring(0, "drone-master-finished".Length) == "drone-master-finished" ) {
                    string data = msg.Data.ToString().Substring("drone-master-finished".Length);
                    this.handleMasterFinished(this.dataStructure.getFormattedInput(data));
                }
            }
        }
    }

    public void handleMasterFinished(List<CommunicationDataStructureValue> responseData) {
        int id = 0;
        foreach (CommunicationDataStructureValue data in responseData) {
            if (data.getName() == "id") {
                id = int.Parse(data.getValue());
            }
        }
        if (id != Communication.currentNode.id && Communication.currentNode.isMasterNode()) {
            Communication.currentNode.moveAwayFromCreator(id);
        }
    }

    public void handleDockingStep(List<CommunicationDataStructureValue> responseData) {
        if (Communication.currentNode.type != "mothership") return;
        int id = 0, slaveId = 0, step = 0, connectorId = 0;
        foreach (CommunicationDataStructureValue data in responseData) {
            if (data.getName() == "id") {
                id = int.Parse(data.getValue());
            } else if (data.getName() == "slaveId") {
                slaveId = int.Parse(data.getValue());
            } else if (data.getName() == "step") {
                step = int.Parse(data.getValue());
            } else if (data.getName() == "connectorId") {
                connectorId = int.Parse(data.getValue());
            }
        }
        if (id == Communication.currentNode.id) {
            if (slaveId != 0 && !Docking.dockingWithDrone(slaveId)) {
                this.sendStopDocking("out-of-order", slaveId);
            } else {
                DockingProcedure procedure = Docking.getDroneDockingProcedure(slaveId);
                if (procedure == null || procedure.myConnector.connectorId != connectorId) {
                    this.sendStopDocking("out-of-order", slaveId);
                }
            }
        }
    }

    public void handleConnectorData(List<CommunicationDataStructureValue> responseData) {
        int id = 0, slaveId = 0, masterConnectorId = 0;
        Vector3D anchorTop = new Vector3D(0,0,0), anchorBottom = new Vector3D(0,0,0);
        foreach (CommunicationDataStructureValue data in responseData) {
            if (data.getName() == "id") {
                id = int.Parse(data.getValue());
            } else if (data.getName() == "slaveId") {
                slaveId = int.Parse(data.getValue());
            } else if (data.getName() == "masterConnectorId") {
                masterConnectorId = int.Parse(data.getValue());
            } else if (data.getName() == "connectorAnchorTopX") {
                anchorTop.X = double.Parse(data.getValue());
            } else if (data.getName() == "connectorAnchorTopY") {
                anchorTop.Y = double.Parse(data.getValue());
            } else if (data.getName() == "connectorAnchorTopZ") {
                anchorTop.Z = double.Parse(data.getValue());
            } else if (data.getName() == "connectorAnchorBottomX") {
                anchorBottom.X = double.Parse(data.getValue());
            } else if (data.getName() == "connectorAnchorBottomY") {
                anchorBottom.Y = double.Parse(data.getValue());
            } else if (data.getName() == "connectorAnchorBottomZ") {
                anchorBottom.Z = double.Parse(data.getValue());
            }
        }
        if (id != 0 && slaveId == Communication.currentNode.id) {
            Display.printDebug("ConnectorData: " + id + " | XPos: " + anchorTop.X);
            int nodeIndex = this.getNodeIndexById(id);
            if (nodeIndex != -1) {
                Communication.connectedNodesData[nodeIndex].connectorAnchorTopPosition = anchorTop;
                Communication.connectedNodesData[nodeIndex].connectorAnchorBottomPosition = anchorBottom;
            } else {
                Communication.connectedNodes.Add(id);
                Drone node = new Drone(id);
                node.initNavigation(this.myGrid);
                Communication.connectedNodesData.Add(node);
                nodeIndex = this.getNodeIndexById(id);
            }
            // Update if also master.
            if (Communication.masterDrone != null && id == Communication.masterDrone.id) {
                Communication.masterDrone = Communication.connectedNodesData[nodeIndex];
                Communication.masterDrone.masterConnectorId = masterConnectorId;
            }
        }
    }

    public void handleHaltDocking(List<CommunicationDataStructureValue> responseData) {
        int id = 0;
        int dockingWithId = 0;
        string reason = "unknown-connection";
        foreach (CommunicationDataStructureValue data in responseData) {
            if (data.getName() == "id") {
                id = int.Parse(data.getValue());
            } else if (data.getName() == "reason") {
                reason = data.getValue();
            } else if (data.getName() == "dockingWithId") {
                dockingWithId = int.Parse(data.getValue());
            }
        }
        if (id != 0 && id == Communication.currentNode.id) {
            Display.printDebug("[Incoming] Halt docking reason " + reason);
            DockingProcedure procedure = Docking.getDroneDockingProcedure(dockingWithId);
            if (procedure != null) {
                procedure.haltDocking(reason, false);
            }
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
        if (Communication.currentNode.type != "mothership") return; // Motherships handle docking requests
        string[] dataSplitted = data.Split('_');
        if (dataSplitted.Count() == 3) {
            int id = int.Parse(dataSplitted[0]);
            if (Communication.currentNode.id != id) return; // If not my id
            int status = int.Parse(dataSplitted[1]);
            int slaveId = int.Parse(dataSplitted[2]);
            DockingProcedure procedure = Docking.getDroneDockingProcedure(slaveId);
            if (procedure != null) {
                Display.printDebug("[INFO] Changing piston state.");
                procedure.myConnector.piston.setPistonState((bool) (status == 1));
                if (procedure.myConnector != null) {
                    AnchoredConnector.setConnectorState(procedure.myConnector.connectorId, (bool) (status == 1));
                }
            } else {
                Display.printDebug("[WARN] Docking procedure not found.");
            }
        }
    }

    public void handleDockingAccepted(string data) {
        if (Communication.currentNode.type == "mothership") return; // Motherships handle docking requests
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
            if (Docking.dockingWithDrone(masterId)) {
                DockingProcedure procedure = Docking.getDroneDockingProcedure(masterId);
                procedure.haltDocking("docking-already-in-progress");
            }
            DockingProcedure dock = new DockingProcedure(masterId);
            dock.initDocking();
            dock.approveDocking();
            dock.setNavHandle(Communication.currentNode.navHandle);
            Docking.activeDockingProcedures.Add(dock);
            Communication.currentNode.navHandle.activeDockingProcedure = dock;
        }
    }

    public void handleDockingRequest(string data) {
        if (Communication.currentNode.type != "mothership") return; // Motherships handle docking requests
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
            AnchoredConnector available = AnchoredConnector.getAvailableAnchoredConnector();
            if (available == null) {
                Display.print("Docking request denied (Connectors full).");
            } else {
                if (Docking.dockingWithDrone(slaveId)) {
                    Display.print("Already accepted, continue on docking.");
                    this.sendDockingAccepted(slaveId);
                    this.sendConnectorData(slaveId);
                } else {
                    Display.print("Assigning a proper connector.");
                    DockingProcedure dock = new DockingProcedure(slaveId);
                    dock.setNavHandle(Communication.currentNode.navHandle);
                    dock.initDocking();
                    dock.myConnector = available;
                    Docking.activeDockingProcedures.Add(dock);
                    this.sendDockingAccepted(slaveId);
                    this.sendConnectorData(slaveId);
                }
            }
        } else {
            Display.print("[ERROR] Docking request invalid. (" + data + ")");
        }
    }

    public void handleMasterAcceptance(string data) {
        if (Communication.currentNode.type == "mothership") return; // Motherships are the masters.
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
                Communication.masterDrone = node;
            } else {
                Communication.masterDrone = Communication.connectedNodesData[nodeIndex];
            }
        }

    }

    public void handleMasterRequest(int id) {
        if (Communication.currentNode.type != "mothership") return; // Motherships are the masters.
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
        int fieldCount = 10;
        string[] dataSplitted = data.Split('_');
        if (dataSplitted.Count() == fieldCount) {
            int id = int.Parse(dataSplitted[0]);
            if (id == Communication.currentNode.id) return;
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

            // drone.usedInventorySpace
            Communication.connectedNodesData[nodeIndex].usedInventorySpace = int.Parse(dataSplitted[9]);

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
