using UnityEngine;

public sealed class armour : MonoBehaviour
{
    private const string SaveKey = "VoxelWorld.Armour.v2";
    private const string LegacySaveKey = "VoxelWorld.Armour.v1";
    private const int EquipmentMask = 255;
    private inventory playerInventory;
    private int equippedMask;

    public int EquippedPieces { get; private set; }
    public float DamageReduction { get; private set; }

    private void Awake()
    {
        playerInventory = GetComponent<inventory>();
        int savedMask = PlayerPrefs.GetInt(SaveKey, -1);
        equippedMask = savedMask < 0
            ? PlayerPrefs.GetInt(LegacySaveKey, 0) & 15
            : savedMask & EquipmentMask;
        RecalculateProtection();
    }

    public bool EquipOwnedItems()
    {
        bool equippedAny = false;
        equippedAny |= Equip(ItemTypes.LeatherHelmet);
        equippedAny |= Equip(ItemTypes.LeatherChestplate);
        equippedAny |= Equip(ItemTypes.LeatherLeggings);
        equippedAny |= Equip(ItemTypes.LeatherBoots);
        equippedAny |= Equip(ItemTypes.SilverHelmet);
        equippedAny |= Equip(ItemTypes.SilverChestplate);
        equippedAny |= Equip(ItemTypes.SilverLeggings);
        equippedAny |= Equip(ItemTypes.SilverBoots);
        return equippedAny;
    }

    public float ReduceDamage(float damage)
    {
        return damage * (1f - DamageReduction);
    }

    public bool EquipItem(int itemType)
    {
        switch (itemType)
        {
            case ItemTypes.LeatherHelmet:
            case ItemTypes.LeatherChestplate:
            case ItemTypes.LeatherLeggings:
            case ItemTypes.LeatherBoots:
            case ItemTypes.SilverHelmet:
            case ItemTypes.SilverChestplate:
            case ItemTypes.SilverLeggings:
            case ItemTypes.SilverBoots: return Equip(itemType);
            default: return false;
        }
    }

    private bool Equip(int itemType)
    {
        int equipmentBit = 1 << (itemType <= ItemTypes.LeatherBoots
            ? itemType - ItemTypes.LeatherHelmet
            : 4 + itemType - ItemTypes.SilverHelmet);
        if (playerInventory == null || (equippedMask & equipmentBit) != 0 ||
            playerInventory.CountItem(itemType) <= 0 ||
            !playerInventory.TryUseItem(itemType))
            return false;

        equippedMask |= equipmentBit;
        RecalculateProtection();
        PlayerPrefs.SetInt(SaveKey, equippedMask);
        PlayerPrefs.Save();
        return true;
    }

    private void RecalculateProtection()
    {
        EquippedPieces = 0;
        DamageReduction = 0f;
        if ((equippedMask & 1) != 0) { EquippedPieces++; DamageReduction += 0.08f; }
        if ((equippedMask & 2) != 0) { EquippedPieces++; DamageReduction += 0.2f; }
        if ((equippedMask & 4) != 0) { EquippedPieces++; DamageReduction += 0.14f; }
        if ((equippedMask & 8) != 0) { EquippedPieces++; DamageReduction += 0.08f; }
        if ((equippedMask & 16) != 0) { EquippedPieces++; DamageReduction += 0.12f; }
        if ((equippedMask & 32) != 0) { EquippedPieces++; DamageReduction += 0.28f; }
        if ((equippedMask & 64) != 0) { EquippedPieces++; DamageReduction += 0.2f; }
        if ((equippedMask & 128) != 0) { EquippedPieces++; DamageReduction += 0.12f; }
        DamageReduction = Mathf.Min(0.7f, DamageReduction);
    }

    private void OnGUI()
    {
        if (EquippedPieces == 0 || Time.timeScale == 0f || playerInventory == null || playerInventory.IsOpen)
            return;

        GUI.color = new Color(0.8f, 0.72f, 0.55f);
        GUI.Label(new Rect(16f, 188f, 280f, 24f),
            "Armour: " + EquippedPieces + " pieces (" +
            Mathf.RoundToInt(DamageReduction * 100f) + "% damage reduction)");
        GUI.color = Color.white;
    }
}
