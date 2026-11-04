using UnityEngine;

[DisallowMultipleComponent]
public abstract class hostileMob : MonoBehaviour
{
    private const float ThinkInterval = 0.15f;
    private float thinkTimer;
    private float attackTimer;
    private float verticalSpeed;
    private float health;
    private CharacterController controller;

    protected world CurrentWorld { get; private set; }
    protected player TargetPlayer { get; private set; }
    protected abstract float MaximumHealth { get; }
    protected abstract float AttackDamage { get; }
    protected abstract float AttackCooldown { get; }
    protected abstract float MovementSpeed { get; }
    protected abstract bool CanFly { get; }
    protected virtual float MaximumStepHeight => 0.45f;
    public abstract string DisplayName { get; }

    public void Initialize(world owningWorld, player target)
    {
        CurrentWorld = owningWorld;
        TargetPlayer = target;
        controller = GetComponent<CharacterController>();
        if (controller == null)
            throw new MissingComponentException("A hostile mob requires a CharacterController.");
        if (!CanFly)
            controller.stepOffset = Mathf.Min(controller.stepOffset, MaximumStepHeight);
        health = MaximumHealth;
    }

    protected virtual void Update()
    {
        if (CurrentWorld == null || TargetPlayer == null || TargetPlayer.IsDead ||
            CurrentWorld.IsPaused)
            return;

        float distanceFromPlayer = (TargetPlayer.transform.position - transform.position).sqrMagnitude;
        if (distanceFromPlayer > 10000f)
        {
            Destroy(gameObject);
            return;
        }

        thinkTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;
        if (thinkTimer > 0f)
            return;

        float deltaTime = ThinkInterval + thinkTimer;
        thinkTimer = ThinkInterval;
        Think(deltaTime);
    }

    protected virtual void Think(float deltaTime)
    {
        Vector3 offset = TargetPlayer.transform.position - transform.position;
        Vector3 horizontalOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        float distance = horizontalOffset.magnitude;
        if (distance > 0.1f)
        {
            Quaternion look = Quaternion.LookRotation(horizontalOffset);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 8f * deltaTime);
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
        if (controller.isGrounded)
            verticalSpeed = -1f;
        else
            verticalSpeed -= 20f * deltaTime;
        Vector3 movement = direction * MovementSpeed;
        movement.y = verticalSpeed;
        controller.Move(movement * deltaTime);
    }

    protected bool TryAttackPlayer()
    {
        if (attackTimer > 0f)
            return false;

        TargetPlayer.TakeDamage(AttackDamage, DisplayName);
        attackTimer = AttackCooldown;
        GameAudio.PlayAttack();
        return true;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || health <= 0f)
            return;

        health = Mathf.Max(0f, health - amount);
        if (health <= 0f)
        {
            CurrentWorld.OnMobKilled(this);
            Destroy(gameObject);
        }
    }
}
