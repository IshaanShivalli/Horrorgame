using UnityEngine;

public sealed class ghouls : hostileMob
{
    private static Material hide;
    private static Material eyes;
    private readonly Collider[] nearbyTargets = new Collider[8];
    private float scanTimer;
    private float dashCooldown;
    private float dashDuration;
    private Vector3 dashDirection;

    protected override float MaximumHealth => 10f;
    protected override float AttackDamage => 2f;
    protected override float AttackCooldown => 0.9f;
    protected override float MovementSpeed => 3.2f;
    protected override bool CanFly => true;
    protected override float AttackRange => 1.8f;
    protected override bool IgnoreTargetCollision => true;
    public override string DisplayName => "ghoul";

    private void Awake()
    {
        hide = MobModels.Material(hide, "Ghoul hide", new Color(0.16f, 0.13f, 0.2f));
        eyes = MobModels.Material(eyes, "Ghoul eyes", new Color(1f, 0.12f, 0.04f));
        MobModels.CreateGhoul(transform, hide, eyes);
    }

    protected override void Think(float deltaTime)
    {
        dashCooldown = Mathf.Max(0f, dashCooldown - deltaTime);
        dashDuration = Mathf.Max(0f, dashDuration - deltaTime);

        scanTimer -= deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = 0.55f;
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(
                transform.position,
                1.3f,
                nearbyTargets,
                ~0,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                zombies target = nearbyTargets[i].GetComponentInParent<zombies>();
                if (target != null && target.isActiveAndEnabled)
                {
                    target.TakeDamage(4f);
                    PlayVocalization();
                    break;
                }
            }
        }

        Vector3 targetPosition = TargetPlayer.transform.position + Vector3.up * 0.9f;
        Vector3 offset = targetPosition - transform.position;
        Vector3 horizontalOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
        float distance = horizontalOffset.magnitude;

        if (dashDuration > 0f)
        {
            DashToward(dashDirection, 11f, deltaTime);
            if (offset.magnitude <= AttackRange + 0.65f && Mathf.Abs(offset.y) <= 1.8f)
            {
                dashDuration = 0f;
                AttackWithKnockback();
                dashCooldown = 1.8f;
            }
            return;
        }

        if (distance > AttackRange + 0.65f && distance <= 5.5f && dashCooldown <= 0f)
        {
            dashDirection = offset.normalized;
            dashDuration = 0.45f;
            return;
        }

        if (distance <= AttackRange && Mathf.Abs(offset.y) <= 1.8f)
        {
            AttackWithKnockback();
            return;
        }

        MoveToward(targetPosition, deltaTime);
    }

    private void AttackWithKnockback()
    {
        if (!TryAttackPlayer())
            return;

        Vector3 pushDirection = Vector3.ProjectOnPlane(
            TargetPlayer.transform.position - transform.position,
            Vector3.up);
        if (pushDirection.sqrMagnitude < 0.01f)
            pushDirection = transform.forward;
        TargetPlayer.ApplyKnockback(pushDirection.normalized * 7f + Vector3.up * 1.5f);
    }
}
