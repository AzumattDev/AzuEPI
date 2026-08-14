using AzuEPI;

public static class ToggleExtensions
{
    extension(AzuExtendedPlayerInventoryPlugin.Toggle toggle)
    {
        public bool isOn()
        {
            return toggle == On;
        }

        public bool isOff()
        {
            return toggle == Off;
        }
    }
}