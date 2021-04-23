
public class Docking
{
    public MyGridProgram myGrid;

    public static List<DockingProcedure> activeDockingProcedures = new List<DockingProcedure>();

    public bool amAMaster = false;
    public Navigation navHandle;

    public Docking(MyGridProgram myGrid) {
        this.myGrid = myGrid;
    }

    public void setNavHandle(Navigation navHandle) {
        this.navHandle = navHandle;
    }

    public void initDockingWith(int id) {
        DockingProcedure proc = new DockingProcedure(id);
        proc.setNavHandle(this.navHandle);
        proc.initDocking();
    }

    public static DockingProcedure getDockingProcedure(int id) {
        for (int i = 0; i < Docking.activeDockingProcedures.Count; i++) {
            if (Docking.activeDockingProcedures[i].myConnector.connectorId == id) {
                return Docking.activeDockingProcedures[i];
            }
        }
        return null;
    }

    public static DockingProcedure getDroneDockingProcedure(int droneId) {
        for (int i = 0; i < Docking.activeDockingProcedures.Count; i++) {
            if (Docking.activeDockingProcedures[i] != null && Docking.activeDockingProcedures[i].dockingWithDrone == droneId) {
                return Docking.activeDockingProcedures[i];
            }
        }
        return null;
    }

    public static bool dockingWithDrone(int id) {
        for (int i = 0; i < Docking.activeDockingProcedures.Count; i++) {
            if (Docking.activeDockingProcedures[i].dockingWithDrone == id) {
                return true;
            }
        }
        return false;
    }

    public void clearActiveProcedures() {
        // Remove from active procedure list
        for (int i = 0; i < Docking.activeDockingProcedures.Count; i++) {
            if (Docking.activeDockingProcedures[i].dockingInProgress == false && Docking.activeDockingProcedures[i].dockingStart - Communication.getTimestamp() > 300) {
                Docking.activeDockingProcedures[i].haltDocking("mothership-timeout");
                Docking.activeDockingProcedures.RemoveAt(i);
            }
        }
    }

    public void handleDockedStep(DockingProcedure procedure) {
        this.navHandle.gyroHandle.disableOverride();
        this.navHandle.thrusterStatus(false);
        Communication.currentNode.status = "docked";
        if (Communication.getTimestamp() - procedure.connectionStart > 10) {
            if (Communication.currentNode.battery > 25 && Communication.currentNode.playerCommand != "recall") { // Do not disengage if battery too low.
                this.navHandle.thrusterStatus(true);
                procedure.haltDocking("docking-timeout");
            }
        }
    }

    public void handleFinalStep(DockingProcedure procedure) {
        Communication.currentNode.status = "docking-step-final";
        this.navHandle.setAutopilotStatus(false);
        this.navHandle.thrusterStatus(false);
        if (procedure.connectionStart > 0) {
            this.handleDockedStep(procedure);
        } else {
            AnchoredConnector connector = procedure.myConnector;
            if (connector != null) {
                if (connector.block.Status != MyShipConnectorStatus.Connected) {
                    if (connector.block.Status == MyShipConnectorStatus.Connectable) {
                        this.navHandle.gyroHandle.disableOverride();
                        connector.block.Connect();
                    } else {
                        Vector3D targetPos = this.getDockingPosition(1, procedure);
                        double distance = this.getDistanceFrom(this.navHandle.getShipPosition(), targetPos);
                        if (distance > 3) {
                            this.navHandle.setAutopilotStatus(true);
                            this.navHandle.move(targetPos, "docking-realign");
                            this.navHandle.setCollisionStatus(false);
                        } else {
                            this.navHandle.setAutopilotStatus(false);
                            AnchoredConnector.setConnectorState(connector.connectorId, true);
                            this.navHandle.commHandle.sendDockingLockRequest(1);
                            this.navHandle.gyroHandle.rotateShip(2); // Rotate near connector.
                        }
                    }
                } else if (connector.block.Status == MyShipConnectorStatus.Connected) {
                    procedure.connectionStart = Communication.getTimestamp();
                    Communication.currentNode.status = "docking-step-connected";
                }
            } else {
                Display.printDebug("No connectors found, total connectors: " + AnchoredConnector.anchoredConnectors.Count);
                Communication.currentNode.status = "error-connector-not-found";
            }
        }
    }

    public void handleStepOne(DockingProcedure procedure) {
        this.navHandle.thrusterStatus(true);
        this.navHandle.setAutopilotStatus(true);
        Vector3D targetPos = this.getDockingPosition(1, procedure);
        Communication.currentNode.status = "docking-step-1";
        double distance = this.getDistanceFrom(this.navHandle.getShipPosition(), targetPos);
        if (distance < 3) {
            Display.printDebug("Next docking step.");
            procedure.dockingStep--;
        } else {
            this.navHandle.move(targetPos, "docking-step-1");
            this.navHandle.setCollisionStatus(false);
            this.navHandle.gyroHandle.disableOverride();
        }
    }

    public void handleStepTwo(DockingProcedure procedure) {
        this.navHandle.thrusterStatus(true);
        Communication.currentNode.status = "docking-step-2";
        Vector3D targetPos = this.getDockingPosition(5, procedure);
        double distance = this.getDistanceFrom(this.navHandle.getShipPosition(), targetPos);
        if (distance < 3) {
            Display.printDebug("Next docking step.");
            procedure.dockingStep--;
        } else {
            this.navHandle.move(targetPos, "docking-step-2");
            if (distance > 100 && Core.generateRandomId() < 7000) {
                this.navHandle.setCollisionStatus(false);
            } else {
                this.navHandle.setCollisionStatus(true);
            }
            this.navHandle.commHandle.sendDockingLockRequest(0);
            this.navHandle.gyroHandle.disableOverride();
        }
    }

    public void handleStepQueueing(DockingProcedure procedure) {
        this.navHandle.thrusterStatus(true);
        this.navHandle.gyroHandle.disableOverride();
        if (this.navHandle.commHandle != null) {
            Communication.currentNode.status = "waiting-dock-permissions";
            this.navHandle.commHandle.sendDockingRequest();
            if (Communication.masterDrone.masterConnectorId == null) {
                Vector3D queuePos = this.getQueuePosition();
                if (queuePos.X != 0) {
                    double distance = this.getDistanceFrom(this.navHandle.getShipPosition(), queuePos);
                    if (distance > 100) {
                        this.navHandle.move(queuePos, "going-to-queue");
                        this.navHandle.setCollisionStatus(true);
                        this.navHandle.setAutopilotStatus(true);
                    }
                }
                Communication.currentNode.status = "waiting-for-connector";
                return;
            } else {
                Vector3D queuePos = this.getQueuePosition();
                double distance = this.getDistanceFrom(this.navHandle.getShipPosition(), queuePos);
                if (queuePos.X != 0 && distance > 50) {
                    Communication.currentNode.status = "navigating-to-queue";
                    this.navHandle.move(queuePos, "going-to-queue");
                    this.navHandle.setCollisionStatus(true);
                    this.navHandle.setAutopilotStatus(true);
                } else {
                    if (procedure != null) {
                        Communication.currentNode.status = "waiting-in-queue";
                        this.navHandle.commHandle.sendDockingStep(procedure.dockingStep + 1);
                        this.navHandle.setCollisionStatus(false);
                        this.navHandle.setAutopilotStatus(false);
                    }
                }
            }
        } else {
            Communication.currentNode.status = "failed-communication";
            Display.print("[Error] Communication module failure.");
        }
    }

    public Vector3D getMasterPosition(double distance) {
        Vector3D anchorBottomPosition = Communication.masterDrone.connectorAnchorBottomPosition;
        Vector3D anchorTopPosition = Communication.masterDrone.connectorAnchorTopPosition;
        Vector3D result = new Vector3D(0, 0, 0);
        result.X = anchorBottomPosition.X + ((anchorBottomPosition.X - anchorTopPosition.X) * (distance/39));
        result.Y = anchorBottomPosition.Y + ((anchorBottomPosition.Y - anchorTopPosition.Y) * (distance/39));
        result.Z = anchorBottomPosition.Z + ((anchorBottomPosition.Z - anchorTopPosition.Z) * (distance/39));
        return result;
    }

    public Vector3D getQueuePosition() {
        if (Communication.masterDrone != null && Communication.masterDrone.position.X != 0) {
            Vector3D targetPos = this.getMasterPosition(10 * 50);
            return targetPos;
        }
        return new Vector3D(0,0,0);
    }

    public Vector3D getDockingPosition(int step, DockingProcedure procedure) {
        if (Communication.masterDrone != null && Communication.masterDrone.connectorAnchorTopPosition.X != null) {
            Vector3D targetPos = this.getMasterPosition(step * 50);
            return targetPos;
        }
        return new Vector3D(0,0,0);
    }

    public double getDistanceFrom(Vector3D pos, Vector3D pos2) {
        return Math.Round( Vector3D.Distance( pos, pos2 ), 2 );
    }

    public void handleDockingProcedure() {
        for (int i = 0; i < Docking.activeDockingProcedures.Count; i++) {
            Docking.activeDockingProcedures[i].handleProcedure();
        }
        this.clearActiveProcedures();
    }

    public void handleDockingMechanism() {
        for (int i = 0; i < Docking.activeDockingProcedures.Count; i++) {
            Docking.activeDockingProcedures[i].handleLockingMechanism();
        }
    }
}
