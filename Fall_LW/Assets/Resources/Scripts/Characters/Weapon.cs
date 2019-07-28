using UnityEngine;

// Internal dependencies
using FALL.Core;
using FALL.Characters;

namespace FALL.Items.Weapons {
public abstract class Weapon : Item
{
    public WeaponDATA stats;
    //protected Player player;
    protected Animator weaponAnimator;
    public Vector3 correctRotationInEuler;
    public Vector3 correctPosition;
    public int attackDistance;
    public float damageBonusModifier;
    public float damage;


    protected void Awake()
    {
        //player = GameControl.player;
    }

    public abstract void EquipTo(Character equipTarget);

    public abstract bool AttackTarget(Character target);
}}
