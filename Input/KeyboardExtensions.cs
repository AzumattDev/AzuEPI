namespace AzuEPI.Input;

public static class KeyboardExtensions
{
    // thank you to 'Margmas' for giving me this snippet from VNEI https://github.com/MSchmoecker/VNEI/blob/master/VNEI/Logic/BepInExExtensions.cs#L21
    public static bool IsKeyDown(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && UnityEngine.Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(UnityEngine.Input.GetKey);
    }

    public static bool IsKeyHeld(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && UnityEngine.Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(UnityEngine.Input.GetKey);
    }
}