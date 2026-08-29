namespace AzuEPI.Game.Panels.Vanity;

public readonly struct VanityState
{
    public readonly bool HasVanity;
    public readonly bool IsHidden;
    public readonly int Hash;
    public readonly int Variant;

    public VanityState(bool has, bool hidden, int hash, int variant)
    {
        HasVanity = has;
        IsHidden = hidden;
        Hash = hash;
        Variant = variant;
    }
}

internal static class VanityLookup
{
    private static readonly Dictionary<int, (string prefab, ItemDrop itemDrop)> _byHash = new(2048);
    private static int _buildFrame = -1;
    private static readonly List<ItemDrop> _tempDrops = new(32);

    public static void InvalidateCache()
    {
        _byHash.Clear();
        _buildFrame = -1;
    }

    private static IEnumerable<ItemDrop> AllItemDrops(ObjectDB odb)
    {
        if (odb == null) yield break;
        foreach (GameObject? go in odb.m_items)
        {
            if (!go) continue;
            _tempDrops.Clear();
            go.GetComponentsInChildren(true, _tempDrops);
            foreach (ItemDrop? id in _tempDrops)
                if (id && id.m_itemData?.m_shared != null)
                    yield return id;
        }

        foreach (Recipe? r in odb.m_recipes)
        {
            if (!r || !r.m_item) continue;
            ItemDrop? id = r.m_item.GetComponent<ItemDrop>();
            if (id && id.m_itemData?.m_shared != null) yield return id;
        }
    }

    private static void EnsureBuilt()
    {
        ObjectDB? odb = ObjectDB.instance;
        if (!odb) return;

        if (_buildFrame == Time.frameCount) return;
        _buildFrame = Time.frameCount;

        if (_byHash.Count == 0)
        {
            foreach (ItemDrop? id in AllItemDrops(odb))
            {
                string prefab = id.m_itemData.m_dropPrefab ? id.m_itemData.m_dropPrefab.name : id.name;
                int hash = prefab.GetStableHashCode();
                _byHash[hash] = (prefab, id);
            }
        }
    }

    public static bool TryGetByHash(int hash, out string prefab, out ItemDrop itemDrop)
    {
        EnsureBuilt();
        if (_byHash.TryGetValue(hash, out (string prefab, ItemDrop itemDrop) t))
        {
            prefab = t.prefab;
            itemDrop = t.itemDrop;
            return true;
        }

        prefab = null;
        itemDrop = null;
        return false;
    }

    public static bool TryGetIcon(int hash, int variant, out Sprite sprite)
    {
        sprite = null;
        if (!TryGetByHash(hash, out _, out ItemDrop id)) return false;

        Sprite[]? icons = id.m_itemData?.m_shared?.m_icons;
        if (icons == null || icons.Length == 0) return false;

        int v = (variant >= 0 && variant < icons.Length) ? variant : 0;
        sprite = icons[v];
        return sprite != null;
    }
}

internal static class VanitySlots
{
    public static bool TryMapItemTypeToVisSlot(ItemDrop.ItemData.ItemType t, out VisSlot slot)
    {
        switch (t)
        {
            case ItemDrop.ItemData.ItemType.Helmet:
                slot = VisSlot.Helmet;
                return true;
            case ItemDrop.ItemData.ItemType.Chest:
                slot = VisSlot.Chest;
                return true;
            case ItemDrop.ItemData.ItemType.Legs:
                slot = VisSlot.Legs;
                return true;
            case ItemDrop.ItemData.ItemType.Shoulder:
                slot = VisSlot.Shoulder;
                return true;
            case ItemDrop.ItemData.ItemType.Utility:
                slot = VisSlot.Utility;
                return true;
            // case ItemDrop.ItemData.ItemType.Trinket: slot = VisSlot.Trinket; return true;
            default:
                slot = default;
                return false;
        }
    }

    public static VanityState GetState(VisEquipment ve, VisSlot slot)
    {
        if (!ve) return new VanityState(false, false, 0, 0);

        int val = slot switch
        {
            VisSlot.Helmet => VanityAPI.Get(ve, VanityZdoKeys.Helmet),
            VisSlot.Chest => VanityAPI.Get(ve, VanityZdoKeys.Chest),
            VisSlot.Legs => VanityAPI.Get(ve, VanityZdoKeys.Legs),
            VisSlot.Shoulder => VanityAPI.Get(ve, VanityZdoKeys.Shoulder),
            VisSlot.Utility => VanityAPI.Get(ve, VanityZdoKeys.Utility),
            _ => 0,
		};

        bool hidden = VanityAPI.IsHidden(val);
        bool hasVanity = !hidden && val != 0;

        int variantKey = VanityZdoKeys.VariantKeyForSlot(slot);
        int variant = variantKey != 0 ? VanityAPI.GetVariant(ve, variantKey) : 0;

        return new VanityState(hasVanity, hidden, val, variant);
    }
}