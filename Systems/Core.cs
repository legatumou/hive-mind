public class Core
{
    private long lastPositionUpdate = 0;
    private Vector3D lastPosition = new Vector3D(0,0,0);
    private MyGridProgram myGrid;
    public static IMyProgrammableBlock coreBlock;
    public static string myShipReference = "";

    public Core(MyGridProgram myGrid) {
        this.myGrid = myGrid;
        if (myGrid.Me == null) {
            throw new Exception("Unable to find core block.");
        } else {
            Core.coreBlock = myGrid.Me;
        }
    }

    public void execute() {
        Communication.currentNode.process(this.myGrid);
        Communication.currentNode.execute();
    }

    public void handleCoreBlock() {
        if (Core.coreBlock != null) {
            List<CustomData> customData = CustomData.getCustomData(Core.coreBlock.CustomData);
            if (customData.Count > 0) {
                CustomData shipReference = CustomData.findKeyFromList("shipReference", customData);
                if (shipReference.value != "") {
                    Core.myShipReference = shipReference.value;
                }
            }
        }
    }

    public static bool isLocal(IMyTerminalBlock block) {
        List<CustomData> customData = CustomData.getCustomData(block.CustomData);
        if (customData.Count > 0) {
            CustomData shipReference = CustomData.findKeyFromList("shipReference", customData);
            if (shipReference.value != "") {
                if (shipReference.value == Core.myShipReference) {
                    return true;
                }
            }
        }

        if (Core.coreBlock == null) {
            Display.printDebug("Core block missing. RandomID: " + Core.generateRandomId());
            return false;
        }
        return (block.CubeGrid == Core.coreBlock.CubeGrid);
    }

    public static bool generateRandomPercent(int percent) {
        Random rnd = new Random();
        return rnd.Next(1, 100) <= percent;
    }

    public static int generateRandomId() {
        Random rnd = new Random();
        return rnd.Next(1, 10000);
    }

    public void updateDroneData() {
        List<IMyBatteryBlock> vBatteries = new List<IMyBatteryBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(vBatteries, c => c.BlockDefinition.ToString().ToLower().Contains("battery"));

        float maxStorage = 0;
        float storage = 0;

        foreach (IMyBatteryBlock block in vBatteries) {
            maxStorage += this.getPowerAsInt(this.getDetailedInfoValue(block, "Max Stored Power"));
            storage += this.getPowerAsInt(this.getDetailedInfoValue(block, "Stored power"));
        }
        Communication.currentNode.battery = (storage / maxStorage) * 100;
        Communication.currentNode.usedInventorySpace = Communication.currentNode.getInventoryUsedSpacePercentage();

        if (this.lastPositionUpdate == 0 || Communication.getTimestamp() - this.lastPositionUpdate > 0) {
            this.lastPositionUpdate = Communication.getTimestamp();
            Vector3D currentPosition = this.myGrid.Me.GetPosition();
            Communication.currentNode.speed = ((currentPosition-this.lastPosition)*60).Length();
            this.lastPosition = currentPosition;
        }
    }

    public string getDetailedInfoValue(IMyBatteryBlock block, string name) {
        string value = "";
        string[] lines = block.DetailedInfo.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string[] line = lines[i].Split(':');
            if (line[0].Equals(name))
            {
                value = line[1].Substring(1);
                break;
            }
        }
        return value;
    }

    public int getPowerAsInt(string text) {
        if (String.IsNullOrWhiteSpace(text)) {
            return 0;
        }
        string[] values = text.Split(' ');
        if (values[1].Equals("kW")) {
            return (int) (float.Parse(values[0])*1000f);
        } else if (values[1].Equals("kWh")) {
            return (int) (float.Parse(values[0])*1000f);
        } else if (values[1].Equals("MW")) {
            return (int) (float.Parse(values[0])*1000000f);
        } else if (values[1].Equals("MWh")) {
            return (int) (float.Parse(values[0])*1000000f);
        } else if (values[1].Equals("GW")) {
            return (int) (float.Parse(values[0])*1000000000f);
        } else if (values[1].Equals("GWh")) {
            return (int) (float.Parse(values[0])*1000000000f);
        } else {
            return (int) float.Parse(values[0]);
        }
    }
}
