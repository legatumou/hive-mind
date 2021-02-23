
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
