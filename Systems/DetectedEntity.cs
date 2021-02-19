
public class DetectedEntity
{
    public DetectedEntity()
    {

    }

    public long id { get; set; }
    public double distance { get; set; }
    public string name { get; set; }
    public Vector3D position { get; set; }
    public MyDetectedEntityType type { get; set; }
    public MyDetectedEntityInfo entityInfo { get; set; }
}
