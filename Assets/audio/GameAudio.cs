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
        if (source == null || instance == null || instance.attackClip == null)
            return;

        float basePitch = mobName == "ghoul" ? 0.72f : mobName == "witch" ? 1.15f : 0.9f;
        source.pitch = basePitch * Random.Range(0.92f, 1.08f);
        source.PlayOneShot(instance.attackClip, 0.45f);
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
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.spatialBlend = 0f;

        normalMusic = Resources.Load<AudioClip>("audio/night_ambience");
        redMoonMusic = Resources.Load<AudioClip>("audio/red_moon");
        attackClip = Resources.Load<AudioClip>("audio/monster_attack");
        purchaseClip = Resources.Load<AudioClip>("audio/purchase");
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
