using UnityEngine;
using UnityEngine.InputSystem;

public sealed class blockBreaking : MonoBehaviour
{
    private world world;
    private Camera playerCamera;
    private Transform playerTransform;
    private inventory playerInventory;
    private Vector3Int? breakingBlock;
    private float breakProgress;
    private Vector3 breakHitPoint;
    private float nextAttackTime;

    public void Initialize(world currentWorld, Camera camera, Transform player, inventory playerItems)
    {
        world = currentWorld;
        playerCamera = camera;
        playerTransform = player;
        playerInventory = playerItems;
    }

    public void HandleInput()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
        {
            ResetBreaking();
            return;
        }

        bool breakHeld = Mouse.current.leftButton.isPressed;
        bool placePressed = Mouse.current.rightButton.wasPressedThisFrame;

        if (placePressed && ItemTypes.IsConsumable(playerInventory.SelectedBlockType))
        {
            UseSelectedConsumable();
            return;
        }

        if (breakHeld && VoxelPhysics.TryGetMobTarget(
            playerCamera,
            playerTransform,
            playerInventory.SelectedBlockType == ItemTypes.Crossbow ? 24f : 3.5f,
            out hostileMob target))
        {
            ResetBreaking();
            if (Time.time >= nextAttackTime)
            {
                int weapon = playerInventory.SelectedBlockType;
                bool firedArrow = weapon != ItemTypes.Crossbow ||
                    playerInventory.TryUseItem(ItemTypes.SilverArrow);
                if (!firedArrow)
                {
                    world.InteractionStatus = "You need silver arrows";
                    return;
                }

                target.TakeDamage(
                    weapon == ItemTypes.Crossbow ? ItemTypes.GetAttackDamage(ItemTypes.SilverArrow) :
                    ItemTypes.GetAttackDamage(weapon));
                nextAttackTime = Time.time + ItemTypes.GetAttackInterval(weapon);
                world.InteractionStatus = "Hit " + target.DisplayName + " with " + ItemTypes.GetName(weapon);
            }
            return;
        }

        if (!VoxelPhysics.TryGetTarget(
            playerCamera,
            playerTransform,
            out Vector3Int hitBlock,
            out Vector3Int adjacentBlock,
            out Vector3 hitPoint))
        {
            ResetBreaking();
            if (placePressed)
                world.InteractionStatus = "No block in reach";
            return;
        }

        if (breakHeld)
        {
            int blockType = world.GetBlock(hitBlock);
            if (blockType == 0)
            {
                ResetBreaking();
                return;
            }

            int dropType = blockType == ItemTypes.SilverOre ? ItemTypes.SilverCoin : blockType;
            int dropAmount = blockType == ItemTypes.SilverOre ? 3 : 1;
            if (!playerInventory.CanAdd(dropType, dropAmount))
            {
                ResetBreaking();
                world.InteractionStatus = "Inventory is full";
                return;
            }

            if (breakingBlock != hitBlock)
            {
                breakingBlock = hitBlock;
                breakProgress = 0f;
            }

            breakHitPoint = hitPoint;
            float breakDuration = blockType == ItemTypes.Stone ? 1.35f : blockType == ItemTypes.Log ? 0.9f : 0.7f;
            breakDuration /= ItemTypes.GetMiningSpeedMultiplier(playerInventory.SelectedBlockType, blockType);
            breakProgress = Mathf.Min(1f, breakProgress + Time.deltaTime / breakDuration);
            world.InteractionStatus = "Breaking " + GetBlockName(blockType);
            if (breakProgress >= 1f)
            {
                if (world.SetBlock(hitBlock, 0) && playerInventory.Add(dropType, dropAmount))
                {
                    world.AwardExperience(1f);
                    world.InteractionStatus = blockType == ItemTypes.SilverOre
                        ? "Collected 3 silver"
                        : "Collected " + GetBlockName(blockType);
                }
                else
                    world.InteractionStatus = "Cannot break outside the world";
                ResetBreaking();
            }
            return;
        }

        ResetBreaking();
        if (!placePressed)
            return;

        if (VoxelPhysics.BlockIntersectsPlayer(adjacentBlock, playerTransform.GetComponent<CharacterController>()))
        {
            world.InteractionStatus = "Cannot place a block inside yourself";
            return;
        }

        int selectedBlock = playerInventory.SelectedBlockType;
        if (!ItemTypes.IsBlock(selectedBlock))
        {
            world.InteractionStatus = selectedBlock == 0
                ? "Select a block from your hotbar"
                : "Cannot place " + ItemTypes.GetName(selectedBlock);
            return;
        }

        if (!playerInventory.TryUseSelected())
        {
            world.InteractionStatus = "No " + GetBlockName(selectedBlock) + " in inventory";
            return;
        }

        if (world.SetBlock(adjacentBlock, selectedBlock))
            world.InteractionStatus = "Placed " + GetBlockName(selectedBlock);
        else
        {
            playerInventory.Add(selectedBlock);
            world.InteractionStatus = "Cannot place outside the world";
        }
    }

    private void OnGUI()
    {
        if (!world.IsPaused && breakingBlock.HasValue && breakProgress > 0f)
            DrawBreakingEffect();
    }

    private void UseSelectedConsumable()
    {
        int selectedItem = playerInventory.SelectedBlockType;
        player playerController = playerTransform.GetComponent<player>();
        if (selectedItem == ItemTypes.CookedMeat)
        {
            world.InteractionStatus = playerController.TryEatCookedMeat()
                ? "Ate cooked meat"
                : "Hunger is already full";
            return;
        }

        if (selectedItem == ItemTypes.RegenerationPotion)
        {
            if (playerController.TryDrinkRegenerationPotion())
                world.InteractionStatus = "Regeneration is active";
            return;
        }

        if (selectedItem == ItemTypes.SplashPotionReverse &&
            playerInventory.TryUseSelected())
        {
            int affected = world.ApplyReversePotion(playerTransform.position, 7f, 6f);
            world.InteractionStatus = affected > 0
                ? "Nearby enemies are fleeing"
                : "The potion splashed, but no enemies were nearby";
        }
    }

    private void DrawBreakingEffect()
    {
        Vector3 screenPoint = playerCamera.WorldToScreenPoint(breakHitPoint);
        if (screenPoint.z <= 0f)
            return;

        float centerX = screenPoint.x;
        float centerY = Screen.height - screenPoint.y;
        Vector4[] cracks =
        {
            new Vector4(-0.02f, 0f, -0.22f, -0.2f),
            new Vector4(-0.22f, -0.2f, -0.31f, -0.36f),
            new Vector4(-0.22f, -0.2f, -0.38f, -0.13f),
            new Vector4(-0.02f, 0f, 0.13f, -0.22f),
            new Vector4(0.13f, -0.22f, 0.34f, -0.28f),
            new Vector4(0.13f, -0.22f, 0.09f, -0.42f),
            new Vector4(-0.02f, 0f, -0.11f, 0.2f),
            new Vector4(-0.11f, 0.2f, -0.3f, 0.31f),
            new Vector4(-0.11f, 0.2f, -0.08f, 0.42f),
            new Vector4(-0.02f, 0f, 0.19f, 0.1f),
            new Vector4(0.19f, 0.1f, 0.33f, 0.03f),
            new Vector4(0.19f, 0.1f, 0.27f, 0.29f)
        };
        int visibleCracks = Mathf.CeilToInt(breakProgress * cracks.Length);
        for (int i = 0; i < visibleCracks; i++)
        {
            Vector4 crack = cracks[i];
            DrawCrackLine(
                centerX + crack.x * 64f,
                centerY + crack.y * 64f,
                centerX + crack.z * 64f,
                centerY + crack.w * 64f);
        }

        Rect progressBackground = new Rect(centerX - 27f, centerY + 25f, 54f, 5f);
        DrawRect(progressBackground, new Color(0f, 0f, 0f, 0.8f));
        DrawRect(new Rect(progressBackground.x, progressBackground.y,
            progressBackground.width * breakProgress, progressBackground.height),
            new Color(0.72f, 0.78f, 0.84f, 0.95f));
    }

    private static void DrawCrackLine(float startX, float startY, float endX, float endY)
    {
        float deltaX = endX - startX;
        float deltaY = endY - startY;
        float length = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        float angle = Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg;
        Vector2 midpoint = new Vector2((startX + endX) * 0.5f, (startY + endY) * 0.5f);
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUIUtility.RotateAroundPivot(angle, midpoint);
        GUI.color = new Color(0.025f, 0.025f, 0.035f, 0.95f);
        GUI.DrawTexture(new Rect(midpoint.x - length * 0.5f, midpoint.y - 1f, length, 2f), Texture2D.whiteTexture);
        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
    }

    private static void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void ResetBreaking()
    {
        breakingBlock = null;
        breakProgress = 0f;
    }

    private static string GetBlockName(int blockType)
    {
        return ItemTypes.GetName(blockType);
    }
}
