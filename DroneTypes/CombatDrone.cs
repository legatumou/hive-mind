
public class Drone : NodeData
{
    public Drone(int id) : base(id) {}

    public void initiate() {
        this.type = "combat";
    }

    public void handleIdle() {
        NodeData targetFriend = this.findFriends();
        Vector3D newPos = this.getIdlePosition();

        if (newPos.X == 0) {
            this.status = "moving-to-master";
            Random rand = new Random();
            newPos.X += rand.Next(50, 200); // Offset from ship
            newPos.Y += rand.Next(50, 200); // Offset from ship
            newPos.Z += rand.Next(50, 200); // Offset from ship
            this.navHandle.move(newPos, "running-to-friend");
        }
    }

    public Vector3D getIdlePosition() {
        if (Communication.masterDrone != null) {
            Vector3D targetPos = Communication.masterDrone.position;
            Random rand = new Random();
            targetPos.X += rand.Next(20, 100); // Offset from ship
            targetPos.Y += rand.Next(20, 100); // Offset from ship
            targetPos.Z += rand.Next(20, 100); // Offset from ship
            return targetPos;
        }
        return new Vector3D(0,0,0);
    }

    public void execute() {

        if (Communication.masterDrone == null) {
            this.status = "waiting-for-master";
            this.commHandle.sendMasterRequest();
            return;
        }
        this.status = "looking-for-targets";
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
            this.navHandle.move(targetPos, "target-position");
            this.status = "target-acquired";
        } else {
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
            targetDistance = this.navHandle.getDistanceFrom(this.navHandle.getShipPosition(), entity.position);
            if (targetDistance < closestDistance) {
                closest = entity;
                closestDistance = targetDistance;
            }
        }
        return closest;
    }
}
