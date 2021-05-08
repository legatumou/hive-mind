
public class Drone : NodeData
{
    long lastLoopTime = 0;

    public Drone(int id) : base(id) {}

    public void initiate() {
        AnchoredConnector.setAllConnectorState(false);
        Piston.setAllPistonState(false);
        this.type = "mothership";
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
            this.dockingHandle.clearActiveProcedures();
            this.mainLogic();
            this.lastLoopTime = Communication.getTimestamp();
            this.broadcastActiveConnectors();
        }
        //this.dockingHandle.handleDockingMechanism();
    }

    public void broadcastActiveConnectors() {
        // Remove from active procedure list
        for (int i = 0; i < Docking.activeDockingProcedures.Count; i++) {
            if (Docking.activeDockingProcedures[i].dockingInProgress == true) {
                if (Docking.activeDockingProcedures[i].lastConnectorPing == 0 || Docking.activeDockingProcedures[i].lastConnectorPing - Communication.getTimestamp() > 15) {
                    this.commHandle.sendConnectorData(Docking.activeDockingProcedures[i].dockingWithDrone);
                }
            }
        }
    }

    public void handleIdleConnectors() {
        for (int i = 0; i < AnchoredConnector.anchoredConnectors.Count; i++) {
            if (AnchoredConnector.anchoredConnectors[i].inUse == false) {
                AnchoredConnector.setConnectorState(AnchoredConnector.anchoredConnectors[i].connectorId, false);
                if (AnchoredConnector.anchoredConnectors[i].piston != null) {
                    AnchoredConnector.anchoredConnectors[i].piston.setPistonState(false);
                }
            }
        }
    }

    public void analyzeInventory() {
        List<MyInventoryItem> inventoryItems = this.getInventoryContents();
    }

    public void mainLogic() {
        // None.
    }
}
