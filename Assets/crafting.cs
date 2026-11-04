using UnityEngine;

public sealed class crafting : MonoBehaviour
{
    private inventory playerInventory;
    private Vector2 recipeScrollPosition;

    public bool IsOpen { get; private set; }

    public void Initialize(inventory playerItems)
    {
        playerInventory = playerItems;
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

    private void OnGUI()
    {
        if (!IsOpen || playerInventory == null)
            return;

        float width = Mathf.Min(460f, Screen.width - 24f);
        float rowHeight = 82f;
        float height = Mathf.Min(88f + recipes.All.Count * rowHeight, Screen.height - 24f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.Box(panel, "");

        GUIStyle heading = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 32f), "CRAFTING", heading);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 40f, panel.width - 40f, 22f),
            "Combine collected blocks  |  C to close");

        Rect scrollViewport = new Rect(panel.x + 12f, panel.y + 68f, panel.width - 24f, panel.height - 80f);
        float contentWidth = scrollViewport.width - 18f;
        recipeScrollPosition = GUI.BeginScrollView(scrollViewport, recipeScrollPosition,
            new Rect(0f, 0f, contentWidth, recipes.All.Count * rowHeight));
        for (int i = 0; i < recipes.All.Count; i++)
        {
            CraftingRecipe recipe = recipes.All[i];
            float rowY = i * rowHeight;
            Rect row = new Rect(0f, rowY, contentWidth, rowHeight - 8f);
            GUI.Box(row, "");
            GUI.Label(new Rect(row.x + 10f, row.y + 6f, row.width - 120f, 22f),
                recipe.Name + " x" + recipe.OutputAmount);
            GUI.Label(new Rect(row.x + 10f, row.y + 30f, row.width - 120f, 36f),
                FormatIngredients(recipe));

            bool canCraft = recipes.CanCraft(playerInventory, recipe);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = canCraft;
            if (GUI.Button(new Rect(row.xMax - 98f, row.y + 15f, 86f, 34f), "Craft"))
                recipes.TryCraft(playerInventory, recipe);
            GUI.enabled = wasEnabled;
        }
        GUI.EndScrollView();
    }

    private static string FormatIngredients(CraftingRecipe recipe)
    {
        string result = "Requires: ";
        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            RecipeIngredient ingredient = recipe.Ingredients[i];
            if (i > 0)
                result += " + ";
            result += ingredient.Amount + " " + ItemTypes.GetName(ingredient.ItemType);
        }
        return result;
    }
}
