using UnityEngine;

public sealed class witches : hostileMob
{
    private static Material robe;
    private static Material skin;
    private static Material eyes;
    private float spellTimer = 1f;
    private Vector3 dungeonDoorway;
    private Vector3 dungeonExterior;
    private bool exitingDungeon;

    protected override float MaximumHealth => 30f;
    protected override float AttackDamage => 4f;
    protected override float AttackCooldown => 2.4f;
    protected override float MovementSpeed => 1.15f;
    protected override bool CanFly => false;
    protected override float AttackRange => 12f;
    public override string DisplayName => "witch";

    public void SetDungeonExit(Vector3 doorway, Vector3 exterior)
    {
        dungeonDoorway = doorway;
        dungeonExterior = exterior;
        exitingDungeon = true;
    }

    private void Awake()
    {
        robe = MobModels.Material(robe, "Witch robe", new Color(0.19f, 0.08f, 0.3f));
        skin = MobModels.Material(skin, "Witch skin", new Color(0.52f, 0.57f, 0.4f));
        eyes = MobModels.Material(eyes, "Witch magic", new Color(0.3f, 1f, 0.08f));
        if (eyes.HasProperty("_EmissionColor"))
        {
            eyes.EnableKeyword("_EMISSION");
            eyes.SetColor("_EmissionColor", new Color(0.25f, 1f, 0.04f) * 2f);
        }
        MobModels.CreateWitch(transform, skin, robe, eyes);

        GameObject magicLight = new GameObject("Witch magic light");
        magicLight.transform.SetParent(transform, false);
        magicLight.transform.localPosition = new Vector3(0.65f, 1.82f, 0.12f);
        Light light = magicLight.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.35f, 1f, 0.1f);
        light.intensity = 1.5f;
        light.range = 6f;
        light.shadows = LightShadows.None;
    }

    protected override void Think(float deltaTime)
    {
        spellTimer -= deltaTime;
        if (exitingDungeon)
        {
            Vector3 exitTarget = Vector3.Distance(transform.position, dungeonDoorway) <= 1f
                ? dungeonExterior
                : dungeonDoorway;
            if (Vector3.Distance(transform.position, dungeonExterior) <= 1.25f)
                exitingDungeon = false;
            else
            {
                MoveToward(exitTarget, deltaTime);
                return;
            }
        }

        Vector3 offset = TargetPlayer.transform.position - transform.position;
        float distance = offset.magnitude;
        if (distance > 11f)
        {
            MoveToward(TargetPlayer.transform.position, deltaTime);
            return;
        }

        Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
        if (distance < 4f && horizontal.sqrMagnitude > 0.01f)
        {
            MoveToward(transform.position - horizontal.normalized * 5f, deltaTime);
            return;
        }

        if (horizontal.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(horizontal),
                6f * deltaTime);

        if (distance > 10f || spellTimer > 0f || !HasLineOfSight())
            return;

        GameObject spellObject = new GameObject("Witch poison spell");
        witchSpell spell = spellObject.AddComponent<witchSpell>();
        spell.Launch(
            CurrentWorld,
            TargetPlayer,
            transform.position + Vector3.up * 1.55f + transform.forward * 0.35f);
        spellTimer = AttackCooldown;
        PlayVocalization();
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 1.55f;
        Vector3 target = TargetPlayer.transform.position + Vector3.up * 0.8f;
        Vector3 direction = target - origin;
        float rayDistance = direction.magnitude;
        if (rayDistance <= 0.01f ||
            !Physics.Raycast(
                origin,
                direction / rayDistance,
                out RaycastHit hit,
                rayDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
            return false;

        return hit.transform == TargetPlayer.transform ||
            hit.transform.IsChildOf(TargetPlayer.transform);
    }
}
