
public class Navigation
{
    public MyGridProgram myGrid;
    public List<IMyRemoteControl> remotes { get; set; }
    public Vector3D lastWaypoint;
    private long lastMovementCommand = 0;
    public List<DetectedEntity> nearbyEntities { get; set; }

    public Navigation(MyGridProgram myGrid) {
        this.myGrid = myGrid;
        this.nearbyEntities = new List<DetectedEntity>();
    }

    public void updateRemoteControls() {
        this.remotes = new List<IMyRemoteControl>();
        List<IMyRemoteControl> handles = new List<IMyRemoteControl>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(handles, c => c.BlockDefinition.ToString().ToLower().Contains("remote"));
        foreach (IMyRemoteControl handle in handles) {
            if (handle.CustomName.Contains("[Drone]")) {
                this.remotes.Add(handle);
            }
        }
    }

    public void setAutopilotStatus(bool autoPilotStatus) {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.SetAutoPilotEnabled(autoPilotStatus);
        }
    }

    public double getDistanceFrom(Vector3D pos, Vector3D pos2) {
        return Math.Round( Vector3D.Distance( pos, pos2 ), 2 );
    }

    public void move(Vector3D coords, string waypointName) {
        if (this.lastMovementCommand == 0 || (Communication.getTimestamp() - this.lastMovementCommand) > 30) {
            this.lastMovementCommand = Communication.getTimestamp();
            this.clearPath();
            this.lastWaypoint = coords;
            foreach (IMyRemoteControl remote in this.remotes) {
                remote.AddWaypoint(coords, waypointName);
                remote.SetAutoPilotEnabled(true);
            }
        }
    }

    public void updateNearbyCollisionData(IMySensorBlock sensor)
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
                        tmp.entityInfo = entity;
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

    public void clearPath() {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.ClearWaypoints();
        }
    }
}
