
public class Anchor
{
    public MyGridProgram myGrid;
    public IMySensorBlock block;
    public List<CustomData> customData;

    public static List<Anchor> anchors = new List<Anchor>();

    public Anchor(IMySensorBlock block, List<CustomData> customData) {
        this.block = block;
        this.customData = customData;
    }

    public Vector3D getAnchorPosition() {
        return this.block.GetPosition();
    }

    public static Anchor getAnchorByConnector(int connectorId, string type) {
        string connectorIdString;
        string connectorType;
        foreach (Anchor anchor in Anchor.anchors) {
            connectorIdString = CustomData.findKeyFromList("connectorId", anchor.customData).value;
            connectorType = CustomData.findKeyFromList("connectorType", anchor.customData).value;
            if (!String.IsNullOrEmpty(connectorIdString) && int.Parse(connectorIdString) == connectorId && type == connectorType) {
                return anchor;
            }
        }
        return null;
    }

    public static void initAnchors(MyGridProgram myGrid) {
        List<IMySensorBlock> blocks = new List<IMySensorBlock>();
        List<CustomData> customData;
        Anchor tmpAnchor;
        myGrid.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(blocks);
        foreach (IMySensorBlock block in blocks) {
            if (block.CustomName.Contains("[Drone]") && block.CustomName.Contains("[Anchor]")) {
                customData = CustomData.getCustomData(block.CustomData);
                if (customData.Count > 0) {
                    tmpAnchor = new Anchor(block, customData);
                    Anchor.anchors.Add(tmpAnchor);
                }
            }
        }
    }
}
