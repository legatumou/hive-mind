
public class Drone : NodeData
{
    long lastLoopTime = 0;
    private bool replicatingInProgress = false;
    private IMyProjector myProjector;

    public Drone(int id) : base(id) {}

    public void initiate() {
        AnchoredConnector.setAllConnectorState(false);
        Piston.setAllPistonState(false);
        this.type = "replicator";
        this.myProjector = this.getProjector();
    }

    public void execute() {
        if (this.lastLoopTime == 0 || Communication.getTimestamp() - this.lastLoopTime > 3) {
            /*var blocks = new List<MyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(blocks);
            var antenna = blocks[0];

            blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(blocks);
            var assembler = blocks[0];

            var inventoryOwner = (IMyInventoryOwner)assembler;

            var items = inventoryOwner.GetInventory(0).GetItems();

            if (items.Count > 0) {
                antenna.SetCustomName("First item in assembler: " + items[0].Content.SubtypeName);
                inventoryOwner.GetInventory(0).TransferItemTo(destInventory, 0, null, true, null);
            } else {
                antenna.SetCustomName("No items in assembler!");
            }*/


            this.dockingHandle.handleDockingProcedure();
            this.mainLogic();
            this.lastLoopTime = Communication.getTimestamp();
        }
        this.dockingHandle.handleDockingMechanism();
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

    public void replicate() {
        IMyProjector myProjector = this.myProjector;
        IMyShipGrinder grinder = this.getGrinder();
        if (grinder != null) {
            if (myProjector != null) {
                if (this.replicatingInProgress == true && myProjector.RemainingBlocks == 0) {
                    // Turn on other drone logic block.
                    this.turnOnDrones();
                    // Cut it free
                    grinder.Enabled = true; // release the new ship.
                    this.commHandle.sendMasterFinishedSignal(this.id);
                    this.replicatingInProgress = false;
                    this.status = "finishing-replication";
                } else {
                    this.replicatingInProgress = true;
                    this.status = "replicating";
                    myProjector.Enabled = true;
                }
            } else {
                grinder.Enabled = false;
            }
        } else {
            this.status = "unable-to-replicate";
        }
    }

    public void analyzeInventory() {
        List<MyInventoryItem> inventoryItems = this.getInventoryContents();
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
        // Disabled, multi grid projecting is an issue.
        //this.replicate();
    }
}
