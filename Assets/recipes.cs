using System.Collections.Generic;

public static class ItemTypes
{
    public const int Grass = 1;
    public const int Dirt = 2;
    public const int Stone = 3;
    public const int Log = 4;
    public const int Planks = 5;
    public const int CraftingTable = 6;
    public const int Stick = 7;
    public const int WoodenSword = 8;
    public const int WoodenAxe = 9;
    public const int WoodenPickaxe = 10;
    public const int Torch = 11;
    public const int SilverOre = 12;
    public const int SilverCoin = 13;
    public const int Crossbow = 14;
    public const int SilverArrow = 15;
    public const int LeatherHelmet = 16;
    public const int LeatherChestplate = 17;
    public const int LeatherLeggings = 18;
    public const int LeatherBoots = 19;

    public static bool IsBlock(int itemType)
    {
        return itemType >= Grass && itemType <= Planks || itemType == CraftingTable ||
            itemType == Torch || itemType == SilverOre;
    }

    public static bool IsTool(int itemType)
    {
        return itemType == WoodenSword || itemType == WoodenAxe ||
            itemType == WoodenPickaxe || itemType == Crossbow;
    }

    public static float GetAttackDamage(int itemType)
    {
        switch (itemType)
        {
            case WoodenSword: return 4f;
            case WoodenAxe: return 7f;
            case WoodenPickaxe: return 2f;
            case Crossbow: return 12f;
            case SilverArrow: return 16f;
            default: return 1f;
        }
    }

    public static float GetAttackInterval(int itemType)
    {
        switch (itemType)
        {
            case WoodenSword: return 0.35f;
            case WoodenAxe: return 1.1f;
            case WoodenPickaxe: return 0.4f;
            case Crossbow: return 0.8f;
            default: return 0.8f;
        }
    }

    public static float GetMiningSpeedMultiplier(int itemType, int blockType)
    {
        if (itemType == WoodenAxe && blockType == Log)
            return 3f;
        if (itemType == WoodenPickaxe && blockType == Stone)
            return 4f;
        if (itemType == WoodenPickaxe && blockType == SilverOre)
            return 3f;
        if (itemType == WoodenPickaxe)
            return 1.4f;
        if (itemType == WoodenAxe)
            return 0.85f;
        if (itemType == WoodenSword)
            return 0.9f;
        return 1f;
    }

    public static string GetName(int itemType)
    {
        switch (itemType)
        {
            case Grass: return "Grass";
            case Dirt: return "Dirt";
            case Stone: return "Stone";
            case Log: return "Log";
            case Planks: return "Planks";
            case CraftingTable: return "Crafting table";
            case Stick: return "Stick";
            case WoodenSword: return "Wooden sword";
            case WoodenAxe: return "Wooden axe";
            case WoodenPickaxe: return "Wooden pickaxe";
            case Torch: return "Torch";
            case SilverOre: return "Silver ore";
            case SilverCoin: return "Silver";
            case Crossbow: return "Crossbow";
            case SilverArrow: return "Silver arrow";
            case LeatherHelmet: return "Leather helmet";
            case LeatherChestplate: return "Leather chestplate";
            case LeatherLeggings: return "Leather leggings";
            case LeatherBoots: return "Leather boots";
            default: return "Unknown item";
        }
    }
}

public sealed class RecipeIngredient
{
    public int ItemType { get; }
    public int Amount { get; }

    public RecipeIngredient(int itemType, int amount)
    {
        ItemType = itemType;
        Amount = amount;
    }
}

public sealed class CraftingRecipe
{
    public string Name { get; }
    public int OutputItem { get; }
    public int OutputAmount { get; }
    public IReadOnlyList<RecipeIngredient> Ingredients { get; }

    public CraftingRecipe(string name, int outputItem, int outputAmount, params RecipeIngredient[] ingredients)
    {
        Name = name;
        OutputItem = outputItem;
        OutputAmount = outputAmount;
        Ingredients = ingredients;
    }
}

public static class recipes
{
    private static readonly CraftingRecipe[] all =
    {
        new CraftingRecipe("Grass block", ItemTypes.Grass, 1,
            new RecipeIngredient(ItemTypes.Dirt, 1), new RecipeIngredient(ItemTypes.Stone, 1)),
        new CraftingRecipe("Dirt block", ItemTypes.Dirt, 1,
            new RecipeIngredient(ItemTypes.Grass, 2), new RecipeIngredient(ItemTypes.Stone, 1)),
        new CraftingRecipe("Stone block", ItemTypes.Stone, 1,
            new RecipeIngredient(ItemTypes.Grass, 1), new RecipeIngredient(ItemTypes.Dirt, 2)),
        new CraftingRecipe("Wood planks", ItemTypes.Planks, 4,
            new RecipeIngredient(ItemTypes.Log, 1)),
        new CraftingRecipe("Crafting table", ItemTypes.CraftingTable, 1,
            new RecipeIngredient(ItemTypes.Planks, 4)),
        new CraftingRecipe("Sticks", ItemTypes.Stick, 4,
            new RecipeIngredient(ItemTypes.Planks, 2)),
        new CraftingRecipe("Wooden sword", ItemTypes.WoodenSword, 1,
            new RecipeIngredient(ItemTypes.Planks, 2), new RecipeIngredient(ItemTypes.Stick, 1)),
        new CraftingRecipe("Wooden axe", ItemTypes.WoodenAxe, 1,
            new RecipeIngredient(ItemTypes.Planks, 3), new RecipeIngredient(ItemTypes.Stick, 2)),
        new CraftingRecipe("Wooden pickaxe", ItemTypes.WoodenPickaxe, 1,
            new RecipeIngredient(ItemTypes.Planks, 3), new RecipeIngredient(ItemTypes.Stick, 2)),
        new CraftingRecipe("Torches", ItemTypes.Torch, 4,
            new RecipeIngredient(ItemTypes.Planks, 1), new RecipeIngredient(ItemTypes.Stick, 1))
    };

    public static IReadOnlyList<CraftingRecipe> All => all;

    public static bool CanCraft(inventory playerInventory, CraftingRecipe recipe)
    {
        if (playerInventory == null || recipe == null || !playerInventory.CanAdd(recipe.OutputItem, recipe.OutputAmount))
            return false;

        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            if (playerInventory.CountItem(ingredient.ItemType) < ingredient.Amount)
                return false;
        }
        return true;
    }

    public static bool TryCraft(inventory playerInventory, CraftingRecipe recipe)
    {
        if (!CanCraft(playerInventory, recipe))
            return false;

        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            if (!playerInventory.TryUseItem(ingredient.ItemType, ingredient.Amount))
                return false;
        }

        return playerInventory.Add(recipe.OutputItem, recipe.OutputAmount);
    }
}
