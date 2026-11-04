using UnityEngine;

public sealed class trade : MonoBehaviour
{
    private inventory playerInventory;
    private armour playerArmour;
    private Vector2 shopScrollPosition;

    public bool IsOpen { get; private set; }

    public void Initialize(inventory items, armour armor)
    {
        playerInventory = items;
        playerArmour = armor;
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        IsOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool TryBuy(int itemType, int amount, int price)
    {
        return TryBuyItems(new[] { itemType }, new[] { amount }, price);
    }

    private bool TryBuyItems(int[] itemTypes, int[] amounts, int price)
    {
        if (playerInventory == null || price <= 0 ||
            playerInventory.CountItem(ItemTypes.SilverCoin) < price ||
            !playerInventory.CanAddBatch(itemTypes, amounts))
            return false;

        if (!playerInventory.TryUseItem(ItemTypes.SilverCoin, price))
            return false;

        for (int i = 0; i < itemTypes.Length; i++)
        {
            if (!playerInventory.Add(itemTypes[i], amounts[i]))
                throw new System.InvalidOperationException("Inventory capacity changed during a store purchase.");
        }

        if (playerArmour != null)
        {
            for (int i = 0; i < itemTypes.Length; i++)
                playerArmour.EquipItem(itemTypes[i]);
        }

        GameAudio.PlayPurchase();
        return true;
    }

    private bool TryBuyArmourSet(bool silver)
    {
        int firstPiece = silver ? ItemTypes.SilverHelmet : ItemTypes.LeatherHelmet;
        int price = silver ? 50 : 40;
        return TryBuyItems(
            new[] { firstPiece, firstPiece + 1, firstPiece + 2, firstPiece + 3 },
            new[] { 1, 1, 1, 1 },
            price);
    }

    private void OnGUI()
    {
        if (!IsOpen)
            return;

        float panelWidth = Mathf.Min(500f, Screen.width - 24f);
        float panelHeight = Mathf.Min(540f, Screen.height - 24f);
        Rect panel = new Rect(
            Mathf.Max(12f, Screen.width * 0.5f - 230f),
            Mathf.Max(12f, Screen.height * 0.5f - 210f),
            panelWidth,
            panelHeight);
        panel.x = (Screen.width - panel.width) * 0.5f;
        panel.y = (Screen.height - panel.height) * 0.5f;
        GUI.Box(panel, "");
        GUIStyle heading = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 30f),
            "SHOP  |  Coins: " + playerInventory.CountItem(ItemTypes.SilverCoin), heading);

        string[] labels =
        {
            "Silver arrows x10  -  15 coins",
            "Crossbow  -  18 coins",
            "Leather helmet  -  8 coins",
            "Leather chestplate  -  16 coins",
            "Leather leggings  -  12 coins",
            "Leather boots  -  8 coins",
            "Cooked meat x5  -  10 coins",
            "Regeneration potion x1  -  20 coins",
            "Leather armor set  -  40 coins",
            "Silver armor set  -  50 coins",
            "Splash potion of Reverse x1  -  75 coins"
        };
        int[] itemTypes =
        {
            ItemTypes.SilverArrow, ItemTypes.Crossbow, ItemTypes.LeatherHelmet,
            ItemTypes.LeatherChestplate, ItemTypes.LeatherLeggings, ItemTypes.LeatherBoots,
            ItemTypes.CookedMeat, ItemTypes.RegenerationPotion, 0, 0,
            ItemTypes.SplashPotionReverse
        };
        int[] amounts = { 10, 1, 1, 1, 1, 1, 5, 1, 0, 0, 1 };
        int[] prices = { 15, 18, 8, 16, 12, 8, 10, 20, 40, 50, 75 };

        const float rowHeight = 42f;
        Rect viewport = new Rect(panel.x + 12f, panel.y + 44f, panel.width - 24f, panel.height - 82f);
        shopScrollPosition = GUI.BeginScrollView(viewport, shopScrollPosition,
            new Rect(0f, 0f, viewport.width - 16f, labels.Length * rowHeight));
        for (int i = 0; i < labels.Length; i++)
        {
            Rect button = new Rect(0f, i * rowHeight, viewport.width - 18f, 36f);
            if (GUI.Button(button, labels[i]))
            {
                bool purchased = i == 8 ? TryBuyArmourSet(false) :
                    i == 9 ? TryBuyArmourSet(true) :
                    TryBuy(itemTypes[i], amounts[i], prices[i]);
                if (purchased)
                    GetComponent<world>().InteractionStatus = "Purchase complete";
                else
                    GetComponent<world>().InteractionStatus = "Not enough coins or inventory space";
            }
        }
        GUI.EndScrollView();

        GUI.Label(new Rect(panel.x + 18f, panel.yMax - 30f, panel.width - 36f, 22f),
            "B or Esc to close. Armor sets equip automatically.");
    }
}
