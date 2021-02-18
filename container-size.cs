
// Just a sample script to look at.
List<IMyTerminalBlock> containers = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> lcd = new List<IMyTerminalBlock>();

void Main()
{
    GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers);
    GridTerminalSystem.SearchBlocksOfName("Name of your LCD panel(s)", lcd);

    double maxFilling = 0.0d;
    double currentFilling = 0.0d;
    double rateFilling;
    string output;

    //DATA : take all the containers, see how they are filled (currentFilling), see what is their max filling (maxFilling)
    for (int i = 0; i < containers.Count; i++)
    {
        var inventory = ((IMyInventoryOwner)containers[i]).GetInventory(0);

        currentFilling += Convert.ToDouble(inventory.CurrentVolume.RawValue);
        maxFilling += Convert.ToDouble(inventory.MaxVolume.RawValue);
    }

    //Calcul of the filling percentage :
    rateFilling = Math.Round(100 * (currentFilling / maxFilling), 2);

    //Text :
    output = "Filling :\n " + rateFilling + "%";

    //Display :
    for (int i = 0; i < lcd.Count; i++)
    {
        var screen = (IMyTextPanel)lcd[i];
        screen.WritePublicText(output);
        screen.ShowTextureOnScreen();
        screen.ShowPublicTextOnScreen();
    }
}
