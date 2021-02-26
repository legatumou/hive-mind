
public class Display
{
    public static List<IMyTextPanel> LCD = new List<IMyTextPanel>();
    public static List<IMyTextPanel> TextPanels = new List<IMyTextPanel>();
    public static List<IMyCockpit> Cockpits = new List<IMyCockpit>();
    public static MyGridProgram myGrid;
    public static List<string> printQueue = new List<string>();
    public static List<string> debugPrintQueue = new List<string>();
    public static List<string> dockingPrintQueue = new List<string>();
    public static long lastDisplayRefresh = 0;

    public static bool debug = true; // @TODO: Should be some kind of config or some shit.

    public static void fetchOutputDevices() {
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(Display.TextPanels, c => c.BlockDefinition.ToString().ToLower().Contains("text"));
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Display.Cockpits, c => c.BlockDefinition.ToString().ToLower().Contains("cockpit"));
    }

    public static void print(string extraMsg) {
        Display.printQueue.Add(extraMsg);
    }

    public static void printDebug(string extraMsg) {
        Display.debugPrintQueue.Add(extraMsg);
    }

    public static void display() {
        if (Display.lastDisplayRefresh == 0 || Communication.getTimestamp() - Display.lastDisplayRefresh >= 3) {
            Display.lastDisplayRefresh = Communication.getTimestamp();

            // Main info panel
            string msg = Display.generateMessage(string.Join("\n", Display.printQueue));
            // TextPanels
            foreach (IMyTextPanel panel in Display.TextPanels) {
                if (panel.CustomName.Contains("[Drone]") && !panel.CustomName.Contains("[Debug]") && !panel.CustomName.Contains("[Docking]")) {
                    panel.WriteText(msg, false);
                }
            }

            // Docking panel
            string dockingMsg = Display.generateDockingMessage(string.Join("\n", Display.dockingPrintQueue));
            // TextPanels
            foreach (IMyTextPanel panel in Display.TextPanels) {
                if (panel.CustomName.Contains("[Drone]") && panel.CustomName.Contains("[Docking]")) {
                    panel.WriteText(dockingMsg, false);
                }
            }

            // Debug data.
            string debugMsg = Display.generateDebugMessage(string.Join("\n", Display.debugPrintQueue));
            // TextPanels
            foreach (IMyTextPanel panel in Display.TextPanels) {
                if (panel.CustomName.Contains("[Drone]") && panel.CustomName.Contains("[Debug]")) {
                    panel.WriteText(debugMsg, false);
                }
            }
            // Merge it?
            if (Display.debug == true) {
                msg += "\n" + debugMsg;
            }

            Display.myGrid.Echo(msg);
            Display.printQueue = new List<string>();
            Display.debugPrintQueue = new List<string>();
        }
    }

    public static string generateDockingMessage(string msg) {
        string message = "";
        message += "=== Docking info ===\n";
        message += "Anchored connectors: " + AnchoredConnector.anchoredConnectors.Count + "\n";
        foreach (AnchoredConnector connector in AnchoredConnector.anchoredConnectors) {
            message += "--> Connector \t";
            if (connector.connectorId != null) {
                message += "-> ID: " + connector.connectorId + "\t";
            }
            if (connector.inUse == true) {
                message += "-> Docking in progress\t";
            }
            message += "-> Connector isAnchored: " + connector.isAnchored + "\n";
        }
        message += "------------------\n";
        message += msg + "\n";

        return message;
    }

    public static string generateDebugMessage(string msg) {
        string message = "";
        message += "=== DEBUG DATA ===\n";
        message += "------------------\n";
        message += msg + "\n";

        return message;
    }

    public static string generateMessage(string msg) {
        string message = "";
        Drone myDrone = Communication.currentNode;
        List<IMyBatteryBlock> vBatteries = new List<IMyBatteryBlock>();
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(vBatteries, c => c.BlockDefinition.ToString().ToLower().Contains("battery"));
        message += "=== Drone Overview (ID: " + myDrone.id + " " + myDrone.type + ") ===\n";
        message += "Battery: " + Math.Round(myDrone.battery) + "% (" + vBatteries.Count + " batteries found)\n";
        message += "Speed: " + Math.Round((myDrone.speed / 100), 3) + " | ";
        if (myDrone.type == "replicator") {
            message += "Slaves: " + Communication.slaves.Count + " | \t";
        } else {
            message += "Space used: " + myDrone.usedInventorySpace + "%\n";
            if (Communication.masterDrone != null) {
                message += "MasterID: " + Communication.masterDrone.id + "\t";
                if (Communication.masterDrone.masterConnectorId != null) {
                    message += ", ConnectorID: " + Communication.masterDrone.masterConnectorId + "\n";
                }
            }
        }
        message += "Status: " + myDrone.status  + "\n";
        if (myDrone.navHandle.nearbyEntities != null && myDrone.navHandle.nearbyEntities.Count() > 0) {
            message += " ==> Nearby entities (" + myDrone.navHandle.nearbyEntities.Count() + " found)\n";
            for (int i = 0; i < myDrone.navHandle.nearbyEntities.Count; i++) {
                if (i > 10) break;
                message += " => " + myDrone.navHandle.nearbyEntities[i].name + " (Distance: " + myDrone.navHandle.nearbyEntities[i].distance + ")" + "\n";
            }
        }
        message += "Active docking procedures: " + Docking.activeDockingProcedures.Count + "\n";
        message += msg + "\n";
        message += "=== Drones connected (" + Communication.connectedNodes.Count + ") ===\n";
        double distance;
        for (int i = 0; i < Communication.connectedNodesData.Count; i++) {
            distance = Communication.currentNode.navHandle.getDistanceFrom(Communication.connectedNodesData[i].position, myDrone.navHandle.getShipPosition());
            message += " ==> Drone ID: " + Communication.connectedNodesData[i].id + "\n";
            message += " Battery: " + Math.Round(Communication.connectedNodesData[i].battery) + "%" + "\t";
            message += " | Type: " + Communication.connectedNodesData[i].type + "\t";
            message += " | Status: " + Communication.connectedNodesData[i].status + "\n";
            message += " | Storage: " + Communication.connectedNodesData[i].usedInventorySpace + "% \t";
            message += " | Distance: " + distance + "m\n";
            if (Communication.connectedNodesData[i].navHandle.nearbyEntities != null && Communication.connectedNodesData[i].navHandle.nearbyEntities.Count > 0) {
                message += " => Nearby entities (" + Communication.connectedNodesData[i].navHandle.nearbyEntities.Count + " found)\n";
                for (int n = 0; n < myDrone.navHandle.nearbyEntities.Count; n++) {
                    if (n > 5) break;
                    message += " => " + Communication.connectedNodesData[i].navHandle.nearbyEntities[n].name + " (Distance: " + Communication.connectedNodesData[i].navHandle.nearbyEntities[n].distance + ")" + "\n";
                }
            }
        }

        return message;
    }
}
