using UnityEngine;

public sealed class respawn : MonoBehaviour
{
    private world world;
    private player player;
    private CharacterController controller;
    private Vector3 spawnPosition;
    private string deathMessage = "You fell out of the world";

    public bool IsDead { get; private set; }

    public void Initialize(world currentWorld, player playerController, Vector3 startingPosition)
    {
        world = currentWorld;
        player = playerController;
        controller = GetComponent<CharacterController>();
        spawnPosition = startingPosition;
    }

    public void Die()
    {
        Die("You fell out of the world");
    }

    public void Die(string message)
    {
        if (IsDead)
            return;

        IsDead = true;
        deathMessage = message;
        world.InteractionStatus = deathMessage;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RespawnPlayer()
    {
        controller.enabled = false;
        transform.position = spawnPosition;
        controller.enabled = true;
        player.ResetVerticalSpeed();
        player.RestoreHealth();
        world.OnPlayerRespawned();
        IsDead = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DrawDeathScreen()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = new Color(0.9f, 0.12f, 0.12f);
        GUI.Label(new Rect(0f, Screen.height * 0.36f, Screen.width, 56f), "YOU DIED", CenteredLabelStyle(38, Color.white));
        GUI.color = Color.white;
        GUI.Label(new Rect(0f, Screen.height * 0.46f, Screen.width, 32f), deathMessage, CenteredLabelStyle(18, Color.white));

        Rect respawnButton = new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.55f, 200f, 48f);
        if (GUI.Button(respawnButton, "RESPAWN"))
            RespawnPlayer();

        GUI.Label(new Rect(0f, Screen.height * 0.55f + 54f, Screen.width, 28f), "or press R", CenteredLabelStyle(14, Color.white));
        GUI.color = previousColor;
    }

    private static GUIStyle CenteredLabelStyle(int fontSize, Color textColor)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        return style;
    }
}
