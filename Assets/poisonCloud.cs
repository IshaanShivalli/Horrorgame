using UnityEngine;

public sealed class poisonCloud : MonoBehaviour
{
    private const float Duration = 3.2f;
    private const float DamageInterval = 0.8f;
    private const float DamageRadius = 2.5f;

    private world currentWorld;
    private ParticleSystem particles;
    private float lifetime;
    private float damageTimer;

    public void Initialize(world owningWorld, ParticleSystem cloudParticles)
    {
        currentWorld = owningWorld;
        particles = cloudParticles;
    }

    private void Update()
    {
        lifetime += Time.deltaTime;
        if (lifetime >= Duration)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gameObject, 1f);
            enabled = false;
            return;
        }

        damageTimer += Time.deltaTime;
        if (damageTimer < DamageInterval || currentWorld.PlayerController == null)
            return;

        damageTimer -= DamageInterval;
        player target = currentWorld.PlayerController;
        if ((target.transform.position - transform.position).sqrMagnitude <= DamageRadius * DamageRadius)
            target.TakeDamage(1f, "ghoul poison");
    }
}
