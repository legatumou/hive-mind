
public class Drone : NodeData
{
    long lastLoopTime = 0;

    public Drone(int id) : base(id) {}

    public void initiate() {
        this.type = "mining";
    }

    public void execute() {
        if (this.lastLoopTime == 0 || Communication.getTimestamp() - this.lastLoopTime > 3) { // 3 Sec thinking intervals
            bool result;
            result = this.hasMaster();
            if (result != true) return;
            this.dockingHandle.handleDockingProcedure();
            this.mainLogic();
            this.lastLoopTime = Communication.getTimestamp();

            this.navHandle.setDirection("Forward"); // @TODO: Should make a method that handles all the hardware in one place.
        }
        this.dockingHandle.handleDockingMechanism();
    }

    public void handleIdle() {
        double targetDistance = this.navHandle.getDistanceFrom(this.navHandle.getShipPosition(), Communication.masterDrone.position);
        if (Communication.masterDrone.position.X != 0 && this.navHandle.getShipPosition().X != 0) {
            if (targetDistance > 25000) { // Over 25km
                Display.print("Too far, returning to master.");
                this.navHandle.returnToMaster();
                return;
            }
        } else {
            Display.print("Missing master info, waiting.");
            return;
        }
        // Find ore
        this.status = "finding-ore";
        Vector3D newPos = Core.coreBlock.GetPosition();
        this.startDrills();
        // Random position
        Random rnd = new Random();
        newPos.X += (int) rnd.Next(-10000, 10000);
        newPos.Y += (int) rnd.Next(-10000, 10000);
        newPos.Z += (int) rnd.Next(-10000, 10000);
        this.navHandle.move(newPos, "cruising");
    }

    public void mainLogic() {
        if (this.usedInventorySpace < 95 && this.navHandle.activeDockingProcedure == null && this.playerCommand != "recall") {
            this.navHandle.setCollisionStatus(false);
            this.navHandle.thrusterStatus(true);
            DetectedEntity target = this.getTarget();

            if (target.id > 0) {

                double targetDistance = this.navHandle.getDistanceFrom(this.navHandle.getShipPosition(), target.position);
                Vector3D targetPos = target.position;
                this.status = "target-acquired-exact";
                if (target.entityInfo.BoundingBox.Min.X != 0 && target.entityInfo.BoundingBox.Min.Y != 0 && target.entityInfo.BoundingBox.Min.Z != 0) {
                    this.status = "target-acquired-box";
                    // Add some random movement.
                    Random rnd = new Random();
                    targetPos.X = (double) rnd.Next((int) target.entityInfo.BoundingBox.Min.X, (int) target.entityInfo.BoundingBox.Max.X);
                    targetPos.Y = (double) rnd.Next((int) target.entityInfo.BoundingBox.Min.Y, (int) target.entityInfo.BoundingBox.Max.Y);
                    targetPos.Z = (double) rnd.Next((int) target.entityInfo.BoundingBox.Min.Z, (int) target.entityInfo.BoundingBox.Max.Z);
                }
                if (targetPos.X == 0 || targetPos.Y == 0 || targetPos.Z == 0) {
                    Display.printDebug("Unable to find target GPS.");
                } else {
                    Display.printDebug("Setting new drilling destination. (" + Math.Round(targetPos.X, 2) + ", " + Math.Round(targetPos.Y, 2) + ", " + Math.Round(targetPos.Z, 2) + ")");
                    this.navHandle.move(targetPos, "navigate-to-ore");
                    if (targetDistance > 500) {
                        this.haltDrills();
                        this.navHandle.setCollisionStatus(true);
                    } else {
                        this.navHandle.setCollisionStatus(false);
                        this.startDrills();
                    }
                }

                this.navHandle.overrideThruster("Forward", 10);
            } else {
                Display.printDebug("No target found.");
                this.navHandle.overrideThruster("Forward", 10);
                this.haltDrills();
                this.handleIdle();
            }
        } else {
            if (this.navHandle.activeDockingProcedure != null && this.navHandle.activeDockingProcedure.dockingStep < 2) {
                Display.printDebug("Returning to master.");
                this.haltDrills();
                this.navHandle.returnToMaster();
            } else {
                this.startDrills();
                Display.printDebug("Returning to master.");
                this.navHandle.returnToMaster();
            }
        }
    }

    public void startDrills() {
        List<IMyShipDrill> drills = new List<IMyShipDrill>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills);
        foreach (IMyShipDrill drill in drills) {
            drill.Enabled = true;
        }
    }

    public void haltDrills() {
        List<IMyShipDrill> drills = new List<IMyShipDrill>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills);
        this.gyroHandle.disableOverride();
        foreach (IMyShipDrill drill in drills) {
            drill.Enabled = false;
        }
    }

    public DetectedEntity getTarget()
    {
        DetectedEntity closest = new DetectedEntity();
        string[] targetList = {"Asteroid"};
        double closestDistance = 999999;
        double targetDistance;
        foreach (DetectedEntity entity in this.navHandle.nearbyEntities) {
            // Filter out non asteroids.
            if (!targetList.Any(entity.name.Contains)) continue;
            targetDistance = this.navHandle.getDistanceFrom(this.navHandle.getShipPosition(), entity.position);
            if (targetDistance < closestDistance) {
                closest = entity;
                closestDistance = targetDistance;
            }
        }

        if (closestDistance > 25000) { // Not too far
            return new DetectedEntity();
        }
        return closest;
    }
}
