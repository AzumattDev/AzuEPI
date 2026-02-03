using Splatform;

namespace AzuEPI.Game.Patches;

[HarmonyPatch(typeof(MaterialMan), nameof(MaterialMan.Update))]
static class MaterialManUpdatePatch
{
    [HarmonyPriority(Priority.First)]
    static bool Prefix(MaterialMan __instance)
    {
        for (int index = __instance.m_containersToUpdate.Count - 1; index >= 0; --index)
        {
            try
            {
                __instance.m_containersToUpdate[index].UpdateBlock();
                __instance.m_containersToUpdate.RemoveAt(index);
            } catch {}
        }

        return false;
    }
}