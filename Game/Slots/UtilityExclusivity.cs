using AzuEPI.Game.PlayerPreview;

namespace AzuEPI.Game.Slots;

internal static class UtilityExclusivity
{
	internal static bool Enabled => UtilitySlotsExclusive != null && UtilitySlotsExclusive.Value.isOn();

	private static bool IsUtility(ItemDrop.ItemData? item) => item is { m_shared: not null } && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility;

	internal static void Enforce(Player? player, ItemDrop.ItemData? keep)
	{
		if (!Enabled || player == null || keep == null) return;

		List<ItemDrop.ItemData>? equipped = player.GetInventory()?.GetEquippedItems();
		if (equipped == null) return;

		bool wasVanillaEquipping = CustomEquipVisuals.HideTypeWhileEquipping.IsVanillaEquipping;
		CustomEquipVisuals.HideTypeWhileEquipping.IsVanillaEquipping = false;
		try
		{
			foreach (ItemDrop.ItemData other in equipped.ToList())
			{
				if (other == null || other == keep || !IsUtility(other)) continue;
				player.UnequipItem(other, false);
			}
		}
		finally
		{
			CustomEquipVisuals.HideTypeWhileEquipping.IsVanillaEquipping = wasVanillaEquipping;
		}
	}

	internal static void TrimToOne(Player? player)
	{
		if (!Enabled || player == null) return;

		List<ItemDrop.ItemData>? equipped = player.GetInventory()?.GetEquippedItems();
		if (equipped == null) return;

		ItemDrop.ItemData? keep = IsUtility(player.m_utilityItem) ? player.m_utilityItem : equipped.FirstOrDefault(IsUtility);
		if (keep == null) return;

		Enforce(player, keep);
		player.SetupEquipment();
	}

	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
	private static class EquipItemPatch
	{
		[HarmonyPostfix]
		[HarmonyPriority(Priority.Last)]
		private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result)
		{
			if (!__result || !Enabled) return;
			if (__instance is not Player player) return;
			if (!IsUtility(item)) return;
			// wishbone/demister go through HideTypeWhileEquipping instead (type is still faked here)
			if (CustomEquipVisuals.IsManaged(item)) return;
			Enforce(player, item);
		}
	}
}
