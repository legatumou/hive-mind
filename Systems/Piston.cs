
public class Piston
{
    public MyGridProgram myGrid;
    public IMyPistonBase block;
    public List<CustomData> customData;

    public static List<Piston> pistons = new List<Piston>();

    public Piston(IMyPistonBase block, List<CustomData> customData) {
        this.block = block;
        this.customData = customData;
    }

    public static Piston getPistonByConnector(int connectorId) {
        string connectorIdString;
        foreach (Piston piston in Piston.pistons) {
            connectorIdString = CustomData.findKeyFromList("connectorId", piston.customData).value;
            if (!String.IsNullOrEmpty(connectorIdString) && int.Parse(connectorIdString) == connectorId) {
                return piston;
            }
        }
        return null;
    }

    public void setPistonState(bool state) {
        if (this.block != null) {
            if (state == true) {
                this.block.Extend();
            } else {
                this.block.Retract();
            }
        }
    }

    public static void setAllPistonState(bool state) {
        foreach (Piston piston in Piston.pistons) {
            if (state == true) {
                piston.block.Extend();
            } else {
                piston.block.Retract();
            }
        }
    }

    public static void initPistons(MyGridProgram myGrid) {
        List<IMyPistonBase> blocks = new List<IMyPistonBase>();
        List<CustomData> customData;
        Piston tmpPiston;
        myGrid.GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(blocks);
        foreach (IMyPistonBase block in blocks) {
            if (block.CustomName.Contains("[Drone]")) {
                customData = CustomData.getCustomData(block.CustomData);
                if (customData.Count > 0) {
                    tmpPiston = new Piston(block, customData);
                    Piston.pistons.Add(tmpPiston);
                }
            }
        }
    }
}
