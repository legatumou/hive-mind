
public class ReplicatorDrone : NodeData
{
    public ReplicatorDrone(int id) : base(id) {}

    public void handleIdle() {
        bool foundFriend = this.findFriends();

        if (foundFriend == false) {
            // Find ore
            this.status = "finding-asteroids";
            Vector3D newPos = Communication.coreBlock.GetPosition();
            // Random position
            Random rnd = new Random();
            newPos.X += (int) rnd.Next(-10000, 10000);
            newPos.Y += (int) rnd.Next(-10000, 10000);
            newPos.Z += (int) rnd.Next(-10000, 10000);
            this.navHandle.move(newPos, "cruising");
        }
    }

    public void execute() {
        DetectedEntity target = this.getTarget();

        if (target.id > 0) {
            // Move to closest ore.
            Vector3D targetPos = target.position;

            // Add some random movement.
            Random rnd = new Random();
            targetPos.X += (int) rnd.Next(-1000, 1000);
            targetPos.Y += (int) rnd.Next(-1000, 1000);
            targetPos.Z += (int) rnd.Next(-1000, 1000);

            // Execute movement
            this.navHandle.move(targetPos, "navigate-to-ore");
            this.status = "target-acquired";
            this.startDrills();
        } else {
            this.haltDrills();
            this.handleIdle();
        }
    }

    public DetectedEntity getTarget()
    {
        DetectedEntity closest = new DetectedEntity();
        double closestDistance = 3000;
        double targetDistance;
        this.myGrid.Echo("Nearby entities: " + this.nearbyEntities.Count + "\n");
        foreach (DetectedEntity entity in this.nearbyEntities) {
            // Filter out non asteroids.
            if (entity.name != "Asteroid") continue;
            targetDistance = this.getDistanceFrom(this.getShipPosition(), entity.position);
            if (targetDistance < closestDistance) {
                closest = entity;
                closestDistance = targetDistance;
            }
        }
        return closest;
    }
}
