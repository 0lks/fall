using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDATA", menuName = "Items/WeaponDATA")]
public class WeaponDATA : ItemDATA
{
    public string weaponName;
    public float damage;
    public float damageBonusModifier;
    public int attackDistance;
    public WeaponClass weaponClass;
    public UseCase useCase;
}

public enum WeaponClass { Bow, Musket, Tomahawk, Sword }
public enum UseCase { Ranged, Throwable, Melee }
