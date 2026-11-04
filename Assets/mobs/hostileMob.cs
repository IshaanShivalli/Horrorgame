using UnityEngine;

[DisallowMultipleComponent]
public abstract class hostileMob : MonoBehaviour
{
    private const float ThinkInterval = 0.15f;
    private float thinkTimer;
    private float attackTimer;
    private float vocalizationTimer;
    private float confusionDuration;
    private float poisonCloudCooldown;
    private float verticalSpeed;
    private float health;
    private float hitFlashTimer;
    private CharacterController controller;
    private AudioSource mobAudio;
    private Renderer[] mobRenderers;
    private Color[][] originalRendererColors;
    private int[][] rendererColorProperties;
    private MaterialPropertyBlock hitFlashBlock;

    protected world CurrentWorld { get; private set; }
    protected player TargetPlayer { get; private set; }
    protected abstract float MaximumHealth { get; }
    protected abstract float AttackDamage { get; }
    protected abstract float AttackCooldown { get; }
    protected abstract float MovementSpeed { get; }
    protected abstract bool CanFly { get; }
    protected virtual bool IgnoreTargetCollision => false;
    protected virtual float MaximumStepHeight => 1.05f;
    public abstract string DisplayName { get; }

    public void Initialize(world owningWorld, player target)
    {
        CurrentWorld = owningWorld;
        TargetPlayer = target;
        controller = GetComponent<CharacterController>();
        if (controller == null)
            throw new MissingComponentException("A hostile mob requires a CharacterController.");
        if (IgnoreTargetCollision && target != null)
        {
            CharacterController targetController = target.GetComponent<CharacterController>();
            if (targetController != null)
                Physics.IgnoreCollision(controller, targetController, true);
        }
        if (!CanFly)
            controller.stepOffset = Mathf.Min(controller.height - 0.1f, MaximumStepHeight);
        health = MaximumHealth;
        hitFlashBlock = new MaterialPropertyBlock();
        CacheMobRenderers();
        mobAudio = GetComponent<AudioSource>();
        if (mobAudio == null)
            mobAudio = gameObject.AddComponent<AudioSource>();
        mobAudio.spatialBlend = 1f;
        mobAudio.minDistance = 2f;
        mobAudio.maxDistance = 30f;
        mobAudio.rolloffMode = AudioRolloffMode.Linear;
        vocalizationTimer = Random.Range(4f, 10f);
        PlayVocalization();
    }

    protected virtual void Update()
    {
        if (CurrentWorld == null || TargetPlayer == null || TargetPlayer.IsDead ||
            CurrentWorld.IsPaused)
            return;

        UpdateHitFlash();
        float distanceFromPlayer = (TargetPlayer.transform.position - transform.position).sqrMagnitude;
        if (distanceFromPlayer > 10000f)
        {
            Destroy(gameObject);
            return;
        }

        thinkTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;
        confusionDuration = Mathf.Max(0f, confusionDuration - Time.deltaTime);
        poisonCloudCooldown = Mathf.Max(0f, poisonCloudCooldown - Time.deltaTime);
        vocalizationTimer -= Time.deltaTime;
        if (vocalizationTimer <= 0f)
        {
            vocalizationTimer = Random.Range(8f, 16f);
            PlayVocalization();
        }
        if (thinkTimer > 0f)
            return;

        float deltaTime = ThinkInterval + thinkTimer;
        thinkTimer = ThinkInterval;
        if (confusionDuration > 0f)
        {
            Vector3 awayFromPlayer = transform.position - TargetPlayer.transform.position;
            if (awayFromPlayer.sqrMagnitude < 0.01f)
                awayFromPlayer = transform.forward;
            MoveToward(transform.position + awayFromPlayer.normalized * 8f, deltaTime);
            return;
        }
        Think(deltaTime);
    }

    protected virtual void Think(float deltaTime)
    {
        Vector3 offset = TargetPlayer.transform.position - transform.position;
        Vector3 horizontalOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        float distance = horizontalOffset.magnitude;
        float chaseRange = 20f;

        if (distance > chaseRange)
        {
            return;
        }

        if (distance > 0.1f)
        {
            Quaternion look = Quaternion.LookRotation(horizontalOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 4f * deltaTime);
        }

        if (distance <= AttackRange && Mathf.Abs(offset.y) <= 1.8f)
            TryAttackPlayer();
        else
            MoveToward(TargetPlayer.transform.position, deltaTime);
    }

    protected virtual float AttackRange => 1.45f;

    protected void MoveToward(Vector3 destination, float deltaTime)
    {
        Vector3 offset = destination - transform.position;
        Vector3 direction;
        if (CanFly)
        {
            direction = offset.sqrMagnitude > 0.01f ? offset.normalized : Vector3.zero;
            Vector3 velocity = direction * MovementSpeed;
            controller.Move(velocity * deltaTime);
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 5f * deltaTime);
            }
            return;
        }

        direction = Vector3.ProjectOnPlane(offset, Vector3.up);
        direction = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;

        float comfortDistance = 3f;
        float currentDistance = offset.sqrMagnitude > 0.01f ? Mathf.Sqrt(offset.sqrMagnitude) : 0f;

        if (controller.isGrounded)
            verticalSpeed = -1f;
        else
            verticalSpeed -= 20f * deltaTime;

        if (currentDistance > comfortDistance)
        {
            Vector3 movement = direction * MovementSpeed;
            movement.y = verticalSpeed;
            controller.Move(movement * deltaTime);
        }
        else
        {
            Vector3 awayFromPlayer = transform.position - TargetPlayer.transform.position;
            float awayDistance = awayFromPlayer.sqrMagnitude;
            if (awayDistance > 0.01f)
            {
                awayFromPlayer.Normalize();
                Vector3 moveAway = awayFromPlayer * (MovementSpeed * 0.5f) * deltaTime;
                controller.Move(moveAway);
            }
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 4f * deltaTime);
        }
    }

    protected void DashToward(Vector3 direction, float speed, float deltaTime)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();
        controller.Move(direction * (speed * deltaTime));
        Quaternion look = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * deltaTime);
    }

    protected bool TryAttackPlayer()
    {
        if (attackTimer > 0f)
            return false;

        TargetPlayer.TakeDamage(AttackDamage, DisplayName);
        attackTimer = AttackCooldown;
        PlayVocalization();
        return true;
    }

    protected void PlayVocalization()
    {
        GameAudio.PlayMobSound(mobAudio, DisplayName);
    }

    public void ApplyConfusion(float duration)
    {
        confusionDuration = Mathf.Max(confusionDuration, duration);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || health <= 0f)
            return;

        ShowHitEffect();
        health = Mathf.Max(0f, health - amount);
        if (health <= 0f)
        {
            CurrentWorld.OnMobKilled(this);
            Destroy(gameObject);
        }
    }

    private void CacheMobRenderers()
    {
        mobRenderers = GetComponentsInChildren<Renderer>(true);
        originalRendererColors = new Color[mobRenderers.Length][];
        rendererColorProperties = new int[mobRenderers.Length][];

        for (int rendererIndex = 0; rendererIndex < mobRenderers.Length; rendererIndex++)
        {
            Material[] materials = mobRenderers[rendererIndex].sharedMaterials;
            originalRendererColors[rendererIndex] = new Color[materials.Length];
            rendererColorProperties[rendererIndex] = new int[materials.Length];
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                int colorProperty = material != null && material.HasProperty("_BaseColor")
                    ? Shader.PropertyToID("_BaseColor")
                    : material != null && material.HasProperty("_Color")
                        ? Shader.PropertyToID("_Color")
                        : 0;
                rendererColorProperties[rendererIndex][materialIndex] = colorProperty;
                if (colorProperty != 0)
                    originalRendererColors[rendererIndex][materialIndex] = material.GetColor(colorProperty);
            }
        }
    }

    private void ShowHitEffect()
    {
        hitFlashTimer = 0.14f;
        Color bloodColor = default;
        bool emitsBlood = false;
        if (this is zombies)
        {
            bloodColor = new Color(0.8f, 0.045f, 0.035f, 1f);
            emitsBlood = true;
        }
        else if (this is witches)
        {
            bloodColor = new Color(0.12f, 0.85f, 0.14f, 1f);
            emitsBlood = true;
        }

        Vector3 effectPosition = transform.position + Vector3.up * 0.9f;
        if (emitsBlood)
            MobEffects.EmitBlood(CurrentWorld, effectPosition, bloodColor);
        else if (this is ghouls && poisonCloudCooldown <= 0f)
        {
            MobEffects.SpawnPoisonCloud(CurrentWorld, effectPosition);
            poisonCloudCooldown = 3.2f;
        }

        SetHitFlash(0.75f);
    }

    private void UpdateHitFlash()
    {
        if (hitFlashTimer <= 0f)
            return;

        hitFlashTimer = Mathf.Max(0f, hitFlashTimer - Time.deltaTime);
        SetHitFlash(hitFlashTimer > 0f ? 0.75f : 0f);
    }

    private void SetHitFlash(float strength)
    {
        if (hitFlashBlock == null || mobRenderers == null ||
            originalRendererColors == null || rendererColorProperties == null)
            return;

        int rendererCount = Mathf.Min(
            mobRenderers.Length,
            Mathf.Min(originalRendererColors.Length, rendererColorProperties.Length));
        for (int rendererIndex = 0; rendererIndex < rendererCount; rendererIndex++)
        {
            Renderer mobRenderer = mobRenderers[rendererIndex];
            if (mobRenderer == null ||
                originalRendererColors[rendererIndex] == null ||
                rendererColorProperties[rendererIndex] == null)
                continue;

            int materialCount = Mathf.Min(
                originalRendererColors[rendererIndex].Length,
                rendererColorProperties[rendererIndex].Length);
            for (int materialIndex = 0;
                materialIndex < materialCount;
                materialIndex++)
            {
                int colorProperty = rendererColorProperties[rendererIndex][materialIndex];
                if (colorProperty == 0)
                    continue;

                Color originalColor = originalRendererColors[rendererIndex][materialIndex];
                Color hitColor = new Color(1f, 0.08f, 0.08f, originalColor.a);
                hitFlashBlock.Clear();
                mobRenderer.GetPropertyBlock(hitFlashBlock, materialIndex);
                hitFlashBlock.SetColor(colorProperty, Color.Lerp(originalColor, hitColor, strength));
                mobRenderer.SetPropertyBlock(hitFlashBlock, materialIndex);
            }
        }
    }
}
