using UnityEngine;

public sealed class pumpkinBomb : hostileMob
{
    private static Material pumpkinMaterial;
    private static Material stemMaterial;
    private static Material faceMaterial;
    private static Material explosionMaterial;
    private const float ExplosionRange = 2.4f;
    private const float ExplosionRadius = 4.5f;
    private const float ExplosionDamage = 8f;
    private bool exploded;

    protected override float MaximumHealth => 8f;
    protected override float AttackDamage => ExplosionDamage;
    protected override float AttackCooldown => 1f;
    protected override float MovementSpeed => 2.5f;
    protected override bool CanFly => false;
    public override string DisplayName => "pumpkin";

    private void Awake()
    {
        pumpkinMaterial = MobModels.Material(
            pumpkinMaterial,
            "Blood moon pumpkin",
            new Color(0.94f, 0.29f, 0.025f));
        stemMaterial = MobModels.Material(
            stemMaterial,
            "Pumpkin stem",
            new Color(0.19f, 0.31f, 0.08f));
        faceMaterial = MobModels.Material(
            faceMaterial,
            "Carved pumpkin face",
            new Color(0.09f, 0.035f, 0.012f));

        MobModels.Part(
            transform,
            PrimitiveType.Sphere,
            "Pumpkin body",
            new Vector3(0f, 0.85f, 0f),
            new Vector3(1.1f, 1.1f, 1.1f),
            pumpkinMaterial);
        MobModels.Part(
            transform,
            PrimitiveType.Cylinder,
            "Pumpkin stem",
            new Vector3(0f, 1.5f, 0f),
            new Vector3(0.15f, 0.25f, 0.15f),
            stemMaterial);
        MobModels.Part(
            transform,
            PrimitiveType.Cube,
            "Left carved eye",
            new Vector3(-0.22f, 1.05f, 0.4f),
            new Vector3(0.15f, 0.15f, 0.06f),
            faceMaterial);
        MobModels.Part(
            transform,
            PrimitiveType.Cube,
            "Right carved eye",
            new Vector3(0.22f, 1.05f, 0.4f),
            new Vector3(0.15f, 0.15f, 0.06f),
            faceMaterial);
        MobModels.Part(
            transform,
            PrimitiveType.Cube,
            "Carved mouth",
            new Vector3(0f, 0.7f, 0.42f),
            new Vector3(0.35f, 0.1f, 0.06f),
            faceMaterial);
    }

    protected override void Think(float deltaTime)
    {
        Vector3 offset = TargetPlayer.transform.position - transform.position;
        if (offset.sqrMagnitude <= ExplosionRange * ExplosionRange)
        {
            Explode();
            return;
        }

        MoveToward(TargetPlayer.transform.position, deltaTime);
    }

    private void Explode()
    {
        if (exploded)
            return;

        exploded = true;
        Vector3 blastPosition = transform.position + Vector3.up * 0.8f;
        SpawnExplosionEffect(blastPosition);
        if ((TargetPlayer.transform.position - blastPosition).sqrMagnitude <=
            ExplosionRadius * ExplosionRadius)
            TargetPlayer.TakeDamage(ExplosionDamage, "a blood moon pumpkin");

        Destroy(gameObject);
    }

    private void SpawnExplosionEffect(Vector3 position)
    {
        GameObject effect = new GameObject("Pumpkin explosion");
        effect.transform.SetParent(CurrentWorld.transform, false);
        effect.transform.position = position;

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.75f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.17f, 0.015f),
            new Color(1f, 0.83f, 0.08f));
        main.maxParticles = 48;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
        explosionMaterial = MobModels.Material(
            explosionMaterial,
            "Pumpkin explosion particles",
            new Color(1f, 0.36f, 0.025f));
        particleRenderer.sharedMaterial = explosionMaterial;

        Light blastLight = effect.AddComponent<Light>();
        blastLight.type = LightType.Point;
        blastLight.color = new Color(1f, 0.29f, 0.035f);
        blastLight.intensity = 4f;
        blastLight.range = 8f;
        blastLight.shadows = LightShadows.None;

        particles.Emit(40);
        particles.Play();
    }
}
