public class CommunicationDataStructure
{
    private List<CommunicationDataStructureValue> package;

    public CommunicationDataStructure() {

    }

    public void newPackage() {
        this.package = new List<CommunicationDataStructureValue>();
    }

    public void add(CommunicationDataStructureValue value) {
        this.package.Add(value);
    }

    public void addData(string configName, string value) {
        this.package.Add(new CommunicationDataStructureValue(configName, value, false));
    }

    public void addRawData(string value) {
        this.package.Add(new CommunicationDataStructureValue("", value, true));
    }

    public string generateOutput() {
        List<string> output = new List<string>();
        foreach (CommunicationDataStructureValue data in this.package) {
            output.Add(data.getFormatted());
        }

        return string.Join("_", output);
    }

    public List<CommunicationDataStructureValue> getFormattedInput(string input) {
        this.package = new List<CommunicationDataStructureValue>();

        string[] dataSplitted = input.Split('_');
        if (dataSplitted.Count() > 0) {
            CommunicationDataStructureValue tmpValue;
            foreach (string entry in dataSplitted) {
                tmpValue = new CommunicationDataStructureValue("", "", false);
                tmpValue.importData(entry);
                this.add(tmpValue);
            }
        } else {
            this.addRawData(input);
        }

        return this.package;
    }
}
