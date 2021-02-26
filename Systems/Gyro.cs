
public class Gyro
{
    public MyGridProgram myGrid;

    public Gyro(MyGridProgram myGrid) {
        this.myGrid = myGrid;
        this.disableOverride();
    }

    public IMyGyro getFirstGyro() {
        List<IMyGyro> blocks = new List<IMyGyro>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks);
        foreach (IMyGyro block in blocks) {
            return block;
        }
        return null;
    }

    public MatrixD getOrientation() {
        return Core.coreBlock.WorldMatrix.GetOrientation();
    }

    /*public Vector3D alignWithTarget(Vector3D Target) {
        Vector3D V3Dcenter = RemCon.GetPosition();
        Vector3D V3Dfow = RemCon.WorldMatrix.Forward;
        Vector3D V3Dup = RemCon.WorldMatrix.Up;
        Vector3D V3Dleft = RemCon.WorldMatrix.Left;

        Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);

        double TargetPitch = Math.Acos(Vector3D.Dot(V3Dfow, Vector3D.Reject(Vector3D.Normalize(RemCon.GetNaturalGravity()),V3Dleft))) - (Math.PI/2);

        double TargetRoll = Math.Acos(Vector3D.Dot(V3Dleft, Vector3D.Reject(Vector3D.Normalize(-RemCon.GetNaturalGravity()),V3Dfow))) - (Math.PI / 2);

        return new Vector3D(0, -TargetPitch, TargetRoll);
    }*/


    public void rotateShip(float amount) {
        List<IMyGyro> blocks = new List<IMyGyro>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks);
        foreach (IMyGyro block in blocks) {
            if (!block.GyroOverride) {
                block.ApplyAction("Override");
            }
            block.SetValueFloat("Power", 100);
    		block.SetValueFloat("Roll", amount);
        }
    }

    public void disableOverride() {
        List<IMyGyro> blocks = new List<IMyGyro>();
        this.myGrid.GridTerminalSystem.GetBlocksOfType<IMyGyro>(blocks);
        foreach (IMyGyro block in blocks) {
            if (block.GyroOverride) {
                block.ApplyAction("Override");
            }
            block.SetValueFloat("Yaw", 0);
            block.SetValueFloat("Pitch", 0);
    		block.SetValueFloat("Roll", 0);
        }
    }

}
