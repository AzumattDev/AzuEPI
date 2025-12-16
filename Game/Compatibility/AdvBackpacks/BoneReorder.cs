#nullable disable
namespace AzuEPI.Game.Compatibility.AdvBackpacks;

// Big thank you to Jotunn and GoldenJude for this, it fixes issues with AdventureBackpacks backpacks having incorrectly ordered bones
internal static class BoneReorder
{
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLegEquipped)), HarmonyPostfix]
    private static void VisEquipmentOnSetLegEquiped(VisEquipment __instance, int hash, ref bool __result)
    {
        if (!__result || __instance.m_legItemInstances == null)
            return;
        ReorderBones(__instance, hash, __instance.m_legItemInstances);
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped)), HarmonyPostfix]
    private static void VisEquipmentOnSetHelmetEquiped(VisEquipment __instance, int hash, int hairHash, ref bool __result)
    {
        if (!__result || !(__instance.m_helmetItemInstance != null))
            return;
        ReorderBones(__instance, hash, new List<GameObject> { __instance.m_helmetItemInstance });
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped)), HarmonyPostfix]
    private static void VisEquipmentOnSetChestEquiped(VisEquipment __instance, int hash, ref bool __result)
    {
        if (!__result || __instance.m_chestItemInstances == null)
            return;
        ReorderBones(__instance, hash, __instance.m_chestItemInstances);
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetShoulderEquipped)), HarmonyPostfix]
    private static void VisEquipmentOnSetShoulderEquiped(VisEquipment __instance, int hash, int variant, ref bool __result)
    {
        if (!__result || __instance.m_shoulderItemInstances == null)
            return;
        ReorderBones(__instance, hash, __instance.m_shoulderItemInstances);
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetUtilityEquipped)), HarmonyPostfix]
    private static void VisEquipmentOnSetUtilityEquiped(VisEquipment __instance, int hash, ref bool __result)
    {
        if (!__result || __instance.m_utilityItemInstances == null)
            return;
        ReorderBones(__instance, hash, __instance.m_utilityItemInstances);
    }

    internal static void ReorderBones(VisEquipment visEquipment, int itemPrefabHash, List<GameObject> instancesToFix)
    {
        if (!(visEquipment != null))
            return;
        try
        {
            Transform skeletonRoot = visEquipment.transform.Find("Visual/Armature/Hips");
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabHash);
            if (!skeletonRoot || !itemPrefab)
            {
                AzuExtendedPlayerInventoryLogger.LogDebug($"Prefab missing components. Skipping {itemPrefab} {skeletonRoot}");
            }
            else
            {
                AzuExtendedPlayerInventoryLogger.LogDebug("Reordering bones");
                int childCount = itemPrefab.transform.childCount;
                int index1 = 0;
                for (int index2 = 0; index2 < childCount; ++index2)
                {
                    Transform child = itemPrefab.transform.GetChild(index2);
                    if (!child.name.StartsWith("attach_skin")) continue;
                    int index3 = 0;
                    SkinnedMeshRenderer[] componentsInChildren = instancesToFix[index1].GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (SkinnedMeshRenderer componentsInChild in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        componentsInChildren[index3].SetBones(componentsInChild.GetBoneNames(), skeletonRoot);
                        ++index3;
                    }

                    ++index1;
                }
            }
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Exception caught while reordering bones: {ex}");
        }
    }

    private static void SetBones(this SkinnedMeshRenderer skinnedMeshRenderer, string[] boneNames, Transform skeletonRoot)
    {
        Transform[] transformArray = new Transform[skinnedMeshRenderer.bones.Length];
        for (int index = 0; index < transformArray.Length; ++index)
            transformArray[index] = FindInChildren(skeletonRoot, boneNames[index]);
        skinnedMeshRenderer.bones = transformArray;
        skinnedMeshRenderer.rootBone = skeletonRoot;
    }

    private static string[] GetBoneNames(this SkinnedMeshRenderer skinnedMeshRenderer)
    {
        List<string> stringList = new();
        foreach (Transform bone in skinnedMeshRenderer.bones)
            stringList.Add(bone.name);
        return stringList.ToArray();
    }

    private static Transform FindInChildren(Transform transform, string name)
    {
        Transform inChildren1;
        if (transform.name == name)
        {
            inChildren1 = transform;
        }
        else
        {
            for (int index = 0; index < transform.childCount; ++index)
            {
                Transform inChildren2 = FindInChildren(transform.GetChild(index), name);
                if (inChildren2 != null)
                    return inChildren2;
            }

            inChildren1 = null;
        }

        return inChildren1;
    }
}