MyGridProgram myGrid;

public Program()
{
    myGrid = this;
    setupAntenna();
    Runtime.UpdateFrequency = UpdateFrequency.None;
}

public void Main(string argument, UpdateType updateSource) {
    if (argument == "recall") {
        broadcastMessage("drone-recall_user");
    } else if (argument == "release") {
        broadcastMessage("drone-release_user");
    }
}

public void broadcastMessage(string messageOut) {
    string tag1 = "drone-channel";
    myGrid.IGC.SendBroadcastMessage(tag1, messageOut);
}

public void setupAntenna() {
    string tag1 = "drone-channel";
    myGrid.IGC.RegisterBroadcastListener(tag1);
}

public long getTimestamp() {
    long epochTicks = new DateTime(1970, 1, 1).Ticks;
    return ((DateTime.UtcNow.Ticks - epochTicks) / TimeSpan.TicksPerSecond);
}
