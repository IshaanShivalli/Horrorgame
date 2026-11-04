using UnityEngine;
using UnityEngine.InputSystem;

public sealed class player : MonoBehaviour
{
    public const float MaxHealth = 20f;
    public const float MaxHunger = 20f;
    private static readonly Key[] HotbarKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
    };

    private world world;
    private respawn respawn;
    private CharacterController controller;
    private Camera playerCamera;
    private inventory inventory;
    private float experience;
    private crafting crafting;
    private armour armour;
    private trade trading;
    private GameObject heldTorchLight;
    private float verticalSpeed;
    private float yaw;
    private float pitch;

    public float Health { get; private set; } = MaxHealth;
    public float Hunger { get; private set; } = MaxHunger;
    public bool IsDead => respawn != null && respawn.IsDead;
    public int ExperienceLevel { get; private set; }
    public float ExperienceProgress => experience / (10f + ExperienceLevel * 5f);

    public void Initialize(world currentWorld, respawn respawnController, CharacterController characterController, Camera camera)
    {
        world = currentWorld;
        respawn = respawnController;
        controller = characterController;
        playerCamera = camera;
        inventory = GetComponent<inventory>();
        crafting = GetComponent<crafting>();
        armour = GetComponent<armour>();
        trading = currentWorld.Trading;
        heldTorchLight = torch.CreateLight(playerCamera.transform, new Vector3(0.25f, -0.22f, 0.45f));
        heldTorchLight.GetComponent<Light>().intensity = 1.8f;
        heldTorchLight.GetComponent<Light>().range = 10f;
        heldTorchLight.SetActive(false);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, "an enemy");
    }

    public void TakeDamage(float amount, string attacker)
    {
        if (amount <= 0f || IsDead)
            return;

        float reducedDamage = armour == null ? amount : armour.ReduceDamage(amount);
        Health = Mathf.Max(0f, Health - reducedDamage);
        if (Health <= 0f)
            respawn.Die("You were killed by " + attacker);
    }

    public void RestoreHealth()
    {
        Health = MaxHealth;
    }

    public void AddExperience(float amount)
    {
        if (amount <= 0f)
            return;

        experience += amount;
        while (experience >= 10f + ExperienceLevel * 5f)
        {
            experience -= 10f + ExperienceLevel * 5f;
            ExperienceLevel++;
        }
    }

    public void ResetVerticalSpeed()
    {
        verticalSpeed = 0f;
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;

        UpdateHeldTorchLight();

        if (respawn.IsDead)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
                respawn.RespawnPlayer();
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (trading != null && trading.IsOpen)
                trading.Close();
            else
            if (world.IsPaused)
                world.ResumeGame();
            else if (inventory.IsOpen)
                inventory.Close();
            else if (crafting.IsOpen)
                crafting.Close();
            else
                world.TogglePause();
            return;
        }

        if (world.IsPaused)
            return;

        if (trading == null)
            trading = world.Trading;

        if (Keyboard.current.bKey.wasPressedThisFrame && trading != null)
            trading.Toggle();

        if (Keyboard.current.oKey.wasPressedThisFrame && armour != null &&
            armour.EquipOwnedItems())
            world.InteractionStatus = "Equipped available leather armour";

        if (trading != null && trading.IsOpen)
            return;

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (crafting.IsOpen)
                crafting.Close();
            inventory.Toggle();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (inventory.IsOpen)
                inventory.Close();
            crafting.Toggle();
        }

        for (int slot = 0; slot < HotbarKeys.Length; slot++)
        {
            if (Keyboard.current[HotbarKeys[slot]].wasPressedThisFrame)
                inventory.SelectSlot(slot);
        }

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            int scrollDirection = scroll > 0f ? -1 : 1;
            inventory.SelectSlot(
                (inventory.SelectedSlot + scrollDirection + inventory.HotbarSlotCount) %
                inventory.HotbarSlotCount);
        }

        if (inventory.IsOpen || crafting.IsOpen)
        {
            world.HandleBlockInput();
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = Mouse.current.delta.ReadValue() * 0.12f;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector2 movement = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) movement.y += 1f;
        if (Keyboard.current.sKey.isPressed) movement.y -= 1f;
        if (Keyboard.current.dKey.isPressed) movement.x += 1f;
        if (Keyboard.current.aKey.isPressed) movement.x -= 1f;

        Vector3 direction = (transform.right * movement.x + transform.forward * movement.y).normalized;
        float speed = Keyboard.current.leftShiftKey.isPressed ? 7f : 4.5f;

        if (controller.isGrounded)
        {
            verticalSpeed = -1f;
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                verticalSpeed = 7f;
        }
        else
        {
            verticalSpeed -= 20f * Time.deltaTime;
        }

        Vector3 velocity = direction * speed;
        velocity.y = verticalSpeed;
        controller.Move(velocity * Time.deltaTime);
        world.HandleBlockInput();
    }

    private void UpdateHeldTorchLight()
    {
        if (heldTorchLight == null || inventory == null)
            return;

        int selectedSlot = inventory.SelectedSlot;
        bool hasSelectedTorch = inventory.GetItemType(selectedSlot) == ItemTypes.Torch &&
            inventory.GetAmount(selectedSlot) > 0;
        heldTorchLight.SetActive(hasSelectedTorch && !IsDead);
    }
}
