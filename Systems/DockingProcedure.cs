
public class DockingProcedure
{
    public int dockingWithDrone = 0;
    public string currentStatus;
    public bool dockingInProgress = false;
    public bool hasDockingPermission = false;
    public bool enableLock = false;
    public long dockingStart = 0;
    public long connectionStart = 0;
    public bool pistonOpen = true;
    public int dockingStep = 2;
    public int queuePos = 0;

    public AnchoredConnector myConnector;
    public Navigation navHandle;

    public DockingProcedure(int nodeId) {
        this.dockingWithDrone = nodeId;
    }

    public void initDocking() {
        this.dockingInProgress = true;
        this.enableLock = true;
        this.hasDockingPermission = false;
        this.dockingStep = 2;
        this.queuePos = 0;
        this.connectionStart = 0;
        this.dockingStart = Communication.getTimestamp();
        this.myConnector = AnchoredConnector.getAvailableConnector();
        if (this.myConnector != null) {
            AnchoredConnector.setConnectorState(this.myConnector.connectorId, true);
        } else {
            Display.printDebug("[ERROR] No available connector found for docking.");
        }
    }

    public void haltDocking(string reason = "unknown", bool sendSignal = true) {
        Display.printDebug("[INFO] Halting docking, reason: " + reason);
        this.dockingInProgress = false;
        this.hasDockingPermission = false;
        this.enableLock = false;
        this.dockingStart = 0;
        this.connectionStart = 0;
        this.dockingStep = 2;

        if (this.myConnector != null) {
            if (this.myConnector.block != null && this.myConnector.block.Status == MyShipConnectorStatus.Connected) {
                this.myConnector.block.Disconnect();
            }
            if (this.myConnector.connectorId != null) {
                AnchoredConnector.setConnectorState(this.myConnector.connectorId, false);
                if (this.myConnector.piston != null) {
                    this.myConnector.piston.setPistonState(false);
                }
            }
        }

        if (this.dockingWithDrone != 0 && sendSignal) {
            // Send halt dock signal.
            this.navHandle.commHandle.sendStopDocking(reason, this.dockingWithDrone);
            this.dockingWithDrone = 0;
        }

        if (Communication.masterDrone != null && Communication.masterDrone.masterConnectorId != 0) {
            Communication.masterDrone.masterConnectorId = 0;
        }

        if (Communication.currentNode.navHandle.activeDockingProcedure != null) {
            Communication.currentNode.navHandle.activeDockingProcedure = null;
        }
    }

    public void setNavHandle(Navigation navHandle) {
        this.navHandle = navHandle;
    }

    public void approveDocking() {
        this.hasDockingPermission = true;
    }

    public bool isDockingInProgress() {
        return this.dockingInProgress;
    }

    public void handleLockingMechanism() {
        if (this.myConnector != null) {
            if (this.dockingInProgress == true) {
                if (!this.myConnector.block.Enabled) {
                    this.myConnector.block.Enabled = true;
                }
                if (this.myConnector.block.Status != MyShipConnectorStatus.Connected) {
                    if (this.enableLock == true) {
                        if (this.myConnector.block.Status == MyShipConnectorStatus.Connectable) {
                            this.myConnector.block.Connect();
                            if (this.connectionStart == 0) {
                                this.connectionStart = Communication.getTimestamp();
                            }
                        }
                    }
                }
            }
        }
    }

    public void handleProcedure() {
        if (this.dockingInProgress == true) {
            // Make sure drone still exists.
            if (this.dockingWithDrone != 0) {
                int nodeIndex = this.navHandle.commHandle.getNodeIndexById(this.dockingWithDrone);
                if (nodeIndex == -1) {
                    this.haltDocking("drone-not-found");
                }
            } else {
                this.haltDocking("drone-not-found");
            }

            // Last resort timeout
            if (Communication.getTimestamp() - this.dockingStart > 300) { // After 5 minutes of docking attempts, you should abandon the drone.
                this.haltDocking("last-resort-timeout");
            }
            if (this.dockingStart != 0 && Communication.getTimestamp() - this.dockingStart > 10) {
                if (this.connectionStart != 0 && Communication.getTimestamp() - this.connectionStart > 10) {
                    AnchoredConnector connector = this.myConnector;
                    if (connector != null && connector.block.Status != MyShipConnectorStatus.Connected) {
                        this.haltDocking("no-connection");
                    }
                }
            }
        } else {
            if (this.myConnector != null) {
                IMyShipConnector connector = this.myConnector.block;
                if (this.connectionStart > 0 && Communication.getTimestamp() - this.connectionStart > 15) {
                    this.haltDocking("docking-not-in-progress");
                }
                if (connector.Status == MyShipConnectorStatus.Connected) {
                    this.haltDocking("docking-has-ended");
                }
            } else {
                this.haltDocking("connector-is-missing");
            }
        }
    }
}
