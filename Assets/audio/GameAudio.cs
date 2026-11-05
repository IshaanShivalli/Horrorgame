using UnityEngine;

public sealed class GameAudio : MonoBehaviour
{
    private static GameAudio instance;

    private AudioSource musicSource;
    private AudioSource effectsSource;
    private AudioClip normalMusic;
    private AudioClip redMoonMusic;
    private AudioClip attackClip;
    private AudioClip purchaseClip;
    private AudioClip creepyDrone;
    private AudioClip[] mobVocalizations;
    private AudioClip[] mobDeathSounds;
    private AudioSource droneSource;

    public static void PlayAttack()
    {
        if (instance != null && instance.attackClip != null)
            instance.effectsSource.PlayOneShot(instance.attackClip, 0.55f);
    }

    public static void PlayPurchase()
    {
        if (instance != null && instance.purchaseClip != null)
            instance.effectsSource.PlayOneShot(instance.purchaseClip, 0.75f);
    }

    public static void PlayPickup()
    {
        if (instance != null && instance.purchaseClip != null)
            instance.effectsSource.PlayOneShot(instance.purchaseClip, 0.3f);
    }

    public static void PlayMobSound(AudioSource source, string mobName)
    {
        if (source == null || instance == null || instance.mobVocalizations == null)
            return;

        int mobIndex = GetMobIndex(mobName);
        source.pitch = Random.Range(0.94f, 1.06f);
        source.PlayOneShot(instance.mobVocalizations[mobIndex], 0.6f);
    }

    public static void PlayMobDeathSound(string mobName, Vector3 position)
    {
        if (instance == null || instance.mobDeathSounds == null)
            return;

        AudioClip clip = instance.mobDeathSounds[GetMobIndex(mobName)];
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, position, 0.8f);
    }

    public void SetRedMoon(bool isRedMoon)
    {
        AudioClip requested = isRedMoon ? redMoonMusic : normalMusic;
        if (requested != null && musicSource.clip != requested)
        {
            musicSource.clip = requested;
            musicSource.Play();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.22f;
        musicSource.spatialBlend = 0f;
        droneSource = gameObject.AddComponent<AudioSource>();
        droneSource.loop = true;
        droneSource.volume = 0.09f;
        droneSource.spatialBlend = 0f;
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.spatialBlend = 0f;

        normalMusic = Resources.Load<AudioClip>("audio/night_ambience");
        redMoonMusic = Resources.Load<AudioClip>("audio/red_moon");
        attackClip = Resources.Load<AudioClip>("audio/monster_attack");
        purchaseClip = Resources.Load<AudioClip>("audio/purchase");
        creepyDrone = CreateCreepyDrone();
        mobVocalizations = new AudioClip[3];
        mobDeathSounds = new AudioClip[3];
        for (int mobIndex = 0; mobIndex < mobVocalizations.Length; mobIndex++)
        {
            mobVocalizations[mobIndex] = CreateMobClip(mobIndex, false);
            mobDeathSounds[mobIndex] = CreateMobClip(mobIndex, true);
        }
        CheckClip(normalMusic, "audio/night_ambience");
        CheckClip(redMoonMusic, "audio/red_moon");
        CheckClip(attackClip, "audio/monster_attack");
        CheckClip(purchaseClip, "audio/purchase");
    }

    private void Start()
    {
        if (normalMusic != null)
        {
            musicSource.clip = normalMusic;
            musicSource.Play();
        }
        droneSource.clip = creepyDrone;
        droneSource.Play();
    }

    private static int GetMobIndex(string mobName)
    {
        if (mobName == "witch")
            return 1;
        if (mobName == "ghoul")
            return 2;
        return 0;
    }

    private static AudioClip CreateMobClip(int mobIndex, bool isDeathSound)
    {
        const int sampleRate = 22050;
        float duration = isDeathSound ? 0.85f : 0.72f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float startFrequency = mobIndex == 0 ? 92f : mobIndex == 1 ? 285f : 470f;
        float endFrequency = mobIndex == 0 ? 48f : mobIndex == 1 ? 165f : 120f;
        if (isDeathSound)
            endFrequency *= 0.68f;

        float phase = 0f;
        float filteredNoise = 0f;
        System.Random random = new System.Random(mobIndex * 17 + (isDeathSound ? 91 : 37));
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float progress = time / duration;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            if (mobIndex == 1 && !isDeathSound)
                frequency *= 1f + 0.11f * Mathf.Sin(time * 27f);
            else if (mobIndex == 2)
                frequency *= 1f + 0.08f * Mathf.Sin(time * 12f);
            phase += 2f * Mathf.PI * frequency / sampleRate;

            float envelope = Mathf.Sin(Mathf.PI * progress);
            if (mobIndex == 1 && !isDeathSound)
                envelope *= Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 19f)), 0.45f);
            if (isDeathSound)
                envelope *= 1f - 0.3f * progress;

            float noise = (float)random.NextDouble() * 2f - 1f;
            filteredNoise += 0.16f * (noise - filteredNoise);
            float harmonics = Mathf.Sin(phase) +
                0.42f * Mathf.Sin(phase * 2.03f) +
                0.2f * Mathf.Sin(phase * 3.97f);
            float rasp = filteredNoise * (mobIndex == 1 ? 0.14f : 0.3f);
            samples[i] = (harmonics * 0.24f + rasp) * envelope;
        }

        string soundName = mobIndex == 0 ? "Zombie" : mobIndex == 1 ? "Witch" : "Ghoul";
        AudioClip clip = AudioClip.Create(
            soundName + (isDeathSound ? " Death" : " Vocalization"),
            sampleCount,
            1,
            sampleRate,
            false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateCreepyDrone()
    {
        const int sampleRate = 22050;
        const int durationSeconds = 12;
        int sampleCount = sampleRate * durationSeconds;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float swell = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * time / 4f);
            float drone = Mathf.Sin(2f * Mathf.PI * 43f * time) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 55f * time + 0.3f) * 0.3f +
                Mathf.Sin(2f * Mathf.PI * 65f * time + 1.1f) * 0.2f;
            samples[i] = drone * swell * 0.35f;
        }

        AudioClip clip = AudioClip.Create("Creepy Background Drone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void CheckClip(AudioClip clip, string resourceName)
    {
        if (clip == null)
            Debug.LogWarning("Missing audio resource: " + resourceName);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
