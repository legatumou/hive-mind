
public class AnchoredConnector
{
    public MyGridProgram myGrid;
    public IMyShipConnector block;
    public Piston piston;
    public List<CustomData> customData;

    public Anchor anchorTop;
    public Anchor anchorBottom;

    public int connectorId;
    public bool inUse = false;
    public bool isAnchored = false;

    public static List<AnchoredConnector> anchoredConnectors = new List<AnchoredConnector>();

    public AnchoredConnector(IMyShipConnector block, List<CustomData> customData) {
        this.block = block;
        this.customData = customData;
    }

    public Vector3D getPosition(double distance) {
        if (this.anchorBottom == null || this.anchorBottom == null) {
            return new Vector3D(0,0,0);
        }
        Vector3D anchorBottomPosition = this.anchorBottom.block.GetPosition();
        Vector3D anchorTopPosition = this.anchorTop.block.GetPosition();
        Vector3D result = new Vector3D(0, 0, 0);
        result.X = anchorBottomPosition.X + ((anchorBottomPosition.X - anchorTopPosition.X) * (distance/39));
        result.Y = anchorBottomPosition.Y + ((anchorBottomPosition.Y - anchorTopPosition.Y) * (distance/39));
        result.Z = anchorBottomPosition.Z + ((anchorBottomPosition.Z - anchorTopPosition.Z) * (distance/39));
        return result;
    }

    public void assignAnchorsToConnector() {
        string connectorId = CustomData.findKeyFromList("connectorId", this.customData).value;
        if (!String.IsNullOrEmpty(connectorId)) {
            this.connectorId = int.Parse(connectorId);
            this.anchorTop = Anchor.getAnchorByConnector(this.connectorId, "top");
            this.anchorBottom = Anchor.getAnchorByConnector(this.connectorId, "bottom");
            this.piston = Piston.getPistonByConnector(this.connectorId);
            if (this.anchorTop != null && this.anchorBottom != null) {
                this.isAnchored = true;
            }
        } else {
            Display.printDebug("[WARN] Connector without a defined ID. Name: " + this.block.CustomName);
            this.isAnchored = false;
        }
    }

    public static void setAllConnectorState(bool state) {
        foreach (AnchoredConnector connector in AnchoredConnector.anchoredConnectors) {
            connector.inUse = state;
            connector.block.Enabled = state;
        }
    }

    public void anchorConnector(Anchor anchorTop, Anchor anchorBottom) {
        this.anchorTop = anchorTop;
        this.anchorBottom = anchorBottom;
    }

    public static AnchoredConnector getAvailableConnector() {
        foreach (AnchoredConnector connector in AnchoredConnector.anchoredConnectors) {
            if (Core.isLocal(connector.block) && connector.inUse != true) {
                return connector;
            }
        }
        // @TODO: Refactor this to also account for the queue size of the connector.
        /*foreach (AnchoredConnector connector in AnchoredConnector.anchoredConnectors) {
            return connector;
        }*/
        return null;
    }

    public static AnchoredConnector getAvailableAnchoredConnector() {
        Random rand = new Random();
        List<AnchoredConnector> randomConnectors = AnchoredConnector.anchoredConnectors.OrderBy (x => rand.Next()).ToList();
        foreach (AnchoredConnector connector in randomConnectors) {
            if (/**Core.isLocal(connector.block) && */connector.inUse != true && connector.isAnchored == true) {
                return connector;
            }
        }
        // @TODO: Refactor this to also account for the queue size of the connector.
        /*foreach (AnchoredConnector connector in randomConnectors) {
            if (connector.isAnchored == true) {
                return connector;
            }
        }*/
        return null;
    }

    public static void setConnectorState(int id, bool state) {
        foreach (AnchoredConnector connector in AnchoredConnector.anchoredConnectors) {
            if (connector.connectorId == id) {
                connector.inUse = state;
                connector.block.Enabled = state;
                break;
            }
        }
    }

    public static void initConnectors(MyGridProgram myGrid) {
        List<IMyShipConnector> blocks = new List<IMyShipConnector>();
        List<CustomData> customData;
        AnchoredConnector tmpAnchoredConnector;
        myGrid.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks);
        foreach (IMyShipConnector block in blocks) {
            if (block.CustomName.Contains("[Drone]")) {
                customData = CustomData.getCustomData(block.CustomData);
                if (customData.Count > 0) {
                    tmpAnchoredConnector = new AnchoredConnector(block, customData);
                    tmpAnchoredConnector.assignAnchorsToConnector();
                    if (tmpAnchoredConnector.connectorId != null) {
                        AnchoredConnector.anchoredConnectors.Add(tmpAnchoredConnector);
                    }
                }
            }
        }
    }
}
