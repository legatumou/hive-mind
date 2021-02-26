
public class CustomData
{
    private string data = "";
    public string key = "";
    public string value = "";

    public CustomData(string data) {
        this.data = data;
    }

    public void process() {
        string[] dataSplitted = this.data.Split('=');
        if (dataSplitted.Count() >= 2) {
            this.key = dataSplitted[0];
            this.value = dataSplitted[1];
        }
    }

    public static List<CustomData> getCustomData(string data) {
        string[] dataSplitted = data.Split('\n');
        List<CustomData> result = new List<CustomData>();
        foreach (string line in dataSplitted) {
            CustomData handle = new CustomData(line);
            handle.process();
            result.Add(handle);
        }

        return result;
    }

    public static CustomData findKeyFromList(string key, List<CustomData> customDataList) {
        foreach (CustomData line in customDataList) {
            if (line.key == key) {
                return line;
            }
        }

        return new CustomData("");
    }


}
