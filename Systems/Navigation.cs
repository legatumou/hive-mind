
public class Navigation
{
    public MyGridProgram myGrid;
    public List<IMyRemoteControl> remotes { get; set; }
    public List<IMyThrust> thrusters { get; set; }
    public Vector3D lastWaypoint;
    private long lastMovementCommand = 0;
    public List<DetectedEntity> nearbyEntities { get; set; }
    public Docking dockingHandle;
    public Communication commHandle;
    public Gyro gyroHandle;


    public Navigation(MyGridProgram myGrid) {
        this.myGrid = myGrid;
        this.nearbyEntities = new List<DetectedEntity>();
    }

    public void setDockingHandle(Docking dockingHandle) {
        this.dockingHandle = dockingHandle;
    }

    public void setGyroHandle(Gyro gyroHandle) {
        this.gyroHandle = gyroHandle;
    }

    public void setCommunicationHandle(Communication commHandle) {
        this.commHandle = commHandle;
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

    public void updateThrusters() {
        this.thrusters = new List<IMyThrust>();
        List<IMyThrust> handles = new List<IMyThrust>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyThrust>(handles, c => c.BlockDefinition.ToString().ToLower().Contains("thruster"));
        foreach (IMyThrust handle in handles) {
            this.thrusters.Add(handle);
        }
    }

    public void setAutopilotStatus(bool autoPilotStatus) {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.SetAutoPilotEnabled(autoPilotStatus);
        }
    }

    public void setCollisionStatus(bool status) {
        foreach (IMyRemoteControl remote in this.remotes) {
            if (status == true) {
                remote.SetValueBool("CollisionAvoidance", true);
                remote.ApplyAction("CollisionAvoidance_On");
            } else {
                remote.SetValueBool("CollisionAvoidance", false);
                remote.ApplyAction("CollisionAvoidance_Off");
            }
        }
    }

    public void overrideThruster(string direction, float valueFloat) {
        /*foreach (IMyThrust thruster in this.thrusters) {
            if (thruster.GetProperty("Name").GetValue().Contains("(" + direction + ")")) {
                thruster.SetValueFloat("Override", valueFloat);
            }
        }*/
    }

    public double getDistanceFrom(Vector3D pos, Vector3D pos2) {
        return Math.Round( Vector3D.Distance( pos, pos2 ), 2 );
    }

    public void move(Vector3D coords, string waypointName) {
        if (coords.X == 0 && coords.Y == 0 && coords.Z == 0) {
            Display.print("[Error] Unable to set waypoint: " + waypointName);
            return; // Hard lock, in case of an issue @TODO: When you get logging system up and running, log these as exceptions.
        }
        if (this.lastMovementCommand == 0 || (Communication.getTimestamp() - this.lastMovementCommand) > 20) {
            this.lastMovementCommand = Communication.getTimestamp();
            this.clearPath();
            this.lastWaypoint = coords;
            Display.print("Waypoint set: " + waypointName);
            foreach (IMyRemoteControl remote in this.remotes) {
                remote.AddWaypoint(coords, waypointName);
                remote.SetAutoPilotEnabled(true);
            }
        }
    }

    public void setDirection(string direction) {
        foreach (IMyRemoteControl remote in this.remotes) {
            /*if (direction == "Forward") {
                remote.Direction = remote.Forward;
            } else if (direction == "Backward") {
                remote.Direction = remote.Backward;
            } else if (direction == "Left") {
                remote.Direction = remote.Left;
            } else if (direction == "Right") {
                remote.Direction = remote.Right;
            } else if (direction == "Up") {
                remote.Direction = remote.Up;
            } else if (direction == "Down") {
                remote.Direction = remote.Down;
            }*/
            remote.GetActionWithName(direction).Apply(remote);
        }
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

    public void returnToMaster() {
        if (Communication.masterDrone == null) {
            Communication.currentNode.status = "no-master";
            return; // Do nothing.
        }
        // Remove overrides
        this.overrideThruster("Forward", 0);
        double targetDistance = this.getDistanceFrom(this.getShipPosition(), Communication.masterDrone.position);
        if (targetDistance > 200) {
            Communication.currentNode.status = "returning";
            Vector3D queuePos = this.dockingHandle.getQueuePosition();
            this.move(queuePos, "returning");
            this.setCollisionStatus(false);
        } else {
            Communication.currentNode.status = "docking-step-unknown";
            if (Communication.masterDrone.connectorAnchorTopPosition.X != 0) {
                if (this.dockingHandle.hasDockingPermission == true) {
                    if (this.dockingHandle.dockingStep == 0) {
                        this.dockingHandle.handleFinalStep();
                    } else if (this.dockingHandle.dockingStep == 1) {
                        this.dockingHandle.handleStepOne();
                    } else {
                        this.dockingHandle.handleStepTwo();
                    }
                } else {
                    this.dockingHandle.handleStepQueueing();
                }
            } else {
                this.clearPath();
                Communication.currentNode.status = "waiting-for-connector";
            }
        }
    }

    public void addNearbyEntity(DetectedEntity entity) {
        bool found = false;
        long lastSeenByMe = 0;
        DetectedEntity myEnt;
        for (int i = 0; i < this.nearbyEntities.Count; i++) {
            myEnt = this.nearbyEntities[i];
            if (myEnt.id == entity.id) {
                if (myEnt.lastSeen > entity.lastSeen) {
                    entity.lastSeen = myEnt.lastSeen;
                }
                this.nearbyEntities[i] = entity;
                found = true;
            }
        }
        if (found == false) {
            this.nearbyEntities.Add(entity);
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
                tmp.lastSeen = Communication.getTimestamp();
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
                        tmp.lastSeen = Communication.getTimestamp();
                        tmp.distance = this.getDistanceFrom(entity.Position, sensor.GetPosition());
                        tmp.type = entity.Type;
                        this.nearbyEntities[i] = tmp;
                        break;
                    }
                }
            }
        } else {
            List<DetectedEntity> tmp = new List<DetectedEntity>();

            for (int i = 0; i < this.nearbyEntities.Count; i++) {
                DetectedEntity nearEntity = this.nearbyEntities[i];
                if (Communication.getTimestamp() - nearEntity.lastSeen < 300) {
                    tmp.Add(nearEntity);
                }
            }

            this.nearbyEntities = tmp;
        }
    }

    public void clearPath() {
        foreach (IMyRemoteControl remote in this.remotes) {
            remote.ClearWaypoints();
        }
    }
}
