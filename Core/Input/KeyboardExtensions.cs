namespace AzuEPI.Core.Input;

public static class KeyboardExtensions
{
    // thank you to 'Margmas' for giving me this snippet from VNEI https://github.com/MSchmoecker/VNEI/blob/master/VNEI/Logic/BepInExExtensions.cs#L21
    extension(KeyboardShortcut shortcut)
    {
        public bool IsKeyDown()
        {
            return shortcut.MainKey != KeyCode.None && UnityEngine.Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(UnityEngine.Input.GetKey);
        }

        public bool IsKeyHeld()
        {
            return shortcut.MainKey != KeyCode.None && UnityEngine.Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(UnityEngine.Input.GetKey);
        }
    }
}