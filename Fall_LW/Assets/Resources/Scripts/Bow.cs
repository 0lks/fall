using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bow : Weapon
{
    GameObject arrowPrefab;
    public float force;

    // For the arrow shot animation
    Vector3 enemyDirection;
    GameObject discardPool;
    Hex enemyLocation;

    GameObject arrow;

    private new void Awake()
    {
        base.Awake();
        correctRotationInEuler = new Vector3(6.837f, 68.21101f, 166.318f);
        correctPosition = new Vector3(0.445f, -0.01f, 0.194f);
        arrowPrefab = (GameObject) Resources.Load("Prefabs/Weapons/Arrow");
        damage = stats.damage;
        attackDistance = stats.attackDistance;
        damageBonusModifier = stats.damageBonusModifier;
    }
    private void Start()
    {
        base.Awake();
        playerAnimator = player.GetComponent<Animator>();
        weaponAnimator = GetComponent<Animator>();
        discardPool = GameControl.discardPool;
    }

    public override bool Target(Character enemy)
    {
        enemyLocation = enemy.currentPosition;
        //Debug.DrawLine(transform.position,enemyLocation.occupyingCharacter.transform.position,Color.blue,Mathf.Infinity);
        weaponAnimator.Play("DrawAndRelease");
        playerAnimator.Play("ShootBow");
        playerAnimator.SetBool("ShootingBow", true);

        enemyDirection = enemyLocation.occupyingCharacter.rb.worldCenterOfMass
            - transform.position;

        Vector3 missDirection;
        float RNG = Random.Range(0f, 100f);
        List<Vector3> missDirections = new List<Vector3>();
        missDirections.Add(Vector3.up);
        missDirections.Add(Vector3.down);
        missDirections.Add(Vector3.right);
        missDirections.Add(Vector3.left);

        if (RNG > enemy.currentPosition.chanceToHit)
        {
            Debug.Log("Arrow is supposed to miss");
            missDirection = missDirections[Random.Range(0, missDirections.Count)] * 5;
        }
        else
        {
            Debug.Log("Arrow is supposed to hit");
            missDirection = Vector3.zero;
        }

        Vector3 pos = transform.position;
        Quaternion arrowRotation = Quaternion.LookRotation(enemyDirection + missDirection, Vector3.up);
        arrow = Instantiate(arrowPrefab, pos, arrowRotation, discardPool.transform);
        if (missDirection == Vector3.zero) arrow.GetComponent<Arrow>().IgnoreTerrainObjectsAndTerrain();
        else arrow.GetComponent<Arrow>().IgnoreCharacters();
        arrow.SetActive(false); //to hide the arrow before the keyframe event

        return true;
    }

    public override void Equip()
    {
        Hand equipTarget = player.transform.GetComponentInChildren<Hand>();
        player.wieldedWeapon = this;
        equipTarget.PlaceWeaponInHand(this);
    }


    public void ShootArrow()
    // Called by: Bow animation event keyframe
    {
        arrow.SetActive(true);
        arrow.GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * force);
    }
}
