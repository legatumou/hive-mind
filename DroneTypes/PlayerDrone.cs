
// Generally to be used only by players. This is just an manual target scouting drone.
// In case you don't want to wait for a drone to find an asteroid itself.
public class Drone : NodeData
{
    public Drone(int id) : base(id) {}

    public void initiate() {
        this.type = "player";
    }

    public void execute() {
        if (Communication.masterDrone == null) {
            this.status = "waiting-for-master";
            this.commHandle.sendMasterRequest();
        } else {
            this.status = "master-obtained";
        }
    }
}
