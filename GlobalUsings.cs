global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using BepInEx;
global using BepInEx.Bootstrap;
global using BepInEx.Configuration;
global using HarmonyLib;
global using JetBrains.Annotations;
global using TMPro;
global using UnityEngine;
global using UnityEngine.EventSystems;
global using UnityEngine.UI;
global using Object = UnityEngine.Object;
#if !API
global using static AzuEPI.AzuExtendedPlayerInventoryPlugin;
global using static AzuEPI.Core.InventoryHandlers.UIBuilder;
global using static AzuEPI.AzuExtendedPlayerInventoryPlugin.Toggle;
global using AzuEPI.Core.InventoryHandlers;
global using AzuEPI.Core.Slots;
global using AzuEPI.Core.Utilities.Extensions;
global using AzuEPI.Game.Patches;
#endif