
int nodeId;
IMyTextPanel LCDPanel;
Communication commHandle;
Core coreHandle;

public void Save()
{
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means.
    //
    // This method is optional and can be removed if not
    // needed.
}

public void Main()
{
    Display.print("");
    commHandle.handleListeners();
    commHandle.handleKeepalives();
    coreHandle.updateDroneData();
    commHandle.sendPing();
    commHandle.sendNodeData();
    coreHandle.execute();
}

public Program()
{
    // @TODO, Validations to make sure all blocks are connected.
    nodeId = generateRandomId();
    Display.myGrid = this;
    Display.fetchOutputDevices();
    commHandle = new Communication(this);
    coreHandle = new Core(this);
    Communication.currentNode = new DrillingDrone(nodeId);
    Communication.currentNode.type = "mining";
    Communication.currentNode.initNavigation(this);
    Communication.coreBlock = (IMyProgrammableBlock) GridTerminalSystem.GetBlockWithName("[Drone] Core");
    LCDPanel = GridTerminalSystem.GetBlockWithName("[Drone] LCD") as IMyTextPanel;
    if (LCDPanel != null) {
        LCDPanel.CustomData = "" + nodeId;
    }
    Echo("Loading drone, ID: " + nodeId);
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    commHandle.setupAntenna();
    setCustomData("drone-id-" + nodeId);
}

public void setCustomData(string data)
{
    if (Communication.coreBlock != null) {
        Communication.coreBlock.CustomData = data;
    }
}

public int generateRandomId()
{
    Random rnd = new Random();
    return rnd.Next(0, 10000);
}
