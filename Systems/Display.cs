
public class Display
{
    public static List<IMyTextPanel> LCD = new List<IMyTextPanel>();
    public static List<IMyTextPanel> TextPanels = new List<IMyTextPanel>();
    public static List<IMyCockpit> Cockpits = new List<IMyCockpit>();
    public static MyGridProgram myGrid;

    public static void fetchOutputDevices()
    {
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(Display.TextPanels, c => c.BlockDefinition.ToString().ToLower().Contains("text"));
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyCockpit>(Display.Cockpits, c => c.BlockDefinition.ToString().ToLower().Contains("cockpit"));
    }

    public static void print(string extraMsg)
    {
        string msg = Display.generateMessage(extraMsg);

        // TextPanels
        foreach (IMyTextPanel panel in Display.TextPanels) {
            if (panel.CustomName.Contains("[Drone]")) {
                panel.WriteText(msg, false);
            }
        }

        Display.myGrid.Echo(msg);
    }

    public static string generateMessage(string msg)
    {
        string message = "";
        List<IMyBatteryBlock> vBatteries = new List<IMyBatteryBlock>();
        Display.myGrid.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(vBatteries, c => c.BlockDefinition.ToString().ToLower().Contains("battery"));
        message += "=== Drone Overview (ID: " + Communication.currentNode.id + ") ===\n";
        message += "Battery: " + Math.Round(Communication.currentNode.battery) + "% (" + vBatteries.Count + " batteries found)\n";
        message += "Speed: " + Math.Round((Communication.currentNode.speed / 100), 3) + " | ";
        message += "Space used: " + Communication.currentNode.usedInventorySpace + "%\n";
        message += "Status: " + Communication.currentNode.status + ", Connected: " + Communication.connectedNodes.Count + "\n";
        if (Communication.currentNode.navHandle.nearbyEntities != null && Communication.currentNode.navHandle.nearbyEntities.Count() > 0) {
            message += " ==> Nearby entities (" + Communication.currentNode.navHandle.nearbyEntities.Count() + " found)\n";
            for (int i = 0; i < Communication.currentNode.navHandle.nearbyEntities.Count; i++) {
                if (i > 10) break;
                message += " => " + Communication.currentNode.navHandle.nearbyEntities[i].name + " (Distance: " + Communication.currentNode.navHandle.nearbyEntities[i].distance + ")" + "\n";
            }
        }
        message += msg + "\n";
        message += "=== Drones connected ===\n";

        for (int i = 0; i < Communication.connectedNodesData.Count; i++) {
            message += " ==> Drone ID: " + Communication.connectedNodesData[i].id + "\n";
            message += " => Battery" + Math.Round(Communication.connectedNodesData[i].battery) + "%" + "\t";
            message += " => Type: " + Communication.connectedNodesData[i].type + "\t";
            message += " => Status: " + Communication.connectedNodesData[i].status + "\n";
            if (Communication.connectedNodesData[i].navHandle.nearbyEntities != null && Communication.connectedNodesData[i].navHandle.nearbyEntities.Count > 0) {
                message += " => Nearby entities (" + Communication.connectedNodesData[i].navHandle.nearbyEntities.Count + " found)\n";
                for (int n = 0; n < Communication.currentNode.navHandle.nearbyEntities.Count; n++) {
                    if (n > 5) break;
                    message += " => " + Communication.connectedNodesData[i].navHandle.nearbyEntities[n].name + " (Distance: " + Communication.connectedNodesData[i].navHandle.nearbyEntities[n].distance + ")" + "\n";
                }
            }
        }

        return message;
    }
}
