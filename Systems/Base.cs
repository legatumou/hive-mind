bool runMainLoop = false;
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
    if (runMainLoop == true) {
        commHandle.handleListeners();
        commHandle.handleKeepalives();
        coreHandle.updateDroneData();
        commHandle.sendPing();
        commHandle.sendNodeData();
        commHandle.sendNearbyEntityList();
        coreHandle.execute();
        Display.display();
    }
}

public Program()
{
    nodeId = Core.generateRandomId();
    runMainLoop = true;
    Echo("Loading drone, ID: " + nodeId);
    Display.myGrid = this;
    Display.fetchOutputDevices();
    commHandle = new Communication(this);
    initCore(nodeId);
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    commHandle.setupAntenna();

    if (this.validation()) {
        Echo("Systems online");
        Display.print("Systems online.");
    } else {
        runMainLoop = false;
        Echo("Drone core missing.");
        Display.print("[Drone] Core is missing!");
    }
}

public void initCore(int nodeId) {
    coreHandle = new Core(this);
    Communication.currentNode = new Drone(nodeId);
    Communication.currentNode.setCoreHandle(coreHandle);
    Communication.currentNode.setCommHandle(commHandle);
    Communication.currentNode.initNavigation(this);
    Communication.currentNode.navHandle.updateRemoteControls();
    Communication.currentNode.navHandle.updateThrusters();
    coreHandle.handleCoreBlock();
    Anchor.initAnchors(this);
    Piston.initPistons(this);
    AnchoredConnector.initConnectors(this);
    Communication.currentNode.initiate();
}

public bool validation() {
    if (
        Core.coreBlock == null
    ) {
        return false;
    }
    return true;
}
