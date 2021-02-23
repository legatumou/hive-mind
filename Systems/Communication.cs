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
