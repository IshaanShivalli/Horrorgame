using System;
using UnityEngine;

/// <summary>Player-owned item stacks and the matching voxel-style inventory UI.</summary>
public sealed class inventory : MonoBehaviour
{
    public const int HotbarSlotCount = 10;
    public const int RowCount = 3;
    public const int SlotCount = HotbarSlotCount * RowCount;
    public const int MaxStackSize = 64;

    [Serializable]
    private sealed class SavedInventory
    {
        public int[] amounts;
        public int[] itemTypes;
        public int selectedSlot;
    }

    private readonly int[] itemTypes = new int[SlotCount];
    private readonly int[] amounts = new int[SlotCount];
    private const string SaveKey = "VoxelWorld.Inventory.v1";
    private static Texture2D itemIconAtlas;

    public int SelectedSlot { get; private set; }
    public bool IsOpen { get; private set; }
    public int SelectedBlockType => GetItemType(SelectedSlot);

    public int GetAmount(int slot)
    {
        return slot >= 0 && slot < SlotCount ? amounts[slot] : 0;
    }

    public int GetItemType(int slot)
    {
        return slot >= 0 && slot < SlotCount ? itemTypes[slot] : 0;
    }

    public int CountItem(int itemType)
    {
        int total = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (itemTypes[i] == itemType)
                total += amounts[i];
        }
        return total;
    }

    public bool CanAdd(int itemType, int amount = 1)
    {
        if (itemType <= 0 || amount <= 0)
            return false;

        int available = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (itemTypes[i] == itemType)
                available += MaxStackSize - amounts[i];
            else if (itemTypes[i] == 0)
                available += MaxStackSize;

            if (available >= amount)
                return true;
        }
        return false;
    }

    public bool CanAddBatch(int[] types, int[] itemAmounts)
    {
        if (types == null || itemAmounts == null || types.Length != itemAmounts.Length)
            return false;

        int[] available = new int[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            available[i] = amounts[i];

        for (int item = 0; item < types.Length; item++)
        {
            if (types[item] <= 0 || itemAmounts[item] <= 0)
                return false;

            int remaining = itemAmounts[item];
            for (int slot = 0; slot < SlotCount && remaining > 0; slot++)
            {
                if (itemTypes[slot] != types[item] || available[slot] >= MaxStackSize)
                    continue;

                int accepted = Mathf.Min(remaining, MaxStackSize - available[slot]);
                available[slot] += accepted;
                remaining -= accepted;
            }

            for (int slot = 0; slot < SlotCount && remaining > 0; slot++)
            {
                if (itemTypes[slot] != 0 || available[slot] != 0)
                    continue;

                int accepted = Mathf.Min(remaining, MaxStackSize);
                available[slot] = accepted;
                remaining -= accepted;
            }

            if (remaining > 0)
                return false;
        }
        return true;
    }

    public bool Add(int itemType, int amount = 1)
    {
        if (itemType <= 0 || amount <= 0)
            return false;

        int remaining = amount;
        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (itemTypes[i] != itemType || amounts[i] >= MaxStackSize)
                continue;

            int accepted = Mathf.Min(remaining, MaxStackSize - amounts[i]);
            amounts[i] += accepted;
            remaining -= accepted;
        }

        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (itemTypes[i] != 0)
                continue;

            int accepted = Mathf.Min(remaining, MaxStackSize);
            itemTypes[i] = itemType;
            amounts[i] = accepted;
            remaining -= accepted;
        }

        if (remaining < amount)
            Save();
        return remaining == 0;
    }

    public bool TryUseSelected()
    {
        return TryUseSlot(SelectedSlot, 1);
    }

    public bool TryUseItem(int itemType, int amount = 1)
    {
        if (itemType <= 0 || amount <= 0 || CountItem(itemType) < amount)
            return false;

        int remaining = amount;
        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (itemTypes[i] != itemType)
                continue;

            int used = Mathf.Min(remaining, amounts[i]);
            amounts[i] -= used;
            remaining -= used;
            if (amounts[i] == 0)
                itemTypes[i] = 0;
        }

        Save();
        return true;
    }

    private bool TryUseSlot(int slot, int amount)
    {
        if (slot < 0 || slot >= SlotCount || amount <= 0 || amounts[slot] < amount)
            return false;

        amounts[slot] -= amount;
        if (amounts[slot] == 0)
            itemTypes[slot] = 0;
        Save();
        return true;
    }

    public void SelectSlot(int slot)
    {
        if (slot < 0 || slot >= HotbarSlotCount)
            return;
        SelectedSlot = slot;
        Save();
    }

    public void SwapSlots(int first, int second)
    {
        if (first < 0 || first >= SlotCount || second < 0 || second >= SlotCount || first == second)
            return;

        int savedType = itemTypes[first];
        int savedAmount = amounts[first];
        itemTypes[first] = itemTypes[second];
        amounts[first] = amounts[second];
        itemTypes[second] = savedType;
        amounts[second] = savedAmount;
        Save();
    }

    public void Toggle()
    {
        SetOpen(!IsOpen);
    }

    public void Close()
    {
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void Awake()
    {
        itemIconAtlas = Resources.Load<Texture2D>("textures/item_icons");
        Load();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void OnGUI()
    {
        if (Time.timeScale == 0f)
            return;

        if (IsOpen)
            DrawFullInventory();
        else
            DrawHotbar();
    }

    private void DrawHotbar()
    {
        float size = Mathf.Min(
            Mathf.Clamp(Screen.height * 0.075f, 30f, 68f),
            Mathf.Max(18f, (Screen.width - 32f - (HotbarSlotCount - 1) * 4f) /
                (HotbarSlotCount + (HotbarSlotCount - 1) * 0.08f)));
        float gap = Mathf.Max(4f, size * 0.08f);
        float width = HotbarSlotCount * size + (HotbarSlotCount - 1) * gap;
        float left = (Screen.width - width) * 0.5f;
        float top = Screen.height - size - 22f;

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            Rect slot = new Rect(left + i * (size + gap), top, size, size);
            DrawSlot(slot, i, i == SelectedSlot, true);
        }
    }

    private void DrawFullInventory()
    {
        float size = Mathf.Clamp(Screen.height * 0.135f, 32f, 72f);
        float padding = Mathf.Max(8f, size * 0.16f);
        float gap = Mathf.Max(4f, size * 0.08f);
        size = Mathf.Min(size,
            (Screen.width - 32f - 2f * padding - (HotbarSlotCount - 1) * gap) / HotbarSlotCount);
        padding = Mathf.Max(8f, size * 0.16f);
        gap = Mathf.Max(4f, size * 0.08f);
        float width = HotbarSlotCount * size + (HotbarSlotCount - 1) * gap;
        float height = RowCount * size + (RowCount - 1) * gap;
        Rect panel = new Rect(
            (Screen.width - width - padding * 2f) * 0.5f,
            (Screen.height - height - padding * 2f) * 0.5f,
            width + padding * 2f,
            height + padding * 2f);

        DrawRect(panel, new Color(0.18f, 0.105f, 0.055f, 0.96f));
        DrawRect(new Rect(panel.x + 3f, panel.y + 3f, panel.width - 6f, panel.height - 6f),
            new Color(0.55f, 0.34f, 0.17f, 0.98f));
        DrawRect(new Rect(panel.x + 7f, panel.y + 7f, panel.width - 14f, panel.height - 14f),
            new Color(0.31f, 0.19f, 0.105f, 0.98f));

        for (int i = 0; i < SlotCount; i++)
        {
            int row = i / HotbarSlotCount;
            int column = i % HotbarSlotCount;
            Rect slot = new Rect(
                panel.x + padding + column * (size + gap),
                panel.y + padding + row * (size + gap),
                size,
                size);
            DrawSlot(slot, i, i == SelectedSlot, row == 0);
        }
    }

    private void DrawSlot(Rect rect, int slot, bool selected, bool showHotbarNumber)
    {
        Color border = selected ? new Color(0.26f, 0.43f, 0.17f) : new Color(0.22f, 0.13f, 0.07f);
        DrawRect(rect, border);
        DrawRect(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f),
            new Color(0.72f, 0.55f, 0.34f));
        DrawRect(new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f),
            new Color(0.83f, 0.79f, 0.66f));

        int itemType = itemTypes[slot];
        if (itemType != 0)
        {
            DrawItemIcon(itemType, new Rect(rect.x + rect.width * 0.25f, rect.y + rect.height * 0.2f,
                rect.width * 0.5f, rect.height * 0.5f));
            GUIStyle countStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.22f), 11, 18),
                fontStyle = FontStyle.Bold
            };
            countStyle.normal.textColor = Color.white;
            Rect countRect = new Rect(rect.x + 3f, rect.y + 1f, rect.width - 7f, rect.height - 4f);
            DrawTextShadow(countRect, amounts[slot].ToString(), countStyle);
        }

        if (showHotbarNumber)
        {
            GUIStyle numberStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerLeft,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.19f), 10, 15),
                fontStyle = FontStyle.Bold
            };
            numberStyle.normal.textColor = Color.white;
            DrawTextShadow(new Rect(rect.x + 4f, rect.y + 1f, 20f, rect.height - 4f),
                slot == HotbarSlotCount - 1 ? "0" : (slot + 1).ToString(), numberStyle);
        }

        if (selected)
            DrawRect(new Rect(rect.xMax - 5f, rect.y + 5f, 3f, rect.height - 10f), new Color(0.37f, 0.78f, 0.2f));

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            if (IsOpen)
            {
                if (slot < HotbarSlotCount)
                    SelectSlot(slot);
                else
                    SwapSlots(SelectedSlot, slot);
            }
            else
            {
                SelectSlot(slot);
            }
            Event.current.Use();
        }
    }

    private static void DrawItemIcon(int itemType, Rect rect)
    {
        int iconIndex = itemType - 1;
        if (itemIconAtlas != null && iconIndex >= 0 && iconIndex < 32)
        {
            itemIconAtlas.filterMode = FilterMode.Point;
            itemIconAtlas.wrapMode = TextureWrapMode.Clamp;
            int column = iconIndex % 8;
            int row = iconIndex / 8;
            GUI.DrawTextureWithTexCoords(
                rect,
                itemIconAtlas,
                new Rect(column / 8f, 1f - (row + 1) / 4f, 1f / 8f, 1f / 4f));
            return;
        }

        Color color;
        switch (itemType)
        {
            case ItemTypes.Grass: color = new Color(0.28f, 0.64f, 0.24f); break;
            case ItemTypes.Dirt: color = new Color(0.48f, 0.32f, 0.19f); break;
            case ItemTypes.Log: color = new Color(0.36f, 0.2f, 0.1f); break;
            case ItemTypes.Planks: color = new Color(0.68f, 0.46f, 0.24f); break;
            case ItemTypes.CraftingTable: color = new Color(0.53f, 0.34f, 0.16f); break;
            case ItemTypes.Stick: color = new Color(0.7f, 0.53f, 0.3f); break;
            case ItemTypes.WoodenSword:
            case ItemTypes.WoodenAxe:
            case ItemTypes.WoodenPickaxe: color = new Color(0.55f, 0.57f, 0.6f); break;
            case ItemTypes.Torch: color = new Color(1f, 0.67f, 0.2f); break;
            case ItemTypes.SilverOre:
            case ItemTypes.SilverCoin:
            case ItemTypes.SilverArrow: color = new Color(0.75f, 0.83f, 0.92f); break;
            case ItemTypes.Crossbow: color = new Color(0.39f, 0.25f, 0.14f); break;
            case ItemTypes.LeatherHelmet:
            case ItemTypes.LeatherChestplate:
            case ItemTypes.LeatherLeggings:
            case ItemTypes.LeatherBoots: color = new Color(0.48f, 0.29f, 0.16f); break;
            default: color = new Color(0.52f, 0.55f, 0.58f); break;
        }
        float inset = rect.width * 0.12f;
        DrawRect(new Rect(rect.x + inset, rect.y, rect.width - inset * 2f, rect.height), color);
        DrawRect(new Rect(rect.x, rect.y + inset, inset, rect.height - inset), color * 0.72f);
        DrawRect(new Rect(rect.xMax - inset, rect.y + inset, inset, rect.height - inset), color * 0.58f);
        DrawRect(new Rect(rect.x + inset, rect.yMax - inset, rect.width - inset * 2f, inset), color * 0.68f);
        if (itemType == ItemTypes.Grass)
            DrawRect(new Rect(rect.x + inset, rect.y, rect.width - inset * 2f, rect.height * 0.22f),
                new Color(0.42f, 0.76f, 0.3f));
        if (itemType == ItemTypes.Torch)
            DrawRect(new Rect(rect.x + rect.width * 0.4f, rect.y, rect.width * 0.2f, rect.height * 0.58f),
                new Color(1f, 0.9f, 0.35f));
    }

    private static void DrawTextShadow(Rect rect, string text, GUIStyle style)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0.12f, 0.09f, 0.06f, 0.9f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
        GUI.color = previous;
        GUI.Label(rect, text, style);
    }

    private static void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new SavedInventory
        {
            amounts = amounts,
            itemTypes = itemTypes,
            selectedSlot = SelectedSlot
        }));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string saved = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(saved))
            return;

        SavedInventory data = JsonUtility.FromJson<SavedInventory>(saved);
        if (data == null || data.amounts == null)
            return;

        if (data.itemTypes == null)
        {
            for (int i = 0; i < Mathf.Min(HotbarSlotCount, data.amounts.Length); i++)
            {
                amounts[i] = Mathf.Clamp(data.amounts[i], 0, MaxStackSize);
                itemTypes[i] = amounts[i] > 0 ? i + 1 : 0;
            }
        }
        else if (data.amounts.Length == 18 && data.itemTypes.Length == 18 && SlotCount != 18)
        {
            int legacyHotbarSlotCount = 3;
            for (int i = 0; i < Mathf.Min(legacyHotbarSlotCount, data.amounts.Length); i++)
            {
                amounts[i] = Mathf.Clamp(data.amounts[i], 0, MaxStackSize);
                itemTypes[i] = amounts[i] > 0 ? Mathf.Max(0, data.itemTypes[i]) : 0;
            }

            for (int i = legacyHotbarSlotCount; i < data.amounts.Length; i++)
            {
                int newSlot = HotbarSlotCount + i - legacyHotbarSlotCount;
                amounts[newSlot] = Mathf.Clamp(data.amounts[i], 0, MaxStackSize);
                itemTypes[newSlot] = amounts[newSlot] > 0 ? Mathf.Max(0, data.itemTypes[i]) : 0;
            }
        }
        else
        {
            for (int i = 0; i < SlotCount && i < data.amounts.Length && i < data.itemTypes.Length; i++)
            {
                amounts[i] = Mathf.Clamp(data.amounts[i], 0, MaxStackSize);
                itemTypes[i] = amounts[i] > 0 ? Mathf.Max(0, data.itemTypes[i]) : 0;
            }
        }
        SelectedSlot = Mathf.Clamp(data.selectedSlot, 0, HotbarSlotCount - 1);
    }
}
