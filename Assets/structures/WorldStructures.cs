using System.Collections.Generic;
using UnityEngine;

public static class WorldStructures
{
    private const int WitchHutSpacing = 17;
    private const int DungeonSpacing = 19;
    private const int LegacyGraveyardSpacing = 13;
    private static Material graveMarkerMaterial;
    private static Material dungeonChestMaterial;
    private static Material dungeonChestMetalMaterial;
    private static Material dungeonCrossbowMaterial;

    public static bool GenerateChunk(
        world currentWorld,
        Vector2Int coordinate,
        int[,,] blocks,
        bool preserveExistingBlocks = false)
    {
        bool changed = false;
        for (int x = coordinate.x - 1; x <= coordinate.x + 1; x++)
        {
            for (int z = coordinate.y - 3; z <= coordinate.y + 1; z++)
            {
                if (preserveExistingBlocks &&
                    TryGetLegacyGraveyardLocation(x, z, out Vector2Int oldGraveyard))
                {
                    int oldGroundY = currentWorld.GetHeight(oldGraveyard.x, oldGraveyard.y);
                    changed |= ClearLegacyCemetery(
                        coordinate,
                        blocks,
                        oldGraveyard,
                        oldGroundY);
                }
                if (TryGetLocation(x, z, WitchHutSpacing, 0x4a17, true, out Vector2Int hut))
                    changed |= BuildHut(currentWorld, coordinate, blocks, hut, preserveExistingBlocks);
                if (TryGetLocation(x, z, DungeonSpacing, 0x61d3, false, out Vector2Int dungeon))
                    changed |= BuildDungeon(currentWorld, coordinate, blocks, dungeon, preserveExistingBlocks);
            }
        }
        return changed;
    }

    public static List<Vector3> GetNearbyWitchHuts(Vector3 position, int radiusChunks)
    {
        List<Vector3> found = new List<Vector3>();
        CollectNearbyWitchHuts(position, radiusChunks, found);
        return found;
    }

    public static void CollectNearbyWitchHuts(Vector3 position, int radiusChunks, List<Vector3> found)
    {
        FindNearby(position, radiusChunks, WitchHutSpacing, 0x4a17, true, found);
    }

    public static List<Vector3> GetNearbyDungeons(Vector3 position, int radiusChunks)
    {
        List<Vector3> found = new List<Vector3>();
        CollectNearbyDungeons(position, radiusChunks, found);
        return found;
    }

    public static void CollectNearbyDungeons(Vector3 position, int radiusChunks, List<Vector3> found)
    {
        FindNearby(position, radiusChunks, DungeonSpacing, 0x61d3, false, found);
    }

    public static void CreateChunkGraveModels(
        world currentWorld,
        Vector2Int coordinate,
        Transform parent)
    {
        int chunkHash = unchecked(coordinate.x * 73856093 ^ coordinate.y * 19349663 ^ 0x319d);
        System.Random random = new System.Random(chunkHash);
        int graveCount = random.Next(2, 4);
        for (int i = 0; i < graveCount; i++)
        {
            bool created = false;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int worldX = coordinate.x * world.ChunkSize + random.Next(2, world.ChunkSize - 2);
                int worldZ = coordinate.y * world.ChunkSize + random.Next(2, world.ChunkSize - 2);
                if (NearStructure(worldX, worldZ))
                    continue;

                int groundY = currentWorld.GetHeight(worldX, worldZ);
                float yaw = random.Next(0, 4) * 90f;
                CreateGraveMarker(
                    parent,
                    new Vector3(
                        worldX - coordinate.x * world.ChunkSize + 0.5f,
                        groundY + 1.02f,
                        worldZ - coordinate.y * world.ChunkSize + 0.5f),
                    yaw);
                created = true;
                break;
            }
            if (!created)
                break;
        }
        CreateDungeonEntranceDecorations(currentWorld, coordinate, parent);
    }

    private static bool NearStructure(int worldX, int worldZ)
    {
        int chunkX = Mathf.FloorToInt((float)worldX / world.ChunkSize);
        int chunkZ = Mathf.FloorToInt((float)worldZ / world.ChunkSize);
        for (int x = chunkX - 1; x <= chunkX + 1; x++)
        {
            for (int z = chunkZ - 1; z <= chunkZ + 1; z++)
            {
                if (TryGetLocation(x, z, WitchHutSpacing, 0x4a17, true, out Vector2Int hut) &&
                    (new Vector2(worldX - hut.x, worldZ - hut.y)).sqrMagnitude < 100f)
                    return true;
                if (TryGetLocation(x, z, DungeonSpacing, 0x61d3, false, out Vector2Int dungeon) &&
                    (new Vector2(worldX - dungeon.x, worldZ - dungeon.y)).sqrMagnitude < 144f)
                    return true;
            }
        }
        return false;
    }

    private static void CreateGraveMarker(Transform parent, Vector3 localPosition, float yaw)
    {
        graveMarkerMaterial = MobModels.Material(
            graveMarkerMaterial,
            "Weathered grave stone",
            new Color(0.39f, 0.4f, 0.43f));
        GameObject marker = new GameObject("Scattered grave cross");
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        MobModels.Part(
            marker.transform,
            PrimitiveType.Cube,
            "Cross shaft",
            new Vector3(0f, 0.52f, 0f),
            new Vector3(0.2f, 1.04f, 0.16f),
            graveMarkerMaterial);
        MobModels.Part(
            marker.transform,
            PrimitiveType.Cube,
            "Cross beam",
            new Vector3(0f, 0.75f, 0f),
            new Vector3(0.62f, 0.17f, 0.18f),
            graveMarkerMaterial);
        MobModels.Part(
            marker.transform,
            PrimitiveType.Cube,
            "Grave base",
            new Vector3(0f, 0.1f, 0f),
            new Vector3(0.52f, 0.18f, 0.36f),
            graveMarkerMaterial);
    }

    private static void CreateDungeonEntranceDecorations(
        world currentWorld,
        Vector2Int coordinate,
        Transform parent)
    {
        int chunkX = coordinate.x;
        int chunkZ = coordinate.y;
        for (int candidateX = chunkX - 2; candidateX <= chunkX + 2; candidateX++)
        {
            for (int candidateZ = chunkZ - 2; candidateZ <= chunkZ + 2; candidateZ++)
            {
                if (!TryGetLocation(
                    candidateX, candidateZ, DungeonSpacing, 0x61d3, false, out Vector2Int dungeon))
                    continue;

                int surfaceY = currentWorld.GetHeight(dungeon.x, dungeon.y);
                if (surfaceY < 5)
                    continue;

                int chestZ = dungeon.y;
                int floorY = surfaceY - 5;
                for (int side = -1; side <= 1; side += 2)
                {
                    int chestX = dungeon.x + side * 2;
                    int chestChunkX = Mathf.FloorToInt((float)chestX / world.ChunkSize);
                    int chestChunkZ = Mathf.FloorToInt((float)chestZ / world.ChunkSize);
                    if (chestChunkX != coordinate.x || chestChunkZ != coordinate.y)
                        continue;

                    CreateDungeonChestDisplay(
                        parent,
                        new Vector3(
                            chestX - coordinate.x * world.ChunkSize + 0.5f,
                            floorY + 1.01f,
                            chestZ - coordinate.y * world.ChunkSize + 0.5f));
                }
            }
        }
    }

    private static void CreateDungeonChestDisplay(Transform parent, Vector3 localPosition)
    {
        dungeonChestMaterial = MobModels.Material(
            dungeonChestMaterial, "Dungeon chest wood", new Color(0.35f, 0.17f, 0.07f));
        dungeonChestMetalMaterial = MobModels.Material(
            dungeonChestMetalMaterial, "Dungeon chest iron", new Color(0.24f, 0.25f, 0.27f));
        dungeonCrossbowMaterial = MobModels.Material(
            dungeonCrossbowMaterial, "Dungeon crossbow wood", new Color(0.22f, 0.12f, 0.06f));

        GameObject chest = new GameObject("Dungeon chest and crossbow");
        chest.transform.SetParent(parent, false);
        chest.transform.localPosition = localPosition;
        MobModels.Part(
            chest.transform, PrimitiveType.Cube, "Chest body",
            new Vector3(0f, 0.27f, 0f), new Vector3(0.86f, 0.52f, 0.72f), dungeonChestMaterial);
        MobModels.Part(
            chest.transform, PrimitiveType.Cube, "Chest lid",
            new Vector3(0f, 0.57f, 0f), new Vector3(0.9f, 0.12f, 0.76f), dungeonChestMaterial);
        MobModels.Part(
            chest.transform, PrimitiveType.Cube, "Chest clasp",
            new Vector3(0f, 0.38f, 0.37f), new Vector3(0.12f, 0.2f, 0.04f), dungeonChestMetalMaterial);

        GameObject crossbow = new GameObject("Crossbow");
        crossbow.transform.SetParent(chest.transform, false);
        crossbow.transform.localPosition = new Vector3(0f, 0.68f, 0f);
        MobModels.Part(
            crossbow.transform, PrimitiveType.Cube, "Crossbow stock",
            new Vector3(0f, 0.04f, 0f), new Vector3(0.12f, 0.1f, 0.58f), dungeonCrossbowMaterial);
        GameObject leftLimb = MobModels.Part(
            crossbow.transform, PrimitiveType.Cube, "Crossbow left limb",
            new Vector3(-0.25f, 0.06f, -0.12f), new Vector3(0.46f, 0.07f, 0.08f),
            dungeonCrossbowMaterial);
        leftLimb.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        GameObject rightLimb = MobModels.Part(
            crossbow.transform, PrimitiveType.Cube, "Crossbow right limb",
            new Vector3(0.25f, 0.06f, -0.12f), new Vector3(0.46f, 0.07f, 0.08f),
            dungeonCrossbowMaterial);
        rightLimb.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
        MobModels.Part(
            crossbow.transform, PrimitiveType.Cube, "Crossbow string",
            new Vector3(0f, 0.09f, -0.12f), new Vector3(0.5f, 0.025f, 0.025f),
            dungeonChestMetalMaterial);
    }

    private static Vector2Int GetGraveyardLocation(int chunkX, int chunkZ, int hash)
    {
        System.Random random = new System.Random(hash);
        return new Vector2Int(
            chunkX * world.ChunkSize + random.Next(8, world.ChunkSize - 7),
            chunkZ * world.ChunkSize + random.Next(7, world.ChunkSize - 6));
    }

    private static bool TryGetLegacyGraveyardLocation(int chunkX, int chunkZ, out Vector2Int location)
    {
        if (chunkX == 0 && chunkZ == 1)
        {
            location = new Vector2Int(8, 24);
            return true;
        }

        unchecked
        {
            int hash = chunkX * 73856093 ^ chunkZ * 19349663 ^ 0x72b9;
            if (Mathf.Abs(hash % LegacyGraveyardSpacing) != 0)
            {
                location = Vector2Int.zero;
                return false;
            }

            location = GetGraveyardLocation(chunkX, chunkZ, hash);
            return true;
        }
    }

    private static void FindNearby(
        Vector3 position,
        int radiusChunks,
        int spacing,
        int salt,
        bool hut,
        List<Vector3> found)
    {
        found.Clear();
        int centerX = Mathf.FloorToInt(position.x / world.ChunkSize);
        int centerZ = Mathf.FloorToInt(position.z / world.ChunkSize);
        for (int x = centerX - radiusChunks; x <= centerX + radiusChunks; x++)
        {
            for (int z = centerZ - radiusChunks; z <= centerZ + radiusChunks; z++)
            {
                if (!TryGetLocation(x, z, spacing, salt, hut, out Vector2Int location))
                    continue;

                found.Add(new Vector3(
                    location.x + 0.5f,
                    WorldHeight(location.x, location.y) + 1.02f,
                    location.y + 0.5f));
            }
        }
    }

    private static bool TryGetLocation(
        int chunkX,
        int chunkZ,
        int spacing,
        int salt,
        bool hut,
        out Vector2Int location)
    {
        if (hut && chunkX == 1 && chunkZ == 0)
        {
            location = new Vector2Int(24, 8);
            return true;
        }

        if (!hut && chunkX == -1 && chunkZ == 1)
        {
            location = new Vector2Int(-24, 24);
            return true;
        }

        if (!hut && chunkX == 0 && chunkZ == 0)
        {
            location = new Vector2Int(8, 8);
            return true;
        }

        unchecked
        {
            int hash = chunkX * 73856093 ^ chunkZ * 19349663 ^ salt;
            if (Mathf.Abs(hash % spacing) != 0)
            {
                location = Vector2Int.zero;
                return false;
            }

            System.Random random = new System.Random(hash);
            int x = chunkX * world.ChunkSize + random.Next(4, world.ChunkSize - 4);
            int z = chunkZ * world.ChunkSize + random.Next(4, world.ChunkSize - 4);
            location = new Vector2Int(x, z);
            return true;
        }
    }

    private static int WorldHeight(int x, int z)
    {
        float broad = Mathf.PerlinNoise((x + 720f) * 0.035f, (z + 720f) * 0.035f);
        float detail = Mathf.PerlinNoise((x + 180f) * 0.11f, (z + 180f) * 0.11f);
        return Mathf.Clamp(5 + Mathf.RoundToInt(broad * 5f + detail * 2f), 4, world.WorldHeight - 1);
    }

    private static bool BuildHut(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        bool preserveExistingBlocks)
    {
        int baseY = currentWorld.GetHeight(center.x, center.y);
        if (preserveExistingBlocks &&
            !CanPlaceOnNaturalGround(currentWorld, chunkCoordinate, blocks, center, 3, baseY + 4))
            return false;

        bool changed = false;
        for (int x = -3; x <= 3; x++)
        {
            for (int z = -3; z <= 3; z++)
            {
                changed |= SetBlock(chunkCoordinate, blocks, center.x + x, baseY, center.y + z, ItemTypes.Planks);
                bool wall = Mathf.Abs(x) == 3 || Mathf.Abs(z) == 3;
                for (int y = 1; y <= 3; y++)
                {
                    bool doorway = z == -3 && x == 0 && y <= 2;
                    bool window = z == 3 && (x == -1 || x == 1) && y == 2;
                    if (wall && !doorway && !window)
                        changed |= SetBlock(
                            chunkCoordinate, blocks, center.x + x, baseY + y, center.y + z, ItemTypes.Log);
                    else if (window)
                        changed |= SetBlock(
                            chunkCoordinate, blocks, center.x + x, baseY + y, center.y + z, ItemTypes.Planks);
                    else
                        changed |= SetBlock(chunkCoordinate, blocks, center.x + x, baseY + y, center.y + z, 0);
                }

                changed |= SetBlock(
                    chunkCoordinate, blocks, center.x + x, baseY + 4, center.y + z, ItemTypes.Planks);
            }
        }

        changed |= SetBlock(chunkCoordinate, blocks, center.x, baseY + 1, center.y + 1, ItemTypes.Torch);
        changed |= SetBlock(chunkCoordinate, blocks, center.x, baseY + 1, center.y - 1, ItemTypes.CraftingTable);
        return changed;
    }

    private static bool BuildDungeon(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        bool preserveExistingBlocks)
    {
        int surfaceY = currentWorld.GetHeight(center.x, center.y);
        if (surfaceY < 5)
            return false;

        bool changed = false;
        // Always create dungeon entrance - force generation
        CreateDungeonEntrance(currentWorld, chunkCoordinate, blocks, center, surfaceY, ref changed);

        int entranceZ = center.y - 6;
        CreateRoomEntryHall(currentWorld, chunkCoordinate, blocks, center, surfaceY, ref changed, entranceZ);
        CreateCorridorRandom(currentWorld, chunkCoordinate, blocks, center, surfaceY, entranceZ, ref changed, 1);
        CreateRoomGuardChamber(currentWorld, chunkCoordinate, blocks, center, surfaceY, ref changed, entranceZ);
        CreateCorridorRandom(currentWorld, chunkCoordinate, blocks, center, surfaceY, entranceZ, ref changed, 2);
        CreateRoomTreasury(currentWorld, chunkCoordinate, blocks, center, surfaceY, ref changed, entranceZ);
        CreateCorridorRandom(currentWorld, chunkCoordinate, blocks, center, surfaceY, entranceZ, ref changed, 3);
        CreateRoomCrypt(currentWorld, chunkCoordinate, blocks, center, surfaceY, ref changed, entranceZ);
        CreateCorridorRandom(currentWorld, chunkCoordinate, blocks, center, surfaceY, entranceZ, ref changed, 4);
        CreateRoomBossArena(currentWorld, chunkCoordinate, blocks, center, surfaceY, ref changed, entranceZ);

        return changed;
    }

    private static void CreateDungeonEntrance(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int surfaceY,
        ref bool changed)
    {
        int entranceZ = center.y - 6;
        for (int x = center.x - 2; x <= center.x + 2; x++)
            changed |= SetBlock(chunkCoordinate, blocks, x, surfaceY + 4, entranceZ, ItemTypes.Stone);

        for (int y = surfaceY + 1; y <= surfaceY + 3; y++)
        {
            changed |= SetBlock(chunkCoordinate, blocks, center.x - 2, y, entranceZ, ItemTypes.Stone);
            changed |= SetBlock(chunkCoordinate, blocks, center.x + 2, y, entranceZ, ItemTypes.Stone);
        }

        for (int step = 1; step <= 4; step++)
        {
            int z = entranceZ + step;
            int floorY = surfaceY - step;
            for (int x = center.x - 1; x <= center.x + 1; x++)
            {
                changed |= SetBlock(chunkCoordinate, blocks, x, floorY, z, ItemTypes.Stone);
                for (int y = floorY + 1; y <= surfaceY + 3; y++)
                    changed |= SetBlock(chunkCoordinate, blocks, x, y, z, 0);
            }

            for (int y = floorY + 1; y <= surfaceY + 2; y++)
            {
                changed |= SetBlock(chunkCoordinate, blocks, center.x - 2, y, z, ItemTypes.Stone);
                changed |= SetBlock(chunkCoordinate, blocks, center.x + 2, y, z, ItemTypes.Stone);
            }
        }

        for (int x = center.x - 1; x <= center.x + 1; x++)
        {
            for (int y = surfaceY + 1; y <= surfaceY + 3; y++)
                changed |= SetBlock(chunkCoordinate, blocks, x, y, entranceZ, 0);
        }
        changed |= SetBlock(chunkCoordinate, blocks, center.x - 2, surfaceY + 2, entranceZ + 1, ItemTypes.Torch);
        changed |= SetBlock(chunkCoordinate, blocks, center.x + 2, surfaceY + 2, entranceZ + 1, ItemTypes.Torch);
    }

    private static void CreateCorridorRandom(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int surfaceY,
        int entranceZ,
        ref bool changed,
        int roomNum)
    {
        int[] roomOffsets = { 7, 18, 29, 40, 51 };
        int[] roomDepths = { 2, 4, 4, 4, 6 };
        int startZ = entranceZ + roomOffsets[roomNum - 1] + roomDepths[roomNum - 1];
        int endZ = entranceZ + roomOffsets[roomNum] - roomDepths[roomNum];
        int seed = unchecked(center.x * 73856093 ^ center.y * 19349663 ^ roomNum * 83492791);
        System.Random random = new System.Random(seed);
        int bendX = center.x + random.Next(-3, 4);
        int bendZ = Mathf.Clamp((startZ + endZ) / 2 + random.Next(-1, 2), startZ, endZ);

        for (int z = startZ; z <= endZ; z++)
            CreateCorridorCell(chunkCoordinate, blocks, center.x, z, surfaceY, ref changed);
        for (int x = Mathf.Min(center.x, bendX); x <= Mathf.Max(center.x, bendX); x++)
            CreateCorridorCell(chunkCoordinate, blocks, x, bendZ, surfaceY, ref changed);
        for (int z = bendZ; z <= endZ; z++)
            CreateCorridorCell(chunkCoordinate, blocks, bendX, z, surfaceY, ref changed);
        for (int x = Mathf.Min(center.x, bendX); x <= Mathf.Max(center.x, bendX); x++)
            CreateCorridorCell(chunkCoordinate, blocks, x, endZ, surfaceY, ref changed);
    }

    private static void CreateCorridorCell(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        int centerX,
        int z,
        int surfaceY,
        ref bool changed)
    {
        int floorY = surfaceY - 5;
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            changed |= SetBlock(chunkCoordinate, blocks, x, floorY, z, ItemTypes.Stone);
            for (int y = floorY + 1; y <= surfaceY - 2; y++)
                changed |= SetBlock(chunkCoordinate, blocks, x, y, z, 0);
        }

        for (int y = floorY + 1; y <= surfaceY - 2; y++)
        {
            changed |= SetBlock(chunkCoordinate, blocks, centerX - 2, y, z, ItemTypes.Stone);
            changed |= SetBlock(chunkCoordinate, blocks, centerX + 2, y, z, ItemTypes.Stone);
        }
        changed |= SetBlock(chunkCoordinate, blocks, centerX, surfaceY - 1, z, ItemTypes.Stone);
    }

    private static void CreateRoomEntryHall(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int surfaceY,
        ref bool changed,
        int entranceZ)
    {
        CreateDungeonRoom(
            chunkCoordinate, blocks, center.x, entranceZ + 7, 3, 2, surfaceY, true, ref changed);
    }

    private static void CreateRoomGuardChamber(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int surfaceY,
        ref bool changed,
        int entranceZ)
    {
        CreateDungeonRoom(
            chunkCoordinate, blocks, center.x, entranceZ + 18, 4, 4, surfaceY, true, ref changed);
        changed |= SetBlock(
            chunkCoordinate, blocks, center.x - 3, surfaceY - 3, entranceZ + 18, ItemTypes.Torch);
        changed |= SetBlock(
            chunkCoordinate, blocks, center.x + 3, surfaceY - 3, entranceZ + 18, ItemTypes.Torch);
    }

    private static void CreateRoomTreasury(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int surfaceY,
        ref bool changed,
        int entranceZ)
    {
        int roomZ = entranceZ + 29;
        CreateDungeonRoom(chunkCoordinate, blocks, center.x, roomZ, 4, 4, surfaceY, true, ref changed);
        changed |= SetBlock(chunkCoordinate, blocks, center.x, surfaceY - 3, roomZ, ItemTypes.CraftingTable);
    }

    private static void CreateRoomCrypt(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int surfaceY,
        ref bool changed,
        int entranceZ)
    {
        int roomZ = entranceZ + 40;
        CreateDungeonRoom(chunkCoordinate, blocks, center.x, roomZ, 4, 4, surfaceY, true, ref changed);
        for (int offset = -2; offset <= 2; offset += 4)
            changed |= SetBlock(
                chunkCoordinate, blocks, center.x + offset, surfaceY - 3, roomZ, ItemTypes.Stone);
    }

    private static void CreateRoomBossArena(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int surfaceY,
        ref bool changed,
        int entranceZ)
    {
        int roomZ = entranceZ + 51;
        CreateDungeonRoom(chunkCoordinate, blocks, center.x, roomZ, 6, 6, surfaceY, false, ref changed);
        for (int xOffset = -3; xOffset <= 3; xOffset += 6)
        {
            for (int zOffset = -3; zOffset <= 3; zOffset += 6)
            {
                for (int y = surfaceY - 3; y <= surfaceY - 2; y++)
                    changed |= SetBlock(
                        chunkCoordinate, blocks, center.x + xOffset, y, roomZ + zOffset, ItemTypes.Stone);
            }
        }
    }

    private static void CreateDungeonRoom(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        int centerX,
        int centerZ,
        int radiusX,
        int radiusZ,
        int surfaceY,
        bool hasForwardExit,
        ref bool changed)
    {
        int floorY = surfaceY - 5;
        int ceilingY = surfaceY - 1;
        for (int xOffset = -radiusX; xOffset <= radiusX; xOffset++)
        {
            for (int zOffset = -radiusZ; zOffset <= radiusZ; zOffset++)
            {
                changed |= SetBlock(
                    chunkCoordinate, blocks, centerX + xOffset, floorY, centerZ + zOffset, ItemTypes.Stone);
                changed |= SetBlock(
                    chunkCoordinate, blocks, centerX + xOffset, ceilingY, centerZ + zOffset, ItemTypes.Stone);

                bool wall = Mathf.Abs(xOffset) == radiusX || Mathf.Abs(zOffset) == radiusZ;
                bool doorway = Mathf.Abs(xOffset) <= 1 &&
                    (zOffset == -radiusZ || (hasForwardExit && zOffset == radiusZ));
                for (int y = floorY + 1; y < ceilingY; y++)
                {
                    int block = wall && !doorway ? ItemTypes.Stone : 0;
                    changed |= SetBlock(
                        chunkCoordinate, blocks, centerX + xOffset, y, centerZ + zOffset, block);
                }
            }
        }
    }

    private static bool HasLegacyDungeon(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int surfaceY)
    {
        int chunkMinX = chunkCoordinate.x * world.ChunkSize;
        int chunkMinZ = chunkCoordinate.y * world.ChunkSize;
        int cleared = 0;
        for (int x = Mathf.Max(center.x - 13, chunkMinX); x <= Mathf.Min(center.x + 13, chunkMinX + world.ChunkSize - 1); x++)
        {
            for (int z = Mathf.Max(center.y - 13, chunkMinZ); z <= Mathf.Min(center.y + 13, chunkMinZ + world.ChunkSize - 1); z++)
            {
                int groundY = currentWorld.GetHeight(x, z);
                for (int y = Mathf.Max(groundY, surfaceY); y <= surfaceY + 5 && y < world.WorldHeight; y++)
                {
                    if (GetBlock(blocks, x - chunkMinX, y, z - chunkMinZ) != ItemTypes.Stone)
                        continue;

                    int replacement = y == groundY ? ItemTypes.Grass : 0;
                    if (SetBlock(chunkCoordinate, blocks, x, y, z, replacement))
                        cleared++;
                }
            }
        }
        return cleared > 0;
    }

    private static bool ClearLegacyCemetery(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int groundY)
    {
        bool changed = false;
        for (int offset = -8; offset <= 8; offset += 2)
        {
            if (offset != 0)
                changed |= ClearBlockIfType(
                    chunkCoordinate, blocks, center.x + offset, groundY + 1, center.y - 8, ItemTypes.Log);
            changed |= ClearBlockIfType(
                chunkCoordinate, blocks, center.x + offset, groundY + 1, center.y + 8, ItemTypes.Log);
            changed |= ClearBlockIfType(
                chunkCoordinate, blocks, center.x - 8, groundY + 1, center.y + offset, ItemTypes.Log);
            changed |= ClearBlockIfType(
                chunkCoordinate, blocks, center.x + 8, groundY + 1, center.y + offset, ItemTypes.Log);
        }

        for (int x = -6; x <= 6; x++)
        {
            for (int z = -7; z <= 7; z++)
            {
                changed |= ClearBlockIfType(
                    chunkCoordinate,
                    blocks,
                    center.x + x,
                    groundY + 1,
                    center.y + z,
                    ItemTypes.Dirt);
            }
        }

        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x, groundY + 1, center.y - 8, ItemTypes.Planks);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x, groundY + 2, center.y - 8, ItemTypes.Planks);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x - 1, groundY + 2, center.y - 8, ItemTypes.Planks);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x + 1, groundY + 2, center.y - 8, ItemTypes.Planks);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x - 6, groundY + 1, center.y - 6, ItemTypes.Torch);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x + 6, groundY + 1, center.y - 6, ItemTypes.Torch);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x - 6, groundY + 1, center.y + 6, ItemTypes.Torch);
        changed |= ClearBlockIfType(
            chunkCoordinate, blocks, center.x + 6, groundY + 1, center.y + 6, ItemTypes.Torch);

        Vector2Int[] oldMarkers =
        {
            new Vector2Int(-4, -4), new Vector2Int(4, -4),
            new Vector2Int(-4, 0), new Vector2Int(4, 0),
            new Vector2Int(-4, 4), new Vector2Int(4, 4),
            new Vector2Int(0, 2),
            new Vector2Int(-3, -1), new Vector2Int(3, -1),
            new Vector2Int(-3, 3), new Vector2Int(3, 3),
            new Vector2Int(0, 1)
        };
        for (int i = 0; i < oldMarkers.Length; i++)
        {
            int worldX = center.x + oldMarkers[i].x;
            int worldZ = center.y + oldMarkers[i].y;
            int markerBlocks = 0;
            for (int y = groundY + 1; y <= groundY + 4; y++)
            {
                int localX = worldX - chunkCoordinate.x * world.ChunkSize;
                int localZ = worldZ - chunkCoordinate.y * world.ChunkSize;
                if (GetBlock(blocks, localX, y, localZ) == ItemTypes.Stone)
                    markerBlocks++;
            }
            if (markerBlocks < 3)
                continue;

            for (int y = groundY + 1; y <= groundY + 4; y++)
                changed |= ClearBlockIfType(
                    chunkCoordinate, blocks, worldX, y, worldZ, ItemTypes.Stone);
            int crossY = groundY + 2;
            for (int arm = -1; arm <= 1; arm += 2)
                changed |= ClearBlockIfType(
                    chunkCoordinate,
                    blocks,
                    worldX + arm,
                    crossY,
                    worldZ,
                    ItemTypes.Stone);
        }
        return changed;
    }

    private static bool ClearBlockIfType(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        int worldX,
        int y,
        int worldZ,
        int expectedBlock)
    {
        int localX = worldX - chunkCoordinate.x * world.ChunkSize;
        int localZ = worldZ - chunkCoordinate.y * world.ChunkSize;
        return GetBlock(blocks, localX, y, localZ) == expectedBlock &&
            SetBlock(chunkCoordinate, blocks, worldX, y, worldZ, 0);
    }

    private static int GetBlock(int[,,] blocks, int x, int y, int z)
    {
        return x >= 0 && x < world.ChunkSize && y >= 0 && y < world.WorldHeight &&
            z >= 0 && z < world.ChunkSize
            ? blocks[x, y, z]
            : 0;
    }

    private static bool CanPlaceOnNaturalGround(
        world currentWorld,
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        Vector2Int center,
        int radius,
        int maximumY)
    {
        int chunkMinX = chunkCoordinate.x * world.ChunkSize;
        int chunkMinZ = chunkCoordinate.y * world.ChunkSize;
        for (int worldX = center.x - radius; worldX <= center.x + radius; worldX++)
        {
            for (int worldZ = center.y - radius; worldZ <= center.y + radius; worldZ++)
            {
                int localX = worldX - chunkMinX;
                int localZ = worldZ - chunkMinZ;
                if (localX < 0 || localX >= world.ChunkSize || localZ < 0 || localZ >= world.ChunkSize)
                    continue;

                int groundY = currentWorld.GetHeight(worldX, worldZ);
                if (blocks[localX, groundY, localZ] != ItemTypes.Grass)
                    return false;
                for (int y = groundY + 1; y <= maximumY && y < world.WorldHeight; y++)
                {
                    if (blocks[localX, y, localZ] != 0)
                        return false;
                }
            }
        }
        return true;
    }

    private static bool SetBlock(
        Vector2Int chunkCoordinate,
        int[,,] blocks,
        int worldX,
        int y,
        int worldZ,
        int block)
    {
        if (y < 0 || y >= world.WorldHeight)
            return false;

        int localX = worldX - chunkCoordinate.x * world.ChunkSize;
        int localZ = worldZ - chunkCoordinate.y * world.ChunkSize;
        if (localX >= 0 && localX < world.ChunkSize &&
            localZ >= 0 && localZ < world.ChunkSize &&
            blocks[localX, y, localZ] != block)
        {
            blocks[localX, y, localZ] = block;
            return true;
        }
        return false;
    }
}
