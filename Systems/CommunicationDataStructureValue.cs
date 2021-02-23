public class CommunicationDataStructureValue
{
    private string key = "";
    private string value = "";
    public bool isRawData = false;
    private List<CommunicationDataStructureValue> additionalData = new List<CommunicationDataStructureValue>();

    public CommunicationDataStructureValue(string configName, string value, bool isRawData) {
        this.key = configName;
        this.value = value;
        this.isRawData = isRawData;
    }

    public string getName() {
        return this.key;
    }

    public string getValue() {
        return this.value;
    }

    public int getAdditionalColumnCount() {
        return this.additionalData.Count;
    }

    public string getFormatted() {
        if (this.isRawData) {
            return this.getValue();
        } else {
            return this.getName() + "=" + this.getValue();
        }
    }

    public CommunicationDataStructureValue getAdditional(int index) {
        if (this.additionalData.Count > index && this.additionalData[index] != null) {
            return this.additionalData[index];
        }
        return new CommunicationDataStructureValue("", "0", true);
    }

    public void importData(string data) {
        string[] dataSplitted = data.Split('=');
        if (dataSplitted.Count() >= 2) {
            this.key = dataSplitted[0];
            this.value = dataSplitted[1];
            if (dataSplitted.Count() > 2) {
                CommunicationDataStructureValue extra;
                for (int i = 2; i < dataSplitted.Count(); i++) {
                    extra = new CommunicationDataStructureValue("", "", true);
                    extra.importData(dataSplitted[i]);
                    this.additionalData.Insert(this.additionalData.Count, extra);
                }
            }
        } else {
            this.value = data;
            this.isRawData = true;
        }
    }
}
