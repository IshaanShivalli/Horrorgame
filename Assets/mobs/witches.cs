using UnityEngine;

public sealed class witches : hostileMob
{
    private static Material robe;
    private static Material skin;
    private static Material eyes;
    private float spellTimer = 1.5f;

    protected override float MaximumHealth => 30f;
    protected override float AttackDamage => 4f;
    protected override float AttackCooldown => 2.4f;
    protected override float MovementSpeed => 1.15f;
    protected override bool CanFly => false;
    protected override float AttackRange => 12f;
    public override string DisplayName => "witch";

    private void Awake()
    {
        robe = MobModels.Material(robe, "Witch robe", new Color(0.19f, 0.08f, 0.3f));
        skin = MobModels.Material(skin, "Witch skin", new Color(0.52f, 0.57f, 0.4f));
        eyes = MobModels.Material(eyes, "Witch eyes", new Color(0.95f, 0.24f, 0.1f));
        MobModels.CreateWitch(transform, skin, robe, eyes);
    }

    protected override void Think(float deltaTime)
    {
        spellTimer -= deltaTime;
        Vector3 offset = TargetPlayer.transform.position - transform.position;
        float distance = offset.magnitude;
        if (distance > 8f)
        {
            MoveToward(TargetPlayer.transform.position, deltaTime);
            return;
        }

        Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
        if (horizontal.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(horizontal),
                6f * deltaTime);

        if (spellTimer <= 0f)
        {
            TargetPlayer.TakeDamage(AttackDamage, DisplayName);
            spellTimer = AttackCooldown;
            GameAudio.PlayAttack();
        }
    }
}
