using UnityEngine;

public abstract class Weapon : Item
{
    public WeaponDATA stats;
    protected Player player;
    protected Animator playerAnimator;
    protected Animator weaponAnimator;
    public Vector3 correctRotationInEuler;
    public Vector3 correctPosition;
    public int attackDistance;
    public float damageBonusModifier;
    public float damage;

    protected void Awake()
    {
        player = GameControl.player;
    }

    public abstract void Equip();

    public abstract bool Target(Character enemy);
}
