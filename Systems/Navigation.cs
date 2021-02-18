
public class Navigation
{
    public MyGridProgram myGrid;
    public List<IMyRemoteControl> remotes { get; set; }
    public Vector3D lastWaypoint;
    private long lastMovementCommand = 0;

    public Navigation(MyGridProgram myGrid) {
        this.myGrid = myGrid;
    }

    public void updateRemoteControls() {
        this.remotes = new List<IMyRemoteControl>();
        List<IMyRemoteControl> handles = new List<IMyRemoteControl>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(handles, c => c.BlockDefinition.ToString().ToLower().Contains("remote"));
        foreach (IMyRemoteControl handle in handles) {
            if (handle.CustomName.Contains("[Drone]")) {
                this.remotes.Add(handle);
            }
        }
    }

    public void setAutopilotStatus(bool autoPilotStatus) {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.SetAutoPilotEnabled(autoPilotStatus);
        }
    }

    public void move(Vector3D coords, string waypointName) {
        if (this.lastMovementCommand == 0 || (Communication.getTimestamp() - this.lastMovementCommand) > 30) {
            this.lastMovementCommand = Communication.getTimestamp();
            this.clearPath();
            this.lastWaypoint = coords;
            foreach (IMyRemoteControl remote in this.remotes) {
                remote.AddWaypoint(coords, waypointName);
                remote.SetAutoPilotEnabled(true);
            }
        }
    }

    public void clearPath() {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.ClearWaypoints();
        }
    }
}
