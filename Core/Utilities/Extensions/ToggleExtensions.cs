using AzuEPI;

public static class ToggleExtensions
{
    public static bool isOn(this AzuExtendedPlayerInventoryPlugin.Toggle toggle)
    {
        return toggle == On;
    }

    public static bool isOff(this AzuExtendedPlayerInventoryPlugin.Toggle toggle)
    {
        return toggle == Off;
    }
}