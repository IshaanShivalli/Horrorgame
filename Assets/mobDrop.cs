using UnityEngine;

public sealed class mobDrop : MonoBehaviour
{
    private inventory playerInventory;
    private world currentWorld;
    private int itemType;
    private int amount;
    private float rotationSpeed;

    public void Initialize(world owningWorld, inventory items, int droppedItem, int droppedAmount)
    {
        currentWorld = owningWorld;
        playerInventory = items;
        itemType = droppedItem;
        amount = droppedAmount;
        rotationSpeed = Random.Range(35f, 70f);

        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Silver coin";
        coin.transform.SetParent(transform, false);
        coin.transform.localScale = new Vector3(0.3f, 0.06f, 0.3f);
        coin.GetComponent<Renderer>().sharedMaterial = currentWorld.Materials[5];
        Collider coinCollider = coin.GetComponent<Collider>();
        if (coinCollider != null)
            Destroy(coinCollider);

        GameObject glint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glint.name = "Coin glint";
        glint.transform.SetParent(transform, false);
        glint.transform.localPosition = new Vector3(-0.06f, 0.045f, -0.035f);
        glint.transform.localScale = new Vector3(0.055f, 0.025f, 0.055f);
        glint.GetComponent<Renderer>().sharedMaterial = currentWorld.Materials[0];
        Collider glintCollider = glint.GetComponent<Collider>();
        if (glintCollider != null)
            Destroy(glintCollider);
    }

    private void Update()
    {
        if (currentWorld == null || playerInventory == null || currentWorld.PlayerTransform == null)
            return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        Vector3 playerPosition = currentWorld.PlayerTransform.position;
        if ((playerPosition - transform.position).sqrMagnitude > 2.25f)
            return;

        if (!playerInventory.Add(itemType, amount))
        {
            currentWorld.InteractionStatus = "Inventory is full";
            return;
        }

        currentWorld.InteractionStatus = "Collected " + amount + " silver coin" + (amount == 1 ? "" : "s");
        GameAudio.PlayPickup();
        Destroy(gameObject);
    }
}
