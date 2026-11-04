using UnityEngine;

public sealed class witchSpell : MonoBehaviour
{
    private const float Speed = 9f;
    private const float MaximumLifetime = 5f;
    private static Material spellMaterial;

    private player target;
    private Vector3 direction;
    private float lifetime;

    public void Launch(world currentWorld, player targetPlayer, Vector3 origin)
    {
        target = targetPlayer;
        transform.SetParent(currentWorld.transform, false);
        transform.position = origin;

        Vector3 targetPosition = target.transform.position + Vector3.up * 0.8f;
        direction = (targetPosition - origin).normalized;
        transform.rotation = Quaternion.LookRotation(direction);

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Poison bolt";
        orb.transform.SetParent(transform, false);
        orb.transform.localScale = Vector3.one * 0.34f;
        Material boltMaterial = MobModels.Material(
            spellMaterial,
            "Witch poison bolt",
            new Color(0.27f, 0.95f, 0.12f));
        if (boltMaterial.HasProperty("_EmissionColor"))
        {
            boltMaterial.EnableKeyword("_EMISSION");
            boltMaterial.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.05f) * 2f);
        }
        spellMaterial = boltMaterial;
        orb.GetComponent<Renderer>().sharedMaterial = spellMaterial;
        Collider orbCollider = orb.GetComponent<Collider>();
        if (orbCollider != null)
            Destroy(orbCollider);

        GameObject trail = new GameObject("Poison bolt trail");
        trail.transform.SetParent(transform, false);
        ParticleSystem particles = trail.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = 0.45f;
        main.startSpeed = 0.15f;
        main.startSize = 0.16f;
        main.startColor = new Color(0.35f, 1f, 0.08f, 0.8f);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 28f;
        ParticleSystemRenderer particleRenderer = trail.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = MobEffects.GetParticleMaterial(
            new Color(0.35f, 1f, 0.08f, 0.8f));
        particles.Play();
    }

    private void Update()
    {
        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        lifetime += Time.deltaTime;
        if (lifetime >= MaximumLifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.transform.position + Vector3.up * 0.8f;
        Vector3 targetDirection = targetPosition - transform.position;
        if (targetDirection.sqrMagnitude > 0.01f)
            direction = Vector3.RotateTowards(direction, targetDirection.normalized, 2.2f * Time.deltaTime, 0f);

        transform.position += direction * (Speed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(direction);
        if ((targetPosition - transform.position).sqrMagnitude > 0.55f * 0.55f)
            return;

        target.TakeDamage(4f, "witch's poison bolt");
        Destroy(gameObject);
    }
}
