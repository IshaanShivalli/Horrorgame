using UnityEngine;

public sealed class zombies : hostileMob
{
    private static Material skin;
    private static Material shirt;
    private static Material trousers;
    private static Material eyes;

    protected override float MaximumHealth => 14f;
    protected override float AttackDamage => 3f;
    protected override float AttackCooldown => 1.3f;
    protected override float MovementSpeed => 1.65f;
    protected override bool CanFly => false;
    protected override float MaximumStepHeight => 1.05f;
    public override string DisplayName => "zombie";

    private void Awake()
    {
        skin = MobModels.Material(skin, "Zombie skin", new Color(0.34f, 0.46f, 0.3f));
        shirt = MobModels.Material(shirt, "Torn zombie shirt", new Color(0.25f, 0.19f, 0.16f));
        trousers = MobModels.Material(trousers, "Zombie trousers", new Color(0.12f, 0.16f, 0.19f));
        eyes = MobModels.Material(eyes, "Zombie eyes", new Color(0.85f, 0.13f, 0.08f));
        MobModels.CreateZombie(transform, skin, shirt, trousers, eyes);
    }
}
