
public class Drone : NodeData
{
    public Drone(int id) : base(id) {}

    public void initiate() {
        this.type = "combat";
    }

    public void handleIdle() {
        NodeData targetFriend = this.findFriends();
        Vector3D newPos = this.getIdlePosition();

        if (newPos.X != 0) {
            if (Communication.masterDrone != null && Communication.masterDrone.position.X != 0) {
                double distance = this.navHandle.getDistanceFrom(this.navHandle.getShipPosition(), Communication.masterDrone.position);
                if (distance > 500) {
                    this.status = "moving-to-master";
                    Random rand = new Random();
                    newPos.X += rand.Next(50, 200); // Offset from ship
                    newPos.Y += rand.Next(50, 200); // Offset from ship
                    newPos.Z += rand.Next(50, 200); // Offset from ship
                    this.navHandle.move(newPos, "running-to-friend");
                } else {
                    this.status = "waiting-for-enemies";
                }
            } else {
                this.status = "waiting-for-master";
            }
        }
    }

    public Vector3D getIdlePosition() {
        if (Communication.masterDrone != null) {
            Vector3D targetPos = Communication.masterDrone.position;
            Random rand = new Random();
            targetPos.X += rand.Next(-500, 500); // Offset from ship
            targetPos.Y += rand.Next(-500, 500); // Offset from ship
            targetPos.Z += rand.Next(-500, 500); // Offset from ship
            return targetPos;
        }
        return new Vector3D(0,0,0);
    }

    public void execute() {

        if (Communication.masterDrone == null) {
            this.status = "waiting-for-master";
            this.commHandle.sendMasterRequest();
        }

        if (this.battery < 5) {
            return; // Solar cells need to recharge.
        }
        this.status = "looking-for-targets";
        DetectedEntity target = this.getTarget();

        if (target.id > 0) {
            // Move to closest ore.
            Vector3D targetPos = target.position;

            // Add some random movement.
            Random rnd = new Random();
            targetPos.X += (int) rnd.Next(-100, 100);
            targetPos.Y += (int) rnd.Next(-100, 100);
            targetPos.Z += (int) rnd.Next(-100, 100);

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
        string[] targetList = {"Small Grid", "Large Grid", "Character Human", "Character Other"};
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
