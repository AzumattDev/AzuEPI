/*using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HarmonyLib;
using TMPro;
using UnityEngine.UI;
using AzuEPI.Game.Loadout;

namespace AzuEPI.PlayerPreview
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    static class CreatePlayerPreviewInventoryGuiShowPatch
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix(InventoryGui __instance)
        {
            PlayerPreviewManager.CreatePlayerPreviewShow();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
    static class InventoryGuiOnDestroyPatch
    {
        static void Prefix(InventoryGui __instance)
        {
            PlayerPreviewManager.DestroyPlayerPreview_All();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    static class HidePlayerPreview
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix(InventoryGui __instance)
        {
            if (AzuEPICharacterPanel.instance?.cam)
                AzuEPICharacterPanel.instance.cam.enabled = false;
            PlayerRotationController.instance?.ResetView(0f, 10f, 3.0f);
        }
    }

    // Instant refresh when visuals change on the real player
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
    static class PreviewInstantRefresh_VisEquipmentPatch
    {
        static void Postfix(VisEquipment __instance)
        {
            if (!Player.m_localPlayer) return;
            if (__instance != Player.m_localPlayer.m_visEquipment) return;

            ActualPlayerPreview.RenderOnce(); // <— core call: render the real player, one frame
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    static class PreviewInstantRefresh_EquipItem
    {
        static void Postfix() => PlayerPreviewManager.TryRefresh();
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    static class PreviewInstantRefresh_UnequipItem
    {
        static void Postfix() => PlayerPreviewManager.TryRefresh();
    }

    public class AzuEPICharacterPanel : MonoBehaviour, TextReceiver
    {
        public static AzuEPICharacterPanel instance;

        public RectTransform render; // RawImage rect
        public RawImage renderRawImage; // RawImage
        public Camera cam; // preview camera
        public RenderTexture renderTexture; // RT

        public static TMP_Dropdown LoadoutDropdown;

        void Awake() => instance = this;

        public string GetText() => string.Empty;

        public void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Invalid loadout name.");
                return;
            }

            if (PersonalLoadoutGui.SaveLoadout(text))
            {
                List<string> loadouts = PersonalLoadoutGui.GetAvailableLoadouts();
                LoadoutDropdown.value = loadouts.Count;
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"New loadout '{text}' created.");
            }
            else
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Failed to create loadout '{text}'.");
            }
        }
    }

    /// <summary>
    /// Renders the *actual* local player by temporarily swapping its hierarchy to the UI layer,
    /// rendering with a UI-only camera, then restoring all layers.
    /// </summary>
    internal static class ActualPlayerPreview
    {
        private static readonly List<Transform> _tmpStack = new(256);
        private static readonly List<int> _tmpLayers = new(256);

        private static int CharacterLayer => LayerMask.NameToLayer("character");
        private static int UILayer => LayerMask.NameToLayer("UI");
        private static int ConfigLayer => LayerMask.NameToLayer("UI");

        public static void EnsureSetup()
        {
            var panel = AzuEPICharacterPanel.instance;
            if (!panel) return;

            if (panel.cam == null)
            {
                // Create a free camera (no parent) so it doesn't inherit FollowPlayer motion/rotation
                GameObject camGo = new("Player Inspector Camera");
                panel.cam = camGo.AddComponent<Camera>();
                panel.cam.CopyFrom(Camera.main);
                /*panel.cam.clearFlags = CameraClearFlags.SolidColor;
                panel.cam.backgroundColor = Color.clear; // transparent#1#
                int[] layers = { 0, 1, 5, 8, 9, 10, 11, 12, 22, 26, 27, 28 }; // Default, TransparentFX, UI, effect, character, piece, terrain, item, weapon, character_net, character_noenv, vehicle got the layers from here: https://github.com/Valheim-Modding/Wiki/wiki/Layers
                int mask = 0;
                foreach (int layer in layers)
                    mask |= 1 << layer;
                panel.cam.cullingMask = mask;
                panel.cam.renderingPath = RenderingPath.UsePlayerSettings;
                panel.cam.useOcclusionCulling = false;

                /*panel.cam.nearClipPlane = 0.02f;
                panel.cam.farClipPlane = 17f;#1#
            }

            EnsureLights();
            UpdateRenderTexture();
            PositionCamera();
        }

        private static void EnsureLights()
        {
            var panel = AzuEPICharacterPanel.instance;
            if (!panel || !panel.cam) return;

            // Create two small fill lights that only affect the UI layer (so they only light while the player is swapped to UI)
            Light TryMake(string name, Vector3 localPos, float intensity, float range)
            {
                var follow = GameObject.Find("_GameMain/_Environment/FollowPlayer");
                var go = GameObject.Find(name) ?? new GameObject(name);
                if (follow) go.transform.SetParent(follow.transform, worldPositionStays: false);
                go.transform.localPosition = localPos;
                var L = go.GetComponent<Light>() ?? go.AddComponent<Light>();
                L.type = LightType.Point;
                L.intensity = intensity;
                L.range = range;
                L.shadows = LightShadows.None;
                // Very important: affect only UI layer, so world isn’t lit
                int[] layers = { 0, 1, 5, 8, 9, 12, 22, 26, 27, 28 }; // Default, TransparentFX, UI, effect, character, item, weapon, character_net, character_noenv, vehicle got the layers from here: https://github.com/Valheim-Modding/Wiki/wiki/Layers
                int mask = 0;
                foreach (int layer in layers)
                    L.cullingMask |= 1 << layer;
                //L.cullingMask = 1 << CharacterLayer;
                return L;
            }

            TryMake("Item Inspector Light1", new Vector3(1.25f, 0.9f, -1.75f), 3.2f, 3.0f);
            TryMake("Item Inspector Light2", new Vector3(-1.1f, -0.2f, 1.25f), 2.4f, 2.5f);
        }

        private static void PositionCamera()
        {
            var ctrl = PlayerRotationController.instance;
            if (!ctrl) return;
            ctrl.ApplyToCamera();
        }

        private static void UpdateRenderTexture()
        {
            var panel = AzuEPICharacterPanel.instance;
            if (!panel || !panel.render) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.render);

            int w = Mathf.Max(32, Mathf.RoundToInt(panel.render.rect.width));
            int h = Mathf.Max(32, Mathf.RoundToInt(panel.render.rect.height));
            if (w <= 32 || h <= 32)
            {
                // If still tiny this frame, bail; the delayed initial render will retry next frame
                return;
            }

            if (panel.renderTexture != null && (panel.renderTexture.width != w || panel.renderTexture.height != h))
            {
                panel.renderTexture.Release();
                UnityEngine.Object.Destroy(panel.renderTexture);
                panel.renderTexture = null;
            }

            if (panel.renderTexture == null)
            {
                panel.renderTexture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32)
                {
                    name = "AzuEPI_PlayerPreviewRT",
                    antiAliasing = 2
                };
                panel.renderTexture.Create();
            }

            panel.cam.targetTexture = panel.renderTexture;
            panel.renderRawImage.texture = panel.renderTexture;
        }

        /// <summary>Render the actual local player into the RT, isolated.</summary>
        public static void RenderOnce()
        {
            var panel = AzuEPICharacterPanel.instance;
            var player = Player.m_localPlayer;
            if (!panel || !panel.cam || !player) return;

            // Reposition cam if the player has moved a lot or UI was resized
            PositionCamera();
            UpdateRenderTexture();

            // One-frame swap of the player's hierarchy to UI layer, render, restore.
            using (new LayerSwapScope(player.gameObject, UILayer))
            using (new ShadowScope(player.gameObject, false))
            {
                panel.cam.enabled = true;
                panel.cam.Render();
            }
        }

        /// <summary>Disposable scope to flip an entire hierarchy to a target layer and restore on Dispose.</summary>
        private sealed class LayerSwapScope : IDisposable
        {
            public LayerSwapScope(GameObject root, int targetLayer)
            {
                _tmpStack.Clear();
                _tmpLayers.Clear();

                // Non-recursive DFS (alloc-light)
                var tr = root.transform;
                int head = 0;
                _tmpStack.Add(tr);

                while (head < _tmpStack.Count)
                {
                    var cur = _tmpStack[head++];
                    _tmpLayers.Add(cur.gameObject.layer); // store original
                    cur.gameObject.layer = targetLayer;

                    for (int i = 0, c = cur.childCount; i < c; ++i)
                        _tmpStack.Add(cur.GetChild(i));
                }
            }

            public void Dispose()
            {
                // restore in the same order
                for (int i = 0; i < _tmpStack.Count; ++i)
                {
                    var tr = _tmpStack[i];
                    if (tr) tr.gameObject.layer = _tmpLayers[i];
                }

                _tmpStack.Clear();
                _tmpLayers.Clear();
            }
        }

        /// <summary>Optional: disable shadow casting on all renderers for the preview render to avoid dark fringes.</summary>
        private sealed class ShadowScope : IDisposable
        {
            private static readonly List<(Renderer r, ShadowCastingMode mode)> _rend = new(128);

            public ShadowScope(GameObject root, bool enableShadows)
            {
                _rend.Clear();
                var rrs = root.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rrs)
                {
                    _rend.Add((r, r.shadowCastingMode));
                    r.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                }
            }

            public void Dispose()
            {
                foreach (var (r, mode) in _rend)
                    if (r)
                        r.shadowCastingMode = mode;
                _rend.Clear();
            }
        }
    }

    public class PlayerPreviewManager
    {
        public static PlayerPreviewManager Instance { get; private set; }
        public AzuEPICharacterPanel CharacterPanel { get; private set; }

        private PlayerPreviewManager()
        {
        }

        public static void Initialize()
        {
            Instance ??= new PlayerPreviewManager();
            Instance.CharacterPanel = AzuEPICharacterPanel.instance;
        }

        public static void CreatePlayerPreviewShow()
        {
            Initialize();
            ActualPlayerPreview.EnsureSetup();
            PlayerRotationController.instance?.InitializeFromPlayer();
            var panel = AzuEPICharacterPanel.instance;
            if (panel) panel.StartCoroutine(DelayedInitialRender());
        }

        private static System.Collections.IEnumerator DelayedInitialRender()
        {
            // Let the UI build this frame
            yield return null;

            // Ensure layout is fully baked
            var panel = AzuEPICharacterPanel.instance;
            if (panel && panel.render)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(panel.render);
            }

            // Update RT size now that rect is valid, then render once
            ActualPlayerPreview.EnsureSetup();
            yield return new WaitForEndOfFrame();
            ActualPlayerPreview.RenderOnce();
        }

        public static void DestroyPlayerPreview_All()
        {
            var panel = AzuEPICharacterPanel.instance;
            if (!panel) return;

            if (panel.cam)
            {
                UnityEngine.Object.Destroy(panel.cam.gameObject);
                panel.cam = null;
            }

            if (panel.renderTexture)
            {
                panel.renderTexture.Release();
                UnityEngine.Object.Destroy(panel.renderTexture);
                panel.renderTexture = null;
            }
        }

        public static void TryRefresh()
        {
            if (!Player.m_localPlayer) return;
            ActualPlayerPreview.RenderOnce();
        }
    }

    public static class GameObjectExtensions
    {
        public static void SetLayerForEntireHierarchy(this GameObject gameObject, int layer, int depth = 0)
        {
            if (depth >= 50) return;
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                child.gameObject.SetLayerForEntireHierarchy(layer, depth + 1);
        }
    }
}*/