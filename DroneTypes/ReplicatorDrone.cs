
public class ReplicatorDrone : NodeData
{
    public ReplicatorDrone(int id) : base(id) {}

    public void execute() {
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
    }
}
