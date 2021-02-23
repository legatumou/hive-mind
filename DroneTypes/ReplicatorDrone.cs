
public class Drone : NodeData
{
    long lastLoopTime = 0;

    public Drone(int id) : base(id) {}

    public void initiate() {
        Communication.currentNode.dockingHandle.setPistonState(false);
        this.type = "replicator";
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
            this.lastLoopTime = Communication.getTimestamp();
        }
            this.dockingHandle.handleLockingMechanism();
    }

    public void handleDockingQueue() {
        foreach (Drone drone in Communication.connectedNodesData) {
            if (drone.dockingHandle.dockingWithDrone == Communication.currentNode.id) {
                if (Communication.currentNode.dockingHandle.dockingWithDrone != drone.id) {
                    Communication.currentNode.commHandle.sendStopDocking("master-out-of-order", drone.id);
                    Display.print("[Command] Stop docking with " + drone.id);
                }
            }
        }
    }

    public void analyzeAsteroidMap() {

    }

    public void analyzeInventory() {
        List<MyInventoryItem> inventoryItems = this.getInventoryContents();
    }

    public void commandDrones() {

    }
}
