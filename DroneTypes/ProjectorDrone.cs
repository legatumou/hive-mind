
public class Drone : NodeData
{
    long lastLoopTime = 0;
    private IMyProjector myProjector;
    private bool replicatingInProgress = false;
    private string currentlyReplicatingType = "SmallMining";
    private long grinderStart = 0;

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

    public IMyProjector getProjector() {
        List<IMyProjector> blocks = new List<IMyProjector>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyProjector>(blocks);
        foreach (IMyProjector block in blocks) {
            if (block.CustomName.Contains("[Drone]")) {
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
        if (grinder != null) {
            if (myProjector != null) {
                if (this.grinderStart != 0 && Communication.getTimestamp() - this.grinderStart > 30) {
                    this.grinderStart = 0;
                    grinder.Enabled = false;
                }

                if (this.replicatingInProgress == true) { // Acceptable loss
                    if (myProjector.RemainingBlocks == myProjector.RemainingArmorBlocks) {
                        // Turn on other drone logic block.
                        this.turnOnDrones();
                        // Cut it free
                        grinder.Enabled = true; // release the new ship.
                        this.grinderStart = Communication.getTimestamp();
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
                    grinder.Enabled = false;
                    myProjector.Enabled = true;
                }
            } else {
                grinder.Enabled = false;
                this.status = "missing-projector";
            }
        } else {
            this.status = "unable-to-replicate";
        }
    }
}
