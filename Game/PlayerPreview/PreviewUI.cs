using AzuEPI.Game.Loadout;
using AzuExtendedPlayerInventory;

namespace AzuEPI.PlayerPreview;

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
static class CreatePlayerPreveiwInventoryGuiShowPatch
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

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupAnimationState))]
static class UpdatePlayerPreviewVisuals
{
    static void Postfix(Humanoid __instance)
    {
        if (Player.m_localPlayer == null) return;
        if (__instance.IsPlayer() && __instance == Player.m_localPlayer)
        {
            if (AzuEPICharacterPanel.playerPreview != null)
            {
                PlayerPreviewManager.UpdatePlayerPreview(__instance);
            }
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
static class HidePlayerPreview
{
    [HarmonyPriority(Priority.Last)]
    static void Postfix(InventoryGui __instance)
    {
        if (AzuEPICharacterPanel.playerPreview)
            AzuEPICharacterPanel.playerPreview.SetActive(false);

        if (AzuEPICharacterPanel.instance?.cam)
            AzuEPICharacterPanel.instance.cam.enabled = false;
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
static class PreviewInstantRefresh_VisEquipmentPatch
{
    static void Postfix(VisEquipment __instance)
    {
        if (!Player.m_localPlayer) return;
        if (__instance != Player.m_localPlayer.m_visEquipment) return;
        if (AzuEPICharacterPanel.playerPreviewComp == null) return;

        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);

        PreviewLayerFix.ForceUILayer(AzuEPICharacterPanel.playerPreviewComp.m_visEquipment);

        var cam = AzuEPICharacterPanel.instance?.cam;
        if (cam) cam.Render();
        //PlayerPreviewManager.SyncAnimationState(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
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

    public RectTransform render;
    public RawImage renderRawImage;
    public Camera cam;
    public RenderTexture renderTexture;

    public readonly Vector3 basePosition = new(0, 50, 1);

    public static GameObject playerPreview;
    public static Player playerPreviewComp;

    public static TMP_Dropdown LoadoutDropdown;

    private void Awake()
    {
        instance = this;
    }

    public string GetText()
    {
        return string.Empty;
    }

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
            // TODO UpdateDropdownOptions();
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"New loadout '{text}' created.");
        }
        else
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Failed to create loadout '{text}'.");
        }
    }
}

static class VECloneSync
{
    private static int _lastStamp = 0;

    public static void ResetStamp() => _lastStamp = 0;

    public static void MirrorFrom(Player srcPlayer, Player dstPreview)
    {
        if (!srcPlayer || !dstPreview) return;

        var src = srcPlayer.m_visEquipment;
        var dst = dstPreview.m_visEquipment;

        int LeftItem, RightItem, ChestItem, LegItem, HelmetItem, ShoulderItem, UtilityItem, TrinketItem;
        int BeardItem = 0, HairItem = 0, LeftBack = 0, RightBack = 0;
        int ShoulderVar = src.m_shoulderItemVariant, LeftVar = src.m_leftItemVariant, LeftBackVar = src.m_leftBackItemVariant;

        var zdo = src.m_nview ? src.m_nview.GetZDO() : null;
        if (zdo != null)
        {
            LeftItem = zdo.GetInt(ZDOVars.s_leftItem);
            RightItem = zdo.GetInt(ZDOVars.s_rightItem);
            ChestItem = zdo.GetInt(ZDOVars.s_chestItem);
            LegItem = zdo.GetInt(ZDOVars.s_legItem);
            HelmetItem = zdo.GetInt(ZDOVars.s_helmetItem);
            ShoulderItem = zdo.GetInt(ZDOVars.s_shoulderItem);
            UtilityItem = zdo.GetInt(ZDOVars.s_utilityItem);
            TrinketItem = zdo.GetInt(ZDOVars.s_trinketItem);

            BeardItem = zdo.GetInt(ZDOVars.s_beardItem);
            HairItem = zdo.GetInt(ZDOVars.s_hairItem);
            LeftBack = zdo.GetInt(ZDOVars.s_leftBackItem);
            RightBack = zdo.GetInt(ZDOVars.s_rightBackItem);
            ShoulderVar = zdo.GetInt(ZDOVars.s_shoulderItemVariant);
            LeftVar = zdo.GetInt(ZDOVars.s_leftItemVariant);
            LeftBackVar = zdo.GetInt(ZDOVars.s_leftBackItemVariant);
        }
        else
        {
            LeftItem = string.IsNullOrEmpty(src.m_leftItem) ? 0 : src.m_leftItem.GetStableHashCode();
            RightItem = string.IsNullOrEmpty(src.m_rightItem) ? 0 : src.m_rightItem.GetStableHashCode();
            ChestItem = string.IsNullOrEmpty(src.m_chestItem) ? 0 : src.m_chestItem.GetStableHashCode();
            LegItem = string.IsNullOrEmpty(src.m_legItem) ? 0 : src.m_legItem.GetStableHashCode();
            HelmetItem = string.IsNullOrEmpty(src.m_helmetItem) ? 0 : src.m_helmetItem.GetStableHashCode();
            ShoulderItem = string.IsNullOrEmpty(src.m_shoulderItem) ? 0 : src.m_shoulderItem.GetStableHashCode();
            UtilityItem = string.IsNullOrEmpty(src.m_utilityItem) ? 0 : src.m_utilityItem.GetStableHashCode();
            TrinketItem = string.IsNullOrEmpty(src.m_trinketItem) ? 0 : src.m_trinketItem.GetStableHashCode();
            BeardItem = string.IsNullOrEmpty(src.m_beardItem) ? 0 : src.m_beardItem.GetStableHashCode();
            HairItem = string.IsNullOrEmpty(src.m_hairItem) ? 0 : src.m_hairItem.GetStableHashCode();
            LeftBack = string.IsNullOrEmpty(src.m_leftBackItem) ? 0 : src.m_leftBackItem.GetStableHashCode();
            RightBack = string.IsNullOrEmpty(src.m_rightBackItem) ? 0 : src.m_rightBackItem.GetStableHashCode();
        }

        int stamp = CombineHash(
            src.m_modelIndex,
            src.m_skinColor.GetHashCode(), src.m_hairColor.GetHashCode(),
            LeftItem, LeftVar, RightItem,
            ChestItem, LegItem, HelmetItem,
            ShoulderItem, ShoulderVar,
            UtilityItem, TrinketItem,
            BeardItem, HairItem,
            LeftBack, RightBack, LeftBackVar
        );
        if (stamp == _lastStamp) return;
        _lastStamp = stamp;

        dst.SetRightItem(src.m_rightItem);
        dst.SetLeftItem(src.m_leftItem, src.m_leftItemVariant);

        dst.SetChestItem(src.m_chestItem);
        dst.SetLegItem(src.m_legItem);
        dst.SetHelmetItem(src.m_helmetItem);
        dst.SetShoulderItem(src.m_shoulderItem, src.m_shoulderItemVariant);
        dst.SetUtilityItem(src.m_utilityItem);
        dst.SetTrinketItem(src.m_trinketItem);
        dst.SetLeftBackItem(src.m_leftBackItem, src.m_leftBackItemVariant);
        dst.SetRightBackItem(src.m_rightBackItem);

        dst.SetBeardItem(src.m_beardItem);
        dst.SetHairItem(src.m_hairItem);

        dst.SetModel(src.m_modelIndex);
        dst.SetSkinColor(src.m_skinColor);
        dst.SetHairColor(src.m_hairColor);

        try
        {
            dst.UpdateVisuals();
            PreviewLayerFix.ForceUILayer(dst);
        }
        catch
        {
        }

        dst.enabled = false;
    }

    internal static int CombineHash(params int[] hashes)
    {
        unchecked
        {
            int h = 17;
            foreach (var x in hashes)
                h = h * 31 + x;
            return h;
        }
    }
}

static class PreviewLayerFix
{
    public static void ForceUILayer(VisEquipment ve)
    {
        int ui = LayerMask.NameToLayer("UI");

        void Set(GameObject go)
        {
            if (!go) return;
            go.SetLayerForEntireHierarchy(ui);
        }

        void SetList(List<GameObject> list)
        {
            if (list == null) return;
            foreach (var go in list) Set(go);
        }

        Set(ve.m_leftItemInstance);
        Set(ve.m_rightItemInstance);
        Set(ve.m_helmetItemInstance);
        Set(ve.m_beardItemInstance);
        Set(ve.m_hairItemInstance);
        Set(ve.m_leftBackItemInstance);
        Set(ve.m_rightBackItemInstance);

        SetList(ve.m_chestItemInstances);
        SetList(ve.m_legItemInstances);
        SetList(ve.m_shoulderItemInstances);
        SetList(ve.m_utilityItemInstances);
        SetList(ve.m_trinketItemInstances);
    }
}

public class PlayerPreviewManager
{
    public static PlayerPreviewManager Instance { get; private set; }

    public AzuEPICharacterPanel CharacterPanel { get; set; }

    private PlayerPreviewManager()
    {
    }

    public static void Initialize()
    {
        Instance ??= new PlayerPreviewManager();
        Instance.CharacterPanel = AzuEPICharacterPanel.instance;
    }

    public void UpdateRenderTexture()
    {
        if (CharacterPanel == null || CharacterPanel.render == null) return;

        int w = Mathf.Max(2, Mathf.RoundToInt(CharacterPanel.render.sizeDelta.x));
        int h = Mathf.Max(2, Mathf.RoundToInt(CharacterPanel.render.sizeDelta.y));

        if (CharacterPanel.renderTexture != null &&
            (CharacterPanel.renderTexture.width != w || CharacterPanel.renderTexture.height != h))
        {
            CharacterPanel.renderTexture.Release();
            Object.Destroy(CharacterPanel.renderTexture);
            CharacterPanel.renderTexture = null;
        }

        if (CharacterPanel.renderTexture == null)
        {
            CharacterPanel.renderTexture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
            CharacterPanel.renderTexture.Create();
        }

        CharacterPanel.cam.targetTexture = CharacterPanel.renderTexture;
        CharacterPanel.renderRawImage.texture = CharacterPanel.renderTexture;
        CharacterPanel.cam.Render();
    }

    internal static GameObject CreatePlayerPreview()
    {
        var src = ZNetScene.instance.GetPrefab("Player");
        ZNetView.m_forceDisableInit = true;
        GameObject clone = Object.Instantiate(src);
        Player.s_players.Remove(clone.GetComponent<Player>());
        clone.SetActive(false);
        clone.transform.SetPositionAndRotation(AzuEPICharacterPanel.instance.basePosition - Vector3.up, Quaternion.identity);

        void DisableBehaviour<T>(string child = "") where T : Behaviour
        {
            var cloneChild = string.IsNullOrEmpty(child) ? clone : clone.transform.Find(child)?.gameObject;
            if (cloneChild == null) return;
            var c = cloneChild.GetComponent<T>();
            if (c) c.enabled = false;
        }

        DisableBehaviour<Player>();
        DisableBehaviour<PlayerController>();
        DisableBehaviour<ZNetView>();
        DisableBehaviour<ZSyncTransform>();
        DisableBehaviour<ZSyncAnimation>();
        DisableBehaviour<FootStep>();
        DisableBehaviour<Talker>();
        DisableBehaviour<Skills>();
        DisableBehaviour<FootStep>();
        DisableBehaviour<Container>();
        DisableBehaviour<CharacterAnimEvent>("Visual");
        ZNetView.m_forceDisableInit = false;
        var rb = clone.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        clone.transform.rotation = Quaternion.LookRotation(-Vector3.forward, Vector3.up);

        clone.SetLayerForEntireHierarchy(LayerMask.NameToLayer("UI"));

        AzuEPICharacterPanel.playerPreview = clone;
        AzuEPICharacterPanel.playerPreviewComp = clone.GetComponent<Player>();

        return clone;
    }

    internal static void CreatePlayerPreviewShow()
    {
        VECloneSync.ResetStamp();

        if (!AzuEPICharacterPanel.playerPreview)
            AzuEPICharacterPanel.playerPreview = CreatePlayerPreview();

        if (!AzuEPICharacterPanel.playerPreviewComp && AzuEPICharacterPanel.playerPreview)
            AzuEPICharacterPanel.playerPreviewComp = AzuEPICharacterPanel.playerPreview.GetComponent<Player>();

        var panel = AzuEPICharacterPanel.instance;
        if (panel?.cam == null)
        {
            Initialize();
            Instance.CreatePreviewCamera();
            Instance.CreatePreviewLights();
            Instance.UpdateRenderTexture();
        }

        var localPlayer = Player.m_localPlayer;
        if (!localPlayer || !AzuEPICharacterPanel.playerPreviewComp) return;

        AzuEPICharacterPanel.playerPreview.SetActive(true);
        panel.cam.enabled = true;

        var dst = AzuEPICharacterPanel.playerPreviewComp;
        dst.m_visEquipment.SetHairItem(localPlayer.m_hairItem);
        dst.m_visEquipment.SetHairColor(localPlayer.m_hairColor);
        dst.m_visEquipment.SetSkinColor(localPlayer.m_skinColor);
        dst.m_visEquipment.SetModel(localPlayer.m_visEquipment.m_currentModelIndex);
        dst.m_visEquipment.m_isPlayer = true;
        try
        {
            dst.m_visEquipment.UpdateVisuals();
            PreviewLayerFix.ForceUILayer(dst.m_visEquipment);
        }
        catch
        {
        }

        dst.m_animator.SetBool("wakeup", false);
        SyncAnimationState(localPlayer, dst);
        dst.m_animator.Update(0f);

        VECloneSync.MirrorFrom(localPlayer, dst);

        AzuEPICharacterPanel.playerPreview.gameObject.SetLayerForEntireHierarchy(LayerMask.NameToLayer("UI"));
        AzuEPICharacterPanel.playerPreview.transform.rotation = Quaternion.LookRotation(-Vector3.forward, Vector3.up);

        panel.cam.Render();
    }

    internal static void DestroyPlayerPreview_All()
    {
        if (AzuEPICharacterPanel.playerPreview)
            Object.Destroy(AzuEPICharacterPanel.playerPreview);
        AzuEPICharacterPanel.playerPreview = null;
        AzuEPICharacterPanel.playerPreviewComp = null;

        var panel = AzuEPICharacterPanel.instance;
        if (panel?.cam)
            Object.Destroy(panel.cam.gameObject);

        if (panel?.renderTexture)
        {
            panel.renderTexture.Release();
            Object.Destroy(panel.renderTexture);
            panel.renderTexture = null;
        }
    }

    internal static void TryRefresh()
    {
        if (!Player.m_localPlayer || AzuEPICharacterPanel.playerPreviewComp == null) return;
        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
        SyncAnimationState(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
        PreviewLayerFix.ForceUILayer(AzuEPICharacterPanel.playerPreviewComp.m_visEquipment);
        AzuEPICharacterPanel.instance?.cam?.Render();
    }

    internal static void UpdatePlayerPreview(Humanoid pHumanoid)
    {
        var src = pHumanoid;
        var dst = AzuEPICharacterPanel.playerPreviewComp;
        if (!src || !dst) return;

        dst.transform.rotation = Quaternion.LookRotation(-Vector3.forward, Vector3.up);

        VECloneSync.MirrorFrom((Player)src, dst);

        if (!AzuEPICharacterPanel.playerPreview.activeSelf)
            AzuEPICharacterPanel.playerPreview.SetActive(true);
    }

    internal static void SyncAnimationState(Humanoid p, Player playerPreviewComp)
    {
        Animator pAnimator = p.m_animator;
        Animator previewAnimator = playerPreviewComp.m_animator;

        AnimatorStateInfo stateInfo = pAnimator.GetCurrentAnimatorStateInfo(0);

        previewAnimator.Play(stateInfo.shortNameHash, 0, stateInfo.normalizedTime);

        foreach (AnimatorControllerParameter param in pAnimator.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    previewAnimator.SetBool(param.name, pAnimator.GetBool(param.name));
                    break;
                case AnimatorControllerParameterType.Float:
                    previewAnimator.SetFloat(param.name, pAnimator.GetFloat(param.name));
                    break;
                case AnimatorControllerParameterType.Int:
                    previewAnimator.SetInteger(param.name, pAnimator.GetInteger(param.name));
                    break;
                case AnimatorControllerParameterType.Trigger:
                    if (pAnimator.GetBool(param.name))
                    {
                        previewAnimator.SetTrigger(param.name);
                    }

                    break;
            }
        }

        previewAnimator.Update(0f);
    }

    internal void CreatePreviewCamera()
    {
        GameObject camEmpty = new GameObject("Player Inspector Camera");
        camEmpty.transform.position = AzuEPICharacterPanel.instance.basePosition + Vector3.forward * -3;
        CharacterPanel.cam = camEmpty.AddComponent<Camera>();
        //CharacterPanel.cam.CopyFrom(Camera.main);
        CharacterPanel.cam.clearFlags = CameraClearFlags.SolidColor;
        CharacterPanel.cam.backgroundColor = Color.clear;
        CharacterPanel.cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
        CharacterPanel.cam.renderingPath = RenderingPath.UsePlayerSettings;
        CharacterPanel.cam.useOcclusionCulling = false;
    }

    internal void CreatePreviewLights()
    {
        GameObject light1 = new GameObject("Item Inspector Light1");
        light1.transform.position = CharacterPanel.basePosition + new Vector3(1.5f, 0.5f, -2f);
        Light light1Comp = light1.AddComponent<Light>();
        light1Comp.type = LightType.Point;
        light1Comp.intensity = 3.5f;
        light1Comp.range = 3;

        GameObject light2 = new GameObject("Item Inspector Light2");
        light2.transform.position = CharacterPanel.basePosition + new Vector3(-1.5f, -0.5f, 1.5f);
        Light light2Comp = light2.AddComponent<Light>();
        light2Comp.type = LightType.Point;
        light2Comp.intensity = 2.5f;
        light2Comp.range = 3;
    }
}

public static class GameObjectExtensions
{
    public static void SetLayerForEntireHierarchy(this GameObject gameObject, int layer, int depth = 0)
    {
        if (depth >= 50)
        {
            return;
        }

        gameObject.layer = layer;

        foreach (Transform child in gameObject.transform)
        {
            SetLayerForEntireHierarchy(child.gameObject, layer, depth + 1);
        }
    }
}