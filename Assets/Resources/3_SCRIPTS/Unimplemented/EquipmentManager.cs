using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
// !! This class is NOT functional
{
    public static EquipmentManager Instance;
    public Item[] equippedItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        equippedItems = new Item[System.Enum.GetNames(typeof(Slot)).Length + 1]; // + 1 for the weapon slot
    }

    public void Unequip(int slot) { }

    public void Equip(Weapon weapon)
    {
        int slotIndex = equippedItems.Length - 1;
        equippedItems[slotIndex] = weapon;
    }
    public void Equip(Gear gear)
    {
        int slot = (int) gear.stats.slot;
        equippedItems[slot] = gear;
    }

    public Weapon GetWeapon()
    {
        return (Weapon) equippedItems[equippedItems.Length - 1];
    }
}
