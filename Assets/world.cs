using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class world : MonoBehaviour
{
    public const int ChunkSize = 16;
    public const int WorldHeight = 256;
    internal const int LegacyWorldHeight = 24;
    private const int ChunkViewDistance = 2;
    private const int MaximumZombies = 30;
    private const int InitialZombieCount = 7;
    private const int MaximumGhouls = 5;
    private const int MaximumWitches = 8;
    private const int DungeonZombieCount = 7;
    private const float ZombieSpawnInterval = 6f;
    private const float RedMoonCycle = 210f;
    private const float RedMoonDuration = 55f;
    private bool isPaused;

    private readonly Dictionary<Vector2Int, chunk> chunks = new Dictionary<Vector2Int, chunk>();
    private readonly List<hostileMob> mobs = new List<hostileMob>();
    private readonly HashSet<Vector2Int> activatedHuts = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> activatedDungeons = new HashSet<Vector2Int>();
    private readonly Material[] materials = new Material[6];
    private Texture2D[] healthIcons;
    private Texture2D[] hungerIcons;
    private GUIStyle experienceLevelStyle;
    private Material treeBarkMaterial;
    private Material treeLeafMaterial;
    private Material hangingBodyMaterial;
    private Light moonLight;
    private Transform playerTransform;
    private Camera playerCamera;
    private player playerController;
    private inventory playerInventory;
    private crafting playerCrafting;
    private blockBreaking blockBreakingController;
    private respawn respawnController;
    private trade trading;
    private GameAudio gameAudio;
    private string saveDirectory;
    private float autosaveTimer;
    private float zombieSpawnTimer;
    private float structureScanTimer;
    private Vector2Int playerChunk;
    private bool isRedMoon;
    private string interactionStatus = "Aim at a block to edit it";
    private string saveStatus = "World ready";

    public IReadOnlyDictionary<Vector2Int, chunk> Chunks => chunks;
    public Material[] Materials => materials;
    internal Material TreeBarkMaterial => treeBarkMaterial;
    internal Material TreeLeafMaterial => treeLeafMaterial;
    internal Material HangingBodyMaterial => hangingBodyMaterial;
    public Camera PlayerCamera => playerCamera;
    public Transform PlayerTransform => playerTransform;
    public bool IsPaused => isPaused;
    public bool IsRedMoon => isRedMoon;
    public trade Trading => trading;
    public string SaveStatus => saveStatus;
    public string InteractionStatus
    {
        get => interactionStatus;
        set => interactionStatus = value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartWorld()
    {
        if (FindAnyObjectByType<world>() != null)
            return;

        GameObject worldObject = new GameObject("Voxel World");
        DontDestroyOnLoad(worldObject);
        worldObject.AddComponent<world>();
    }

    private void Awake()
    {
        Camera sceneCamera = Camera.main;
        Light sceneLight = FindAnyObjectByType<Light>();

        if (sceneCamera != null)
            sceneCamera.transform.SetParent(null);

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (sceneCamera != null && root == sceneCamera.gameObject)
                continue;
            if (sceneLight != null && root == sceneLight.gameObject)
                continue;
            Destroy(root);
        }

        ConfigureLighting(sceneLight);
        CreateMaterials();
        CreateHudIcons();
        gameAudio = gameObject.AddComponent<GameAudio>();
        Time.timeScale = 1f;
        saveDirectory = Path.Combine(Application.persistentDataPath, "VoxelWorld", "Chunks");
        CreatePlayer(sceneCamera);
        blockBreakingController = gameObject.AddComponent<blockBreaking>();
        blockBreakingController.Initialize(this, playerCamera, playerTransform, playerInventory);
        playerChunk = GetPlayerChunk();
        UpdateChunkStreaming(playerChunk);
        respawnController.Initialize(this, playerController, playerTransform.position);
        trading = gameObject.AddComponent<trade>();
        trading.Initialize(playerInventory, playerTransform.GetComponent<armour>(), playerTransform);
        SpawnInitialZombies();
    }

    private void ConfigureLighting(Light sceneLight)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.075f, 0.09f, 0.14f);
        RenderSettings.ambientIntensity = 0.45f;
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.025f, 0.035f, 0.075f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 24f;
        RenderSettings.fogEndDistance = 78f;

        if (sceneLight == null)
        {
            GameObject lightObject = new GameObject("Moonlight");
            sceneLight = lightObject.AddComponent<Light>();
        }

        sceneLight.type = LightType.Directional;
        moonLight = sceneLight;
        sceneLight.color = new Color(0.38f, 0.48f, 0.8f);
        sceneLight.intensity = 0.3f;
        sceneLight.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    private void CreateMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        materials[0] = CreateMaterial(shader, "Grass", new Color(0.27f, 0.63f, 0.22f));
        materials[1] = CreateMaterial(shader, "Dirt", new Color(0.43f, 0.29f, 0.17f));
        materials[2] = CreateMaterial(shader, "Stone", new Color(0.48f, 0.49f, 0.5f));
        materials[3] = CreateMaterial(shader, "Wood", new Color(0.12f, 0.075f, 0.055f));
        materials[4] = CreateMaterial(shader, "Torch", new Color(1f, 0.48f, 0.08f));
        materials[5] = CreateMaterial(shader, "Silver ore", new Color(0.78f, 0.84f, 0.92f));
        if (materials[4].HasProperty("_EmissionColor"))
        {
            materials[4].EnableKeyword("_EMISSION");
            materials[4].SetColor("_EmissionColor", new Color(1f, 0.32f, 0.04f) * 2f);
        }
        treeBarkMaterial = CreateMaterial(shader, "Dark Tree Bark", new Color(0.12f, 0.075f, 0.055f));
        treeLeafMaterial = CreateMaterial(shader, "Night Leaves", new Color(0.045f, 0.09f, 0.055f));
        hangingBodyMaterial = CreateMaterial(shader, "Hanging Figure", new Color(0.12f, 0.11f, 0.13f));
        WorldTextures.Apply(materials[0], "grass");
        WorldTextures.Apply(materials[1], "dirt");
        WorldTextures.Apply(materials[2], "stone");
        WorldTextures.Apply(materials[3], "wood");
        WorldTextures.Apply(materials[4], "torch");
        WorldTextures.Apply(materials[5], "silver");
    }

    private static Material CreateMaterial(Shader shader, string materialName, Color color)
    {
        Material material = new Material(shader) { name = materialName };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    private void CreatePlayer(Camera sceneCamera)
    {
        playerTransform = new GameObject("Player").transform;
        playerTransform.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        playerTransform.position = new Vector3(0f, GetHeight(0, 0) + 2f, 0f);

        CharacterController controller = playerTransform.gameObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.35f;
        playerController = playerTransform.gameObject.AddComponent<player>();
        playerInventory = playerTransform.gameObject.AddComponent<inventory>();
        playerCrafting = playerTransform.gameObject.AddComponent<crafting>();
        respawnController = playerTransform.gameObject.AddComponent<respawn>();
        playerTransform.gameObject.AddComponent<armour>();
        playerCrafting.Initialize(playerInventory);

        if (sceneCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            sceneCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            sceneCamera.fieldOfView = 75f;
        }

        playerCamera = sceneCamera;
        playerCamera.transform.SetParent(playerTransform);
        playerCamera.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        playerCamera.transform.localRotation = Quaternion.identity;
        playerCamera.clearFlags = CameraClearFlags.SolidColor;
        playerCamera.backgroundColor = new Color(0.015f, 0.02f, 0.045f);
        playerCamera.nearClipPlane = 0.05f;
        playerCamera.farClipPlane = 180f;
        playerCamera.fieldOfView = 75f;
        playerController.Initialize(this, respawnController, controller, playerCamera);
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        if (!respawnController.IsDead && playerTransform.position.y < -10f)
            respawnController.Die();

        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            SaveDirtyChunks();

        Vector2Int currentPlayerChunk = GetPlayerChunk();
        if (currentPlayerChunk != playerChunk)
        {
            playerChunk = currentPlayerChunk;
            UpdateChunkStreaming(playerChunk);
        }

        autosaveTimer += Time.unscaledDeltaTime;
        if (autosaveTimer >= 30f)
        {
            autosaveTimer = 0f;
            SaveDirtyChunks();
        }

        if (respawnController.IsDead || isPaused)
            return;

        UpdateMoonCycle();
        mobs.RemoveAll(mob => mob == null);
        structureScanTimer -= Time.deltaTime;
        if (structureScanTimer <= 0f)
        {
            structureScanTimer = 1f;
            SpawnNearbyStructures();
        }

        zombieSpawnTimer += Time.deltaTime;
        if (zombieSpawnTimer >= ZombieSpawnInterval)
        {
            zombieSpawnTimer = 0f;
            if (CountMobs<zombies>() < MaximumZombies)
                SpawnZombie();
        }

        if (isRedMoon && CountMobs<ghouls>() < MaximumGhouls &&
            UnityEngine.Random.value < Time.deltaTime / 12f)
            SpawnGhoul();
    }

    private void SpawnInitialZombies()
    {
        for (int i = 0; i < InitialZombieCount; i++)
            SpawnZombie();
    }

    private void SpawnZombie()
    {
        if (CountMobs<zombies>() >= MaximumZombies)
            return;

        for (int attempt = 0; attempt < 24; attempt++)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = UnityEngine.Random.Range(10f, 17f);
            int x = Mathf.RoundToInt(playerTransform.position.x + Mathf.Cos(angle) * distance);
            int z = Mathf.RoundToInt(playerTransform.position.z + Mathf.Sin(angle) * distance);
            int height = GetHeight(x, z);
            Vector3 position = new Vector3(x, height + 0.55f, z);

            if (GetBlock(new Vector3Int(x, height, z)) == 0 ||
                GetBlock(new Vector3Int(x, height + 1, z)) != 0 ||
                Mathf.Abs(height - playerTransform.position.y) > 5f)
                continue;

            CreateMob<zombies>("Zombie", position, false);
            return;
        }
    }

    private T CreateMob<T>(string mobName, Vector3 position, bool flying) where T : hostileMob
    {
        GameObject mobObject = new GameObject(mobName);
        mobObject.transform.SetParent(transform, false);
        mobObject.transform.position = position;
        CharacterController controller = mobObject.AddComponent<CharacterController>();
        controller.height = flying ? 1.1f : 1.8f;
        controller.radius = flying ? 0.42f : 0.32f;
        controller.center = flying ? Vector3.zero : new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = flying ? 0f : 0.45f;
        T mob = mobObject.AddComponent<T>();
        mob.Initialize(this, playerController);
        mobs.Add(mob);
        return mob;
    }

    private int CountMobs<T>() where T : hostileMob
    {
        int count = 0;
        for (int i = 0; i < mobs.Count; i++)
        {
            if (mobs[i] is T)
                count++;
        }
        return count;
    }

    private void SpawnGhoul()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 position = playerTransform.position +
            new Vector3(Mathf.Cos(angle) * 14f, UnityEngine.Random.Range(4f, 8f), Mathf.Sin(angle) * 14f);
        CreateMob<ghouls>("Ghoul", position, true);
    }

    private void SpawnNearbyStructures()
    {
        foreach (Vector3 location in WorldStructures.GetNearbyWitchHuts(playerTransform.position, 2))
        {
            Vector2Int key = new Vector2Int(Mathf.RoundToInt(location.x), Mathf.RoundToInt(location.z));
            if (activatedHuts.Contains(key) || Vector3.Distance(playerTransform.position, location) > 42f ||
                CountMobs<witches>() >= MaximumWitches)
                continue;

            CreateMob<witches>("Witch", location + Vector3.right * 1.5f, false);
            activatedHuts.Add(key);
        }

        foreach (Vector3 location in WorldStructures.GetNearbyDungeons(playerTransform.position, 2))
        {
            Vector2Int key = new Vector2Int(Mathf.RoundToInt(location.x), Mathf.RoundToInt(location.z));
            if (activatedDungeons.Contains(key) ||
                Vector3.Distance(playerTransform.position, location) > 38f ||
                CountMobs<zombies>() + DungeonZombieCount > MaximumZombies)
                continue;

            for (int i = 0; i < DungeonZombieCount; i++)
            {
                Vector3 offset = new Vector3((i % 3 - 1) * 1.3f, 0f, (i / 3 - 1) * 1.3f);
                CreateMob<zombies>("Dungeon zombie", location + offset, false);
            }
            activatedDungeons.Add(key);
        }
    }

    private void UpdateMoonCycle()
    {
        float cycleTime = Time.timeSinceLevelLoad % RedMoonCycle;
        bool redMoon = cycleTime >= RedMoonCycle - RedMoonDuration;
        if (redMoon == isRedMoon)
            return;

        isRedMoon = redMoon;
        if (moonLight != null)
            moonLight.color = isRedMoon ? new Color(0.9f, 0.16f, 0.18f) : new Color(0.38f, 0.48f, 0.8f);
        RenderSettings.ambientLight = isRedMoon
            ? new Color(0.2f, 0.045f, 0.065f)
            : new Color(0.075f, 0.09f, 0.14f);
        if (gameAudio != null)
            gameAudio.SetRedMoon(isRedMoon);
        InteractionStatus = isRedMoon
            ? "A red moon rises. Ghouls are hunting."
            : "The red moon has passed.";
    }

    public void OnMobKilled(hostileMob mob)
    {
        if (mob is zombies && UnityEngine.Random.value < 0.35f &&
            playerInventory.CanAdd(ItemTypes.SilverCoin))
            playerInventory.Add(ItemTypes.SilverCoin);
        if (mob is ghouls && playerInventory.CanAdd(ItemTypes.SilverCoin))
            playerInventory.Add(ItemTypes.SilverCoin, 2);
    }

    private Vector2Int GetPlayerChunk()
    {
        return new Vector2Int(
            Mathf.FloorToInt(playerTransform.position.x / ChunkSize),
            Mathf.FloorToInt(playerTransform.position.z / ChunkSize));
    }

    private void UpdateChunkStreaming(Vector2Int center)
    {
        List<chunk> createdChunks = new List<chunk>();
        for (int x = center.x - ChunkViewDistance; x <= center.x + ChunkViewDistance; x++)
        {
            for (int z = center.y - ChunkViewDistance; z <= center.y + ChunkViewDistance; z++)
            {
                Vector2Int coordinate = new Vector2Int(x, z);
                if (chunks.ContainsKey(coordinate))
                    continue;

                chunk newChunk = new chunk(this, coordinate);
                chunks.Add(coordinate, newChunk);
                createdChunks.Add(newChunk);
            }
        }

        HashSet<Vector2Int> chunksToRebuild = new HashSet<Vector2Int>();
        foreach (chunk newChunk in createdChunks)
        {
            chunksToRebuild.Add(newChunk.Coordinate);
            AddNeighborCoordinates(chunksToRebuild, newChunk.Coordinate);
        }

        List<Vector2Int> chunksToUnload = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, chunk> entry in chunks)
        {
            if (Mathf.Abs(entry.Key.x - center.x) > ChunkViewDistance ||
                Mathf.Abs(entry.Key.y - center.y) > ChunkViewDistance)
                chunksToUnload.Add(entry.Key);
        }

        foreach (Vector2Int coordinate in chunksToUnload)
        {
            chunk oldChunk = chunks[coordinate];
            if (!SaveChunk(oldChunk))
                continue;

            chunks.Remove(coordinate);
            oldChunk.Destroy();
            AddNeighborCoordinates(chunksToRebuild, coordinate);
        }

        foreach (Vector2Int coordinate in chunksToRebuild)
            RefreshChunk(coordinate);

        UnityEngine.Physics.SyncTransforms();
    }

    private static void AddNeighborCoordinates(HashSet<Vector2Int> coordinates, Vector2Int coordinate)
    {
        coordinates.Add(new Vector2Int(coordinate.x - 1, coordinate.y));
        coordinates.Add(new Vector2Int(coordinate.x + 1, coordinate.y));
        coordinates.Add(new Vector2Int(coordinate.x, coordinate.y - 1));
        coordinates.Add(new Vector2Int(coordinate.x, coordinate.y + 1));
    }

    private string GetChunkSavePath(Vector2Int coordinate)
    {
        return Path.Combine(saveDirectory, "chunk_" + coordinate.x + "_" + coordinate.y + ".json");
    }

    internal int[] LoadChunkBlocks(Vector2Int coordinate)
    {
        string path = GetChunkSavePath(coordinate);
        if (!File.Exists(path))
            return null;

        try
        {
            ChunkSaveData data = JsonUtility.FromJson<ChunkSaveData>(File.ReadAllText(path));
            int currentBlockCount = ChunkSize * WorldHeight * ChunkSize;
            int legacyBlockCount = ChunkSize * LegacyWorldHeight * ChunkSize;
            if (data == null || data.version != 1 || data.blocks == null ||
                (data.blocks.Length != currentBlockCount && data.blocks.Length != legacyBlockCount))
            {
                Debug.LogError("Saved chunk has an unsupported or invalid format: " + path);
                saveStatus = "Could not load saved chunk " + coordinate;
                return null;
            }

            for (int i = 0; i < data.blocks.Length; i++)
            {
                if (data.blocks[i] != 0 && !ItemTypes.IsBlock(data.blocks[i]))
                {
                    Debug.LogError("Saved chunk contains an invalid block type: " + path);
                    saveStatus = "Could not load saved chunk " + coordinate;
                    return null;
                }
            }

            return data.blocks;
        }
        catch (IOException exception)
        {
            Debug.LogError("Could not read saved chunk '" + path + "': " + exception.Message);
            saveStatus = "Could not load saved chunk " + coordinate;
            return null;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError("Could not read saved chunk '" + path + "': " + exception.Message);
            saveStatus = "Could not load saved chunk " + coordinate;
            return null;
        }
        catch (ArgumentException exception)
        {
            Debug.LogError("Saved chunk is not valid JSON '" + path + "': " + exception.Message);
            saveStatus = "Could not load saved chunk " + coordinate;
            return null;
        }
    }

    private bool SaveChunk(chunk changedChunk)
    {
        if (!changedChunk.IsDirty)
            return true;

        string path = GetChunkSavePath(changedChunk.Coordinate);
        try
        {
            Directory.CreateDirectory(saveDirectory);
            ChunkSaveData data = new ChunkSaveData
            {
                version = 1,
                blocks = changedChunk.CopyBlocks()
            };
            File.WriteAllText(path, JsonUtility.ToJson(data));
            changedChunk.MarkSaved();
            saveStatus = "Saved chunk " + changedChunk.Coordinate.x + ", " + changedChunk.Coordinate.y;
            return true;
        }
        catch (IOException exception)
        {
            Debug.LogError("Could not save chunk '" + path + "': " + exception.Message);
            saveStatus = "Save failed for chunk " + changedChunk.Coordinate;
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError("Could not save chunk '" + path + "': " + exception.Message);
            saveStatus = "Save failed for chunk " + changedChunk.Coordinate;
            return false;
        }
    }

    private void SaveDirtyChunks()
    {
        int savedCount = 0;
        bool allSaved = true;
        foreach (chunk currentChunk in chunks.Values)
        {
            if (!currentChunk.IsDirty)
                continue;
            if (SaveChunk(currentChunk))
                savedCount++;
            else
                allSaved = false;
        }

        if (allSaved)
            saveStatus = savedCount == 0 ? "World is already saved" : "Saved " + savedCount + " chunks";
    }

    private void OnApplicationQuit()
    {
        SaveDirtyChunks();
    }

    private void OnDestroy()
    {
        if (isPaused)
            Time.timeScale = 1f;

        DestroyHudIcons(healthIcons);
        DestroyHudIcons(hungerIcons);

        foreach (Material material in materials)
        {
            if (material != null)
                Destroy(material);
        }

        if (treeBarkMaterial != null) Destroy(treeBarkMaterial);
        if (treeLeafMaterial != null) Destroy(treeLeafMaterial);
        if (hangingBodyMaterial != null) Destroy(hangingBodyMaterial);
    }

    private static void DestroyHudIcons(Texture2D[] icons)
    {
        if (icons == null)
            return;

        foreach (Texture2D icon in icons)
        {
            if (icon != null)
                Destroy(icon);
        }
    }

    internal int GetHeight(int x, int z)
    {
        float broad = Mathf.PerlinNoise((x + 720f) * 0.035f, (z + 720f) * 0.035f);
        float detail = Mathf.PerlinNoise((x + 180f) * 0.11f, (z + 180f) * 0.11f);
        return Mathf.Clamp(5 + Mathf.RoundToInt(broad * 5f + detail * 2f), 4, WorldHeight - 1);
    }

    internal bool SetBlock(Vector3Int position, int blockType)
    {
        Vector2Int coordinate = new Vector2Int(
            Mathf.FloorToInt((float)position.x / ChunkSize),
            Mathf.FloorToInt((float)position.z / ChunkSize));

        if (!chunks.TryGetValue(coordinate, out chunk currentChunk))
            return false;

        if (!currentChunk.SetBlock(
            position.x - coordinate.x * ChunkSize,
            position.y,
            position.z - coordinate.y * ChunkSize,
            blockType))
            return false;

        int localX = position.x - coordinate.x * ChunkSize;
        int localZ = position.z - coordinate.y * ChunkSize;
        RefreshChunk(coordinate);
        if (localX == 0)
            RefreshChunk(new Vector2Int(coordinate.x - 1, coordinate.y));
        if (localX == ChunkSize - 1)
            RefreshChunk(new Vector2Int(coordinate.x + 1, coordinate.y));
        if (localZ == 0)
            RefreshChunk(new Vector2Int(coordinate.x, coordinate.y - 1));
        if (localZ == ChunkSize - 1)
            RefreshChunk(new Vector2Int(coordinate.x, coordinate.y + 1));
        UnityEngine.Physics.SyncTransforms();
        return true;
    }

    private void RefreshChunk(Vector2Int coordinate)
    {
        if (chunks.TryGetValue(coordinate, out chunk currentChunk))
            currentChunk.BuildMesh();
    }

    internal void HandleBlockInput()
    {
        blockBreakingController.HandleInput();
    }

    internal void AwardExperience(float amount)
    {
        playerController.AddExperience(amount);
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    internal int GetBlock(Vector3Int position)
    {
        Vector2Int coordinate = new Vector2Int(Mathf.FloorToInt((float)position.x / ChunkSize), Mathf.FloorToInt((float)position.z / ChunkSize));
        return chunks.TryGetValue(coordinate, out chunk currentChunk)
            ? currentChunk.GetBlock(position.x - coordinate.x * ChunkSize, position.y, position.z - coordinate.y * ChunkSize) : 0;
    }

    private void OnGUI()
    {
        if (respawnController != null && respawnController.IsDead)
        {
            respawnController.DrawDeathScreen();
            return;
        }

        if (isPaused)
        {
            DrawPauseMenu();
            return;
        }

        if ((playerInventory != null && playerInventory.IsOpen) ||
            (playerCrafting != null && playerCrafting.IsOpen))
            return;

        GUI.color = Color.white;
        GUI.Box(new Rect(16f, 16f, 360f, 168f), "");
        GUI.Label(new Rect(28f, 24f, 280f, 22f), "VOXEL WORLD  |  " + chunks.Count + " loaded chunks");
        GUI.Label(new Rect(28f, 46f, 260f, 22f), "WASD move  |  Space jump  |  Mouse look");
        GUI.Label(new Rect(28f, 68f, 280f, 22f), "Left click break  |  Right click place  |  Esc pause");
        GUI.Label(new Rect(28f, 88f, 280f, 22f), "Chunks stream as you walk  |  F5 save");
        GUI.Label(new Rect(28f, 108f, 330f, 22f), "1-0 hotbar  |  I inventory  |  C crafting");
        GUI.Label(new Rect(28f, 130f, 330f, 22f), "B trade near camp  |  O equip armour");
        GUI.Label(new Rect(28f, 150f, 330f, 22f),
            isRedMoon ? "RED MOON  |  " + CountMobs<ghouls>() + " ghouls" : "Mobs nearby: " + mobs.Count);
        GUI.Label(new Rect(28f, 130f, 310f, 22f), saveStatus + "  |  " + interactionStatus);

        DrawSurvivalHud();

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 7f, 2f, 14f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 7f, centerY - 1f, 14f, 2f), Texture2D.whiteTexture);
    }

    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DrawPauseMenu()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = Mathf.Min(360f, Screen.width - 32f);
        float height = 250f;
        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);
        GUI.Box(panel, "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(panel.x + 16f, panel.y + 24f, panel.width - 32f, 46f), "PAUSED", titleStyle);

        float buttonWidth = Mathf.Min(240f, panel.width - 48f);
        float buttonLeft = panel.x + (panel.width - buttonWidth) * 0.5f;
        if (GUI.Button(new Rect(buttonLeft, panel.y + 100f, buttonWidth, 48f), "Resume"))
            ResumeGame();
        if (GUI.Button(new Rect(buttonLeft, panel.y + 164f, buttonWidth, 48f), "Quit"))
            QuitGame();

        GUI.color = previousColor;
    }

    private void QuitGame()
    {
        SaveDirtyChunks();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CreateHudIcons()
    {
        string[] heartShape =
        {
            "01100110",
            "11111111",
            "11111111",
            "11111111",
            "01111110",
            "00111100",
            "00011000",
            "00000000"
        };
        string[] hungerShape =
        {
            "00011000",
            "00111100",
            "01111110",
            "01111111",
            "00111111",
            "00011110",
            "00001100",
            "00011000"
        };
        healthIcons = CreateHudIconSet(
            heartShape,
            new Color(0.9f, 0.16f, 0.2f),
            new Color(0.24f, 0.12f, 0.13f),
            new Color(0.2f, 0.035f, 0.045f));
        hungerIcons = CreateHudIconSet(
            hungerShape,
            new Color(0.91f, 0.62f, 0.19f),
            new Color(0.3f, 0.21f, 0.12f),
            new Color(0.16f, 0.1f, 0.055f));
    }

    private static Texture2D[] CreateHudIconSet(string[] shape, Color filled, Color empty, Color outline)
    {
        Texture2D[] icons = new Texture2D[3];
        for (int state = 0; state < icons.Length; state++)
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
            {
                name = "HUD icon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < shape.Length; y++)
            {
                for (int x = 0; x < shape[y].Length; x++)
                {
                    if (shape[y][x] != '1')
                        continue;

                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            int pixelX = x + offsetX;
                            int pixelY = y + offsetY;
                            if (pixelX < 0 || pixelX >= 8 || pixelY < 0 || pixelY >= 8)
                                continue;

                            bool insideShape = shape[pixelY][pixelX] == '1';
                            if (!insideShape)
                                texture.SetPixel(pixelX, 7 - pixelY, outline);
                            else
                            {
                                bool isFilled = state == 2 || (state == 1 && pixelX < 4);
                                texture.SetPixel(
                                    pixelX,
                                    7 - pixelY,
                                    isFilled ? filled : empty);
                            }
                        }
                    }
                }
            }

            texture.Apply(false, true);
            icons[state] = texture;
        }

        return icons;
    }

    private void DrawSurvivalHud()
    {
        if (playerController == null)
            return;

        float iconSize = Mathf.Clamp(Screen.height / 90f, 10f, 14f);
        float iconStep = iconSize + 2f;
        float iconRowWidth = 10f * iconStep - 2f;
        float centerX = Screen.width * 0.5f;
        float hotbarSize = Mathf.Min(
            Mathf.Clamp(Screen.height * 0.075f, 30f, 68f),
            Mathf.Max(18f, (Screen.width - 32f - (inventory.HotbarSlotCount - 1) * 4f) /
                (inventory.HotbarSlotCount + (inventory.HotbarSlotCount - 1) * 0.08f)));
        float hotbarGap = Mathf.Max(4f, hotbarSize * 0.08f);
        float hotbarWidth = inventory.HotbarSlotCount * hotbarSize +
            (inventory.HotbarSlotCount - 1) * hotbarGap;
        float hotbarTop = Screen.height - hotbarSize - 22f;
        float iconsY = hotbarTop - iconSize - 16f;

        DrawStatusIcons(
            healthIcons,
            playerController.Health,
            centerX - iconRowWidth - 13f,
            iconsY,
            iconSize,
            iconStep);
        DrawStatusIcons(
            hungerIcons,
            playerController.Hunger,
            centerX + 13f,
            iconsY,
            iconSize,
            iconStep);

        float barHeight = 7f;
        Rect experienceBar = new Rect(
            centerX - hotbarWidth * 0.5f,
            hotbarTop - 12f,
            hotbarWidth,
            barHeight);
        GUI.color = new Color(0f, 0f, 0f, 0.9f);
        GUI.DrawTexture(
            new Rect(experienceBar.x - 2f, experienceBar.y - 2f,
                experienceBar.width + 4f, experienceBar.height + 4f),
            Texture2D.whiteTexture);
        GUI.color = new Color(0.12f, 0.15f, 0.1f, 1f);
        GUI.DrawTexture(experienceBar, Texture2D.whiteTexture);
        GUI.color = new Color(0.43f, 0.83f, 0.18f, 1f);
        GUI.DrawTexture(
            new Rect(experienceBar.x, experienceBar.y,
                experienceBar.width * Mathf.Clamp01(playerController.ExperienceProgress),
                experienceBar.height),
            Texture2D.whiteTexture);
        DrawExperienceLevel(centerX, experienceBar.y - 17f, playerController.ExperienceLevel);
        GUI.color = Color.white;
    }

    private static void DrawStatusIcons(Texture2D[] icons, float value, float x, float y, float size, float step)
    {
        for (int i = 0; i < 10; i++)
        {
            float iconValue = value - i * 2f;
            int state = iconValue >= 2f ? 2 : iconValue > 0f ? 1 : 0;
            GUI.DrawTexture(new Rect(x + i * step, y, size, size), icons[state]);
        }
    }

    private void DrawExperienceLevel(float centerX, float y, int level)
    {
        if (experienceLevelStyle == null)
        {
            experienceLevelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
        }

        experienceLevelStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(centerX - 18f, y + 1f, 36f, 18f), level.ToString(), experienceLevelStyle);
        experienceLevelStyle.normal.textColor = new Color(0.62f, 1f, 0.32f);
        GUI.Label(new Rect(centerX - 18f, y, 36f, 18f), level.ToString(), experienceLevelStyle);
    }

    internal void OnPlayerRespawned()
    {
        playerChunk = GetPlayerChunk();
        UpdateChunkStreaming(playerChunk);
        UnityEngine.Physics.SyncTransforms();
        interactionStatus = "Aim at a block to edit it";
    }
}
