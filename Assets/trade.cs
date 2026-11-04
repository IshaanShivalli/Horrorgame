using UnityEngine;
using UnityEngine.InputSystem;

public sealed class trade : MonoBehaviour
{
    private inventory playerInventory;
    private armour playerArmour;
    private Transform playerTransform;
    private Transform merchant;

    public bool IsOpen { get; private set; }

    public void Initialize(inventory items, armour armor, Transform player)
    {
        playerInventory = items;
        playerArmour = armor;
        playerTransform = player;
        CreateMerchant();
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        if (playerTransform == null || merchant == null ||
            (playerTransform.position - merchant.position).sqrMagnitude > 36f)
        {
            GetComponent<world>().InteractionStatus = "Find the trader's camp to open the store";
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
        if (playerInventory == null || price <= 0 ||
            !playerInventory.CanAdd(itemType, amount) ||
            playerInventory.CountItem(ItemTypes.SilverCoin) < price)
            return false;

        if (!playerInventory.TryUseItem(ItemTypes.SilverCoin, price))
            return false;
        if (!playerInventory.Add(itemType, amount))
            throw new System.InvalidOperationException("Inventory capacity changed during a store purchase.");

        if (itemType == ItemTypes.LeatherHelmet || itemType == ItemTypes.LeatherChestplate ||
            itemType == ItemTypes.LeatherLeggings || itemType == ItemTypes.LeatherBoots)
            playerArmour.EquipItem(itemType);

        GameAudio.PlayPurchase();
        return true;
    }

    private void Update()
    {
        if (IsOpen && Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.bKey.wasPressedThisFrame))
            Close();
    }

    private void OnGUI()
    {
        if (!IsOpen)
            return;

        Rect panel = new Rect(
            Mathf.Max(12f, Screen.width * 0.5f - 230f),
            Mathf.Max(12f, Screen.height * 0.5f - 210f),
            Mathf.Min(460f, Screen.width - 24f),
            Mathf.Min(420f, Screen.height - 24f));
        GUI.Box(panel, "TRADER  |  Silver: " + playerInventory.CountItem(ItemTypes.SilverCoin));

        string[] labels =
        {
            "5 Silver arrows  -  4 silver",
            "Crossbow  -  18 silver",
            "Leather helmet  -  8 silver",
            "Leather chestplate  -  16 silver",
            "Leather leggings  -  12 silver",
            "Leather boots  -  8 silver"
        };
        int[] itemTypes =
        {
            ItemTypes.SilverArrow, ItemTypes.Crossbow, ItemTypes.LeatherHelmet,
            ItemTypes.LeatherChestplate, ItemTypes.LeatherLeggings, ItemTypes.LeatherBoots
        };
        int[] amounts = { 5, 1, 1, 1, 1, 1 };
        int[] prices = { 4, 18, 8, 16, 12, 8 };

        for (int i = 0; i < labels.Length; i++)
        {
            Rect button = new Rect(panel.x + 24f, panel.y + 48f + i * 44f, panel.width - 48f, 36f);
            if (GUI.Button(button, labels[i]))
            {
                if (TryBuy(itemTypes[i], amounts[i], prices[i]))
                    GetComponent<world>().InteractionStatus = "Purchased " + ItemTypes.GetName(itemTypes[i]);
                else
                    GetComponent<world>().InteractionStatus = "Not enough silver or inventory space";
            }
        }

        GUI.Label(new Rect(panel.x + 24f, panel.yMax - 38f, panel.width - 48f, 28f),
            "B or Esc to close. Armour is equipped when purchased.");
    }

    private void CreateMerchant()
    {
        GameObject vendor = new GameObject("Silver trader");
        vendor.transform.SetParent(transform, false);
        Vector3 position = playerTransform.position + playerTransform.right * 4f;
        position.y = GetComponent<world>().GetHeight(
            Mathf.RoundToInt(position.x),
            Mathf.RoundToInt(position.z)) + 0.55f;
        vendor.transform.position = position;
        merchant = vendor.transform;

        Material coat = MobModels.Material(null, "Trader coat", new Color(0.24f, 0.19f, 0.12f));
        Material skin = MobModels.Material(null, "Trader face", new Color(0.61f, 0.46f, 0.34f));
        Material wood = MobModels.Material(null, "Trader stall wood", new Color(0.3f, 0.18f, 0.09f));
        Material canvas = MobModels.Material(null, "Trader stall canvas", new Color(0.42f, 0.16f, 0.12f));
        MobModels.Part(vendor.transform, PrimitiveType.Cube, "Trading counter",
            new Vector3(0f, 0.56f, 0.85f), new Vector3(1.7f, 0.45f, 0.7f), wood);
        MobModels.Part(vendor.transform, PrimitiveType.Cube, "Shop canopy",
            new Vector3(0f, 2.45f, 0.35f), new Vector3(2f, 0.12f, 1.8f), canvas);
        MobModels.Part(vendor.transform, PrimitiveType.Cube, "Silver display",
            new Vector3(0.58f, 0.84f, 0.8f), new Vector3(0.25f, 0.16f, 0.2f),
            MobModels.Material(null, "Silver display coins", new Color(0.78f, 0.83f, 0.9f)));
        MobModels.Part(vendor.transform, PrimitiveType.Capsule, "Trader body",
            new Vector3(0f, 0.9f, 0f), new Vector3(0.48f, 0.63f, 0.36f), coat);
        MobModels.Part(vendor.transform, PrimitiveType.Sphere, "Trader head",
            new Vector3(0f, 1.65f, 0f), new Vector3(0.38f, 0.4f, 0.35f), skin);
    }
}
