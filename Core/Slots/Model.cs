#if API
using System;
using UnityEngine;
#endif

namespace AzuEPI.Core.Slots;

public class Model
{
    internal class Slot
    {
        public string Name = null!;
        public Vector2 Position;
        public bool IsQuickSlot = false;
        public bool IsAPIAdded = false;
        public bool Occupied = false;
        public EquipmentSlot? EquipmentSlot => this as EquipmentSlot;
    }

    internal class EquipmentSlot : Slot
    {
        public Func<Player, ItemDrop.ItemData?>? Get = null!;
        public Func<ItemDrop.ItemData, bool>? Valid = null!;
    }
}