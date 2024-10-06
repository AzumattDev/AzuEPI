using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AzuExtendedPlayerInventory.EPI.Utilities;

public static class Utilities
{
    private static readonly string[] WbSuffixes = { "", "k", "m", "b" };

    internal static string FormatNumberSimpleNoDecimal(float number)
    {
        CalculateShortNumberAndSuffix(number, out double shortNumber, out string suffix);
        return $"{shortNumber:N0}{suffix}";
    }

    private static void CalculateShortNumberAndSuffix(float number, out double shortNumber, out string suffix)
    {
        int mag = (int)(Math.Log10(number) / 3);
        mag = Math.Min(WbSuffixes.Length - 1, mag);
        double divisor = Math.Pow(10, mag * 3);

        shortNumber = number / divisor;
        suffix = WbSuffixes[mag];
    }

    public static bool IgnoreKeyPresses(bool includeExtra = false)
    {
        if (!includeExtra)
            return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() ||
                   Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() ||
                   Chat.instance?.HasFocus() == true;

        return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() ||
               Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() ||
               Chat.instance?.HasFocus() == true || StoreGui.IsVisible() || InventoryGui.IsVisible() ||
               Menu.IsVisible() || TextViewer.instance?.IsVisible() == true;
    }
}

public static class KeyboardExtensions
{
    // thank you to 'Margmas' for giving me this snippet from VNEI https://github.com/MSchmoecker/VNEI/blob/master/VNEI/Logic/BepInExExtensions.cs#L21
    // since KeyboardShortcut.IsPressed and KeyboardShortcut.IsDown behave unintuitively
    public static bool IsKeyDown(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsKeyHeld(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }
}