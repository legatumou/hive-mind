
public class Drone : NodeData
{
    public Drone(int id) : base(id) {}

    public void handleIdle() {
        NodeData targetFriend = this.findFriends();

        if (targetFriend.id > 0) {
            this.status = "running-to-friend";
            this.navHandle.move(targetFriend.getShipPosition(), "running-to-friend");
        } else {
            // Find allies
            this.status = "finding-friends";
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
        DetectedEntity target = this.getTarget();

        if (target.id > 0) {
            // Move to closest ore.
            Vector3D targetPos = target.position;

            // Add some random movement.
            Random rnd = new Random();
            targetPos.X += (int) rnd.Next(-10, 10);
            targetPos.Y += (int) rnd.Next(-10, 10);
            targetPos.Z += (int) rnd.Next(-10, 10);

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
        string[] targetList = {"SmallGrid", "LargeGrid", "CharacterHuman", "CharacterOther"};
        double closestDistance = 3000;
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
