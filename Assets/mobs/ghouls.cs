using UnityEngine;

public sealed class ghouls : hostileMob
{
    private static Material hide;
    private static Material wings;
    private static Material eyes;
    private readonly Collider[] nearbyTargets = new Collider[8];
    private float scanTimer;

    protected override float MaximumHealth => 10f;
    protected override float AttackDamage => 2f;
    protected override float AttackCooldown => 0.9f;
    protected override float MovementSpeed => 3.2f;
    protected override bool CanFly => true;
    protected override float AttackRange => 1.6f;
    public override string DisplayName => "ghoul";

    private void Awake()
    {
        hide = MobModels.Material(hide, "Ghoul hide", new Color(0.16f, 0.13f, 0.2f));
        wings = MobModels.Material(wings, "Ghoul wings", new Color(0.25f, 0.12f, 0.2f));
        eyes = MobModels.Material(eyes, "Ghoul eyes", new Color(1f, 0.12f, 0.04f));
        MobModels.CreateGhoul(transform, hide, wings, eyes);
    }

    protected override void Think(float deltaTime)
    {
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
                    target.TakeDamage(AttackDamage * 2f);
                    GameAudio.PlayAttack();
                    break;
                }
            }
        }

        Vector3 targetPosition = TargetPlayer.transform.position + Vector3.up * 0.8f;
        Vector3 offset = targetPosition - transform.position;
        if (offset.magnitude <= AttackRange)
            TryAttackPlayer();
        else
            MoveToward(targetPosition, deltaTime);
    }
}
