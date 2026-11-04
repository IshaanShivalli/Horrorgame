using UnityEngine;

public static class MobEffects
{
    private static Material redParticlesMaterial;
    private static Material greenParticlesMaterial;

    public static void EmitBlood(world currentWorld, Vector3 position, Color color)
    {
        GameObject effect = new GameObject("Mob blood particles");
        effect.transform.SetParent(currentWorld.transform, false);
        effect.transform.position = position;

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.8f;
        main.loop = false;
        main.startLifetime = 0.45f;
        main.startSpeed = 2.2f;
        main.startSize = 0.13f;
        main.startColor = color;
        main.maxParticles = 24;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = GetParticleMaterial(color);
        particles.Emit(12);
        particles.Play();
    }

    public static void SpawnPoisonCloud(world currentWorld, Vector3 position)
    {
        GameObject cloud = new GameObject("Poisonous ghoul cloud");
        cloud.transform.SetParent(currentWorld.transform, false);
        cloud.transform.position = position;

        ParticleSystem particles = cloud.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 3.2f;
        main.loop = true;
        main.startLifetime = 0.9f;
        main.startSpeed = 0.55f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
        main.startColor = new Color(0.31f, 0.95f, 0.12f, 0.72f);
        main.maxParticles = 80;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.65f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 22f;

        ParticleSystemRenderer particleRenderer = cloud.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = GetParticleMaterial(new Color(0.31f, 0.95f, 0.12f));

        poisonCloud damage = cloud.AddComponent<poisonCloud>();
        damage.Initialize(currentWorld, particles);
        particles.Play();
    }

    public static Material GetParticleMaterial(Color color)
    {
        bool green = color.g > color.r;
        Material material = green ? greenParticlesMaterial : redParticlesMaterial;
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            material = new Material(shader)
            {
                name = green ? "Green toxic particles" : "Zombie blood particles"
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (green)
                greenParticlesMaterial = material;
            else
                redParticlesMaterial = material;
        }
        return material;
    }
}
