
public class DetectedEntity
{
    public DetectedEntity() {
        this.id = 0;
    }

    public long id { get; set; }
    public double distance { get; set; }
    public string name { get; set; }
    public long lastSeen { get; set; }
    public Vector3D position { get; set; }
    public MyDetectedEntityType type { get; set; }
    public MyDetectedEntityInfo entityInfo { get; set; }

    public static int getEntityTypeInteger(MyDetectedEntityType type) {
        if (type == MyDetectedEntityType.None) {
            return 0;
        } else if (type == MyDetectedEntityType.Unknown) {
            return 1;
        } else if (type == MyDetectedEntityType.SmallGrid) {
            return 2;
        } else if (type == MyDetectedEntityType.LargeGrid) {
            return 3;
        } else if (type == MyDetectedEntityType.CharacterHuman) {
            return 4;
        } else if (type == MyDetectedEntityType.CharacterOther) {
            return 5;
        } else if (type == MyDetectedEntityType.FloatingObject) {
            return 6;
        } else if (type == MyDetectedEntityType.Asteroid) {
            return 7;
        } else if (type == MyDetectedEntityType.Planet) {
            return 8;
        } else if (type == MyDetectedEntityType.Meteor) {
            return 9;
        } else if (type == MyDetectedEntityType.Missile) {
            return 10;
        } else {
            return 11;
        }
    }

    public static MyDetectedEntityType getEntityType(int type) {
        if (type == 0) {
            return MyDetectedEntityType.None;
        } else if (type == 1) {
            return MyDetectedEntityType.Unknown;
        } else if (type == 2) {
            return MyDetectedEntityType.SmallGrid;
        } else if (type == 3) {
            return MyDetectedEntityType.LargeGrid;
        } else if (type == 4) {
            return MyDetectedEntityType.CharacterHuman;
        } else if (type == 5) {
            return MyDetectedEntityType.CharacterOther;
        } else if (type == 6) {
            return MyDetectedEntityType.FloatingObject;
        } else if (type == 7) {
            return MyDetectedEntityType.Asteroid;
        } else if (type == 8) {
            return MyDetectedEntityType.Planet;
        } else if (type == 9) {
            return MyDetectedEntityType.Meteor;
        } else if (type == 10) {
            return MyDetectedEntityType.Meteor;
        } else {
            return MyDetectedEntityType.Unknown;
        }
    }
}
