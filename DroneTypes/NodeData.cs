public class NodeData
{
    public NodeData(int id)
    {
        this.id = id;
        this.battery = 0;
        this.speed = 0.0;
        this.type = "N/A";
        this.status = "init";
        this.keepalive = Communication.getTimestamp();
        this.nearbyEntities = new List<DetectedEntity>();
    }
    public MyGridProgram myGrid;

    public int id { get; set; }
    public long keepalive { get; set; }
    public float battery { get; set; }
    public double speed { get; set; }
    public string type { get; set; }
    public string status { get; set; }
    public List<DetectedEntity> nearbyEntities { get; set; }
    public Navigation navHandle;

    public void setNavigation(Navigation navHandle) {
        this.navHandle = navHandle;
    }

    public double getDistanceFrom(Vector3D pos, Vector3D pos2) {
        return Math.Round( Vector3D.Distance( pos, pos2 ), 2 );
    }

    public Vector3D getShipPosition() {
        List<IMySensorBlock> sensors = new List<IMySensorBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, c => c.BlockDefinition.ToString().ToLower().Contains("sensor"));
        foreach (IMySensorBlock sensor in sensors) {
            if (sensor.CustomName.Contains("[Drone]")) {
                return sensor.GetPosition();
            }
        }
        return new Vector3D();
    }

    public void updateNearbyCollision(IMySensorBlock sensor)
    {
        if (!sensor.LastDetectedEntity.IsEmpty()) {
            MyDetectedEntityInfo entity = sensor.LastDetectedEntity;
            if (!this.nearbyEntities.Any(val => val.id == entity.EntityId)) {
                DetectedEntity tmp = new DetectedEntity();
                tmp.id = entity.EntityId;
                tmp.name = entity.Name;
                tmp.position = entity.Position;
                tmp.distance = this.getDistanceFrom(entity.Position, sensor.GetPosition());
                tmp.type = entity.Type;
                this.nearbyEntities.Add(tmp);
            } else {
                for (int i = 0; i < this.nearbyEntities.Count; i++) {
                    DetectedEntity nearEntity = this.nearbyEntities[i];
                    if (nearEntity.id == entity.EntityId) {
                        DetectedEntity tmp = new DetectedEntity();
                        tmp.id = entity.EntityId;
                        tmp.name = entity.Name;
                        tmp.position = entity.Position;
                        tmp.distance = this.getDistanceFrom(entity.Position, sensor.GetPosition());
                        tmp.type = entity.Type;
                        this.nearbyEntities[i] = tmp;
                        break;
                    }
                }
            }
        } else {
            this.nearbyEntities = new List<DetectedEntity>();
        }
    }

    public Vector3D getTarget() {
        return new Vector3D();
    }

    public void execute(){}

    public void process(MyGridProgram grid)
    {
        this.myGrid = grid;
        List<IMySensorBlock> sensors = new List<IMySensorBlock>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, c => c.BlockDefinition.ToString().ToLower().Contains("sensor"));
        foreach (IMySensorBlock sensor in sensors) {
            if (sensor.CustomName.Contains("[Drone]")) {
                this.updateNearbyCollision(sensor);
            }
        }
    }

    public bool findFriends() {
        // Find a friendly drone.
        double closestDistance = 100000.0;
        double distance;
        NodeData closest = new NodeData(0);

        for (int i = 0; i < Communication.connectedNodesData.Count; i++) {
            NodeData node = Communication.connectedNodesData[i];
            distance = this.getDistanceFrom(node.getShipPosition(), Communication.coreBlock.GetPosition());
            if (distance < closestDistance && distance > 50) { // not too close ;)
                closestDistance = distance;
                closest = node;
            }
        }
        if (closest.id > 0) {
            this.status = "running-to-friend";
            this.navHandle.move(closest.getShipPosition(), "running-to-friend");
            return true;
        }
        return false;
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
        foreach (IMyShipDrill drill in drills) {
            drill.Enabled = false;
        }
    }
}
