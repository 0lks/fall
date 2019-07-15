using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public bool handSide; //0 -> left; 1 -> right
    public Weapon weapon;

    public void PlaceWeaponInHand(Weapon weapon)
    {
        weapon.transform.SetParent(transform);
        weapon.transform.localPosition = weapon.correctPosition;
        weapon.transform.localRotation = Quaternion.Euler(weapon.correctRotationInEuler);
    } 
}
