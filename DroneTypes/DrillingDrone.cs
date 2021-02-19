
public class DrillingDrone : NodeData
{
    public DrillingDrone(int id) : base(id) {}

    public void handleIdle() {
        NodeData targetFriend = this.findFriends();

        if (targetFriend.id > 0) {
            this.status = "running-to-storage";
            this.navHandle.move(targetFriend.getShipPosition(), "running-to-storage");
        } else {
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
    }

    public void execute() {
        if (this.usedInventorySpace < 95 || 1 == 1) { // @Lack of features
            DetectedEntity target = this.getTarget();

            if (target.id > 0) {

                // @BUG: Keeps moving out of sensor range...
                // Move to closest ore.
                Vector3D targetPos = target.position;
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
                this.status = "target-acquired-exact";
                this.startDrills();
            } else {
                this.haltDrills();
                this.handleIdle();
            }
        } else {
            this.status = "idle";
            this.haltDrills();
            this.navHandle.clearPath();
            // @TODO: add Find home base feature.
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
            targetDistance = this.navHandle.getDistanceFrom(this.getShipPosition(), entity.position);
            if (targetDistance < closestDistance) {
                closest = entity;
                closestDistance = targetDistance;
            }
        }
        return closest;
    }
}
