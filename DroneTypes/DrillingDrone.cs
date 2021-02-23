
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
            result = this.handleMaster();
            if (result == true) return;
            this.dockingHandle.handleDockingProcedure();
            this.mainLogic();
            this.lastLoopTime = Communication.getTimestamp();

            this.navHandle.setDirection("Forward"); // @TODO: Should make a method that handles all the hardware in one place.
        }

        this.dockingHandle.handleLockingMechanism();
    }

    public bool handleMaster() {
        if (Communication.masterDrone == null) {
            this.status = "requesting-master";
            this.commHandle.sendMasterRequest();
            this.navHandle.clearPath();
            return true;
        } else {
            if (this.status == "requesting-master") {
                this.status = "master-accepted";
            }
        }
        return false;
    }

    public void handleIdle() {
        double targetDistance = this.navHandle.getDistanceFrom(this.navHandle.getShipPosition(), Communication.masterDrone.position);
        if (targetDistance > 25000) { // Over 25km
            this.navHandle.returnToMaster();
            return;
        }
        // Find ore
        this.status = "finding-ore";
        Vector3D newPos = Core.coreBlock.GetPosition();
        // Random position
        Random rnd = new Random();
        newPos.X += (int) rnd.Next(-10000, 10000);
        newPos.Y += (int) rnd.Next(-10000, 10000);
        newPos.Z += (int) rnd.Next(-10000, 10000);
        this.navHandle.move(newPos, "cruising");
    }

    public void mainLogic() {
        if (this.usedInventorySpace < 95 && this.dockingHandle.dockingInProgress == false) {
            this.navHandle.setCollisionStatus(false);
            DetectedEntity target = this.getTarget();

            if (target.id > 0) {

                double targetDistance = this.navHandle.getDistanceFrom(this.navHandle.getShipPosition(), target.position);
                // @BUG: Keeps moving out of sensor range...
                // Move to closest ore.
                Vector3D targetPos = target.position;
                this.status = "target-acquired-exact";
                if ((double) target.entityInfo.BoundingBox.Min.X != 0) {
                    this.status = "target-acquired-box";
                    // Add some random movement.
                    Random rnd = new Random();
                    targetPos.X = (double) rnd.Next((int) target.entityInfo.BoundingBox.Min.X, (int) target.entityInfo.BoundingBox.Max.X);
                    targetPos.Y = (double) rnd.Next((int) target.entityInfo.BoundingBox.Min.Y, (int) target.entityInfo.BoundingBox.Max.Y);
                    targetPos.Z = (double) rnd.Next((int) target.entityInfo.BoundingBox.Min.Z, (int) target.entityInfo.BoundingBox.Max.Z);
                }
                // Execute movement
                this.navHandle.move(targetPos, "navigate-to-ore");
                if (targetDistance > 100) {
                    this.haltDrills();
                    this.navHandle.setCollisionStatus(true);
                } else {
                    this.navHandle.setCollisionStatus(false);
                    this.startDrills();
                }

                // Add some passive pressure.
                this.navHandle.overrideThruster("Forward", 10);
            } else {
                // Add some passive pressure.
                this.navHandle.overrideThruster("Forward", 10);
                this.haltDrills();
                this.handleIdle();
            }
        } else {
            if (this.dockingHandle.dockingStep > 1) {
                this.startDrills();
            } else {
                this.haltDrills();
            }
            this.navHandle.returnToMaster();
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
        return closest;
    }
}
