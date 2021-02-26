
public class Drone : NodeData
{
    long lastLoopTime = 0;
    private IMyProjector myProjector;
    public Vector3D myReplicationPosition =  new Vector3D(0,0,0);
    private bool replicatingInProgress = false;
    private string currentlyReplicatingType = "SmallMining";

    public static List<string> blueprintList = new List<string>() { "SmallMining", "SmallProjector" };

    public Drone(int id) : base(id) {}

    public void initiate() {
        this.type = "projector";
        this.myProjector = this.getProjector();
    }

    public void execute() {
        if (this.lastLoopTime == 0 || Communication.getTimestamp() - this.lastLoopTime > 3) {
            this.mainLogic();
            this.lastLoopTime = Communication.getTimestamp();
        }
    }

    public void mainLogic() {
        if (this.movingFromCreator) {
            int nodeIndex = this.commHandle.getNodeIndexById(this.creatorId);
            if (nodeIndex != -1) {
                Drone creator = Communication.connectedNodesData[nodeIndex];
                if (creator.position.X != 0) {
                    Communication.currentNode.status = "finding-home";
                    creator.position.X += 5000;
                    creator.position.Y += 5000;
                    creator.position.Z += 5000;
                    this.navHandle.setCollisionStatus(true);
                    this.navHandle.move(creator.position, "finding-home");
                }
            }
        }
        if (Communication.masterDrone == null) {
            this.status = "waiting-for-master";
            this.commHandle.sendMasterRequest();
            return;
        }
        this.replicate();
    }

    public IMyShipGrinder getGrinder() {
        List<IMyShipGrinder> blocks = new List<IMyShipGrinder>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(blocks);
        foreach (IMyShipGrinder block in blocks) {
            if (block.CustomName.Contains("[Drone]")) {
                return block;
            }
        }
        return null;
    }

    public Vector3D getReplicationPosition() {
        if (Communication.masterDrone != null) {
            Vector3D targetPos = Communication.masterDrone.position;
            Random rand = new Random();
            targetPos.X += rand.Next(20, 100); // Offset from ship
            targetPos.Y += rand.Next(20, 100); // Offset from ship
            targetPos.Z += rand.Next(20, 100); // Offset from ship
            return targetPos;
        }
        return new Vector3D(0,0,0);
    }

    public IMyProjector getProjector() {
        List<IMyProjector> blocks = new List<IMyProjector>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyProjector>(blocks);
        foreach (IMyProjector block in blocks) {
            if (block.CustomName.Contains("[Drone]") && block.CustomName.Contains("[" + this.currentlyReplicatingType + "]")) {
                return block;
            }
        }
        return null;
    }

    public void changeReplicatingType(int typeIndex) {
        this.currentlyReplicatingType = Drone.blueprintList[typeIndex];
        this.myProjector = this.getProjector();
    }

    public void replicate() {
        if (this.battery < 5) {
            return; // Solar cells need to recharge.
        }
        IMyProjector myProjector = this.myProjector;
        IMyShipGrinder grinder = this.getGrinder();
        Vector3D replicationPosition = this.myReplicationPosition;
        if (replicationPosition.X != 0) {
            double distance = this.navHandle.getDistanceFrom(replicationPosition, Communication.masterDrone.position);
            if (distance > 100) {
                // too far from nanites, try again.
                this.status = "finding-replication-position-" + distance;
                this.myReplicationPosition = this.getReplicationPosition();
                return;
            }
            if (grinder != null) {
                if (myProjector != null) {
                    distance = this.navHandle.getDistanceFrom(replicationPosition, this.navHandle.getShipPosition());
                    if (distance < 5) {
                        this.navHandle.setAutopilotStatus(false);
                        this.navHandle.setCollisionStatus(false);
                        if (this.replicatingInProgress == true && myProjector.RemainingArmorBlocks < 5) { // Acceptable loss
                            if (myProjector.RemainingBlocks == myProjector.RemainingArmorBlocks) {
                                // Turn on other drone logic block.
                                this.turnOnDrones();
                                // Cut it free
                                grinder.Enabled = true; // release the new ship.
                                this.commHandle.sendMasterFinishedSignal(this.id);
                                this.replicatingInProgress = false;
                                this.status = "finishing-replication";
                                // @TODO: switch to another projector?
                            } else {
                                this.status = "non-armor-blocks-remainig";
                            }
                        } else {
                            this.replicatingInProgress = true;
                            this.status = "replicating";
                            myProjector.Enabled = true;
                        }
                    } else {
                        this.navHandle.move(replicationPosition, "replicating-position");
                        this.status = "moving-to-position";
                        this.navHandle.setCollisionStatus(true);
                    }
                } else {
                    grinder.Enabled = false;
                    this.status = "missing-projector";
                }
            } else {
                this.status = "unable-to-replicate";
            }
        } else {
            this.myReplicationPosition = this.getReplicationPosition();
            this.status = "waiting-for-position";
        }
    }
}
