using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class ObjectPlacementRule
{
    public GameObject Prefab;
    [Range(0.001f, 10f)] public float Weight = 1f;
    public Vector2 UniformScaleRange = Vector2.one;
    public Vector2 VerticalOffsetRange = Vector2.zero;
}

public class RandomObjectPlacer : BaseObjectPlacer
{
    [SerializeField] ObjectPlacementRule[] PlacementRules;
    [SerializeField] [Min(0.5f)] float CellSize = 4f;
    [SerializeField] [Range(0f, 1f)] float PlacementChance = 0.35f;
    [SerializeField] Vector2 NormalizedHeightRange = new(0f, 1f);
    [SerializeField] Vector2 SlopeRange = new(0f, 35f);
    [SerializeField] Vector2 RotationYRange = new(0f, 360f);
    [SerializeField] bool AlignToSurfaceNormal = true;
    [SerializeField] int SeedOffset = 0;

    public override void Execute(in ObjectPlacerContext context)
    {
        if (context.TargetTerrain == null || context.TerrainData == null)
        {
            Debug.LogWarning($"RandomObjectPlacer on {gameObject.name} is missing a target terrain.");
            return;
        }

        if (PlacementRules == null || PlacementRules.Length == 0)
        {
            Debug.LogWarning($"RandomObjectPlacer on {gameObject.name} has no placement rules.");
            return;
        }

        if (CellSize <= Mathf.Epsilon)
        {
            Debug.LogWarning($"RandomObjectPlacer on {gameObject.name} has an invalid cell size.");
            return;
        }

        float effectivePlacementChance = Mathf.Clamp01(PlacementChance * Strength);
        if (effectivePlacementChance <= Mathf.Epsilon)
            return;

        float totalWeight = 0f;
        for (int ruleIndex = 0; ruleIndex < PlacementRules.Length; ++ruleIndex)
        {
            ObjectPlacementRule rule = PlacementRules[ruleIndex];
            if (rule == null || rule.Prefab == null)
                continue;

            totalWeight += Mathf.Max(0f, rule.Weight);
        }

        if (totalWeight <= Mathf.Epsilon)
        {
            Debug.LogWarning($"RandomObjectPlacer on {gameObject.name} has no weighted prefab entries.");
            return;
        }

        Vector2 heightRange = SortRange01(NormalizedHeightRange);
        Vector2 slopeRange = SortRange(SlopeRange);
        Vector2 terrainSizeXZ = new(context.TerrainData.size.x, context.TerrainData.size.z);
        int cellCountX = Mathf.Max(1, Mathf.CeilToInt(terrainSizeXZ.x / CellSize));
        int cellCountY = Mathf.Max(1, Mathf.CeilToInt(terrainSizeXZ.y / CellSize));

        for (int cellY = 0; cellY < cellCountY; ++cellY)
        {
            for (int cellX = 0; cellX < cellCountX; ++cellX)
            {
                float placementRoll = Sample01(cellX, cellY, 0);
                if (placementRoll > effectivePlacementChance)
                    continue;

                float normalizedX = Mathf.Clamp01((((float)cellX + Sample01(cellX, cellY, 1)) * CellSize) / Mathf.Max(Mathf.Epsilon, terrainSizeXZ.x));
                float normalizedY = Mathf.Clamp01((((float)cellY + Sample01(cellX, cellY, 2)) * CellSize) / Mathf.Max(Mathf.Epsilon, terrainSizeXZ.y));

                if (!context.TargetsBiomeAtNormalized(normalizedX, normalizedY))
                    continue;

                float normalizedHeight = context.GetNormalizedHeight(normalizedX, normalizedY);
                if (normalizedHeight < heightRange.x || normalizedHeight > heightRange.y)
                    continue;

                float slope = context.GetSlope(normalizedX, normalizedY);
                if (slope < slopeRange.x || slope > slopeRange.y)
                    continue;

                ObjectPlacementRule selectedRule = SelectRule(cellX, cellY, totalWeight);
                if (selectedRule == null || selectedRule.Prefab == null)
                    continue;

                float verticalOffset = Mathf.Lerp(
                    selectedRule.VerticalOffsetRange.x,
                    selectedRule.VerticalOffsetRange.y,
                    Sample01(cellX, cellY, 4));
                Vector3 position = context.GetWorldPosition(normalizedX, normalizedY, verticalOffset);

                float yaw = Mathf.Lerp(RotationYRange.x, RotationYRange.y, Sample01(cellX, cellY, 5));
                Quaternion rotation = Quaternion.AngleAxis(yaw, Vector3.up);
                if (AlignToSurfaceNormal)
                {
                    rotation = Quaternion.FromToRotation(Vector3.up, context.GetSurfaceNormal(normalizedX, normalizedY)) * rotation;
                }

                float uniformScale = Mathf.Max(
                    0.0001f,
                    Mathf.Lerp(
                        selectedRule.UniformScaleRange.x,
                        selectedRule.UniformScaleRange.y,
                        Sample01(cellX, cellY, 6)));

                GameObject instance = InstantiateRulePrefab(selectedRule.Prefab, context.Parent);
                if (instance == null)
                    continue;

                instance.name = selectedRule.Prefab.name;
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.transform.localScale = Vector3.one * uniformScale;
            }
        }
    }

    private ObjectPlacementRule SelectRule(int cellX, int cellY, float totalWeight)
    {
        float selection = Sample01(cellX, cellY, 3) * totalWeight;
        float accumulatedWeight = 0f;
        ObjectPlacementRule fallbackRule = null;

        for (int ruleIndex = 0; ruleIndex < PlacementRules.Length; ++ruleIndex)
        {
            ObjectPlacementRule rule = PlacementRules[ruleIndex];
            if (rule == null || rule.Prefab == null)
                continue;

            fallbackRule = rule;
            accumulatedWeight += Mathf.Max(0f, rule.Weight);
            if (selection <= accumulatedWeight)
                return rule;
        }

        return fallbackRule;
    }

    private float Sample01(int cellX, int cellY, int salt)
    {
        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)cellX) * 16777619u;
            hash = (hash ^ (uint)cellY) * 16777619u;
            hash = (hash ^ (uint)(SeedOffset * 397)) * 16777619u;
            hash = (hash ^ (uint)(salt * 7919)) * 16777619u;
            hash ^= hash >> 13;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return (hash & 0x00FFFFFFu) / 16777215f;
        }
    }

    private static Vector2 SortRange(Vector2 range)
    {
        return range.x <= range.y ? range : new Vector2(range.y, range.x);
    }

    private static Vector2 SortRange01(Vector2 range)
    {
        Vector2 sorted = SortRange(range);
        return new Vector2(Mathf.Clamp01(sorted.x), Mathf.Clamp01(sorted.y));
    }

    private static GameObject InstantiateRulePrefab(GameObject prefab, Transform parent)
    {
        if (prefab == null)
            return null;

        GameObject instance;
#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
        else
        {
            instance = UnityEngine.Object.Instantiate(prefab);
        }
#else
        instance = UnityEngine.Object.Instantiate(prefab);
#endif

        if (instance == null)
            return null;

        if (parent != null)
        {
            instance.transform.SetParent(parent, true);
        }

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(instance, "Place biome object");
#endif

        return instance;
    }
}
