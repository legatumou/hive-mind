
public class Drone : NodeData
{
    long lastLoopTime = 0;

    public Drone(int id) : base(id) {}

    public void initiate() {
        Communication.currentNode.dockingHandle.setPistonState(false);
        this.type = "projector";
    }

    public void execute() {
        if (this.lastLoopTime == 0 || Communication.getTimestamp() - this.lastLoopTime > 3) {

            this.dockingHandle.handleDockingProcedure();
            this.lastLoopTime = Communication.getTimestamp();
        }
    }
}
