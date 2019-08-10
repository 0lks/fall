using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Internal dependencies
using FALL.Core;

namespace FALL.Items.Weapons {
    public class Bow : Weapon
    {
        GameObject arrowPrefab;
        public float force;

        // For the arrow shot animation
        GameObject discardPool;
        Arrow arrow;

        private new void Awake()
        {
            //base.Awake();
            correctRotationInEuler = new Vector3(6.837f, 68.21101f, 166.318f);
            correctPosition = new Vector3(0.445f, -0.01f, 0.194f);
            arrowPrefab = (GameObject)Resources.Load("Prefabs/Weapons/Arrow");
            damage = stats.damage;
            attackDistance = stats.attackDistance;
            damageBonusModifier = stats.damageBonusModifier;
        }
        private void Start()
        {
            base.Awake();
            weaponAnimator = GetComponent<Animator>();
            discardPool = GameControl.discardPool;
        }

        public override void AttackBehaviour(Vector3 enemyPos, float chanceToHit)
        {
            Vector3 shift = CalculateMiss(chanceToHit);
            arrow = SpawnArrow(enemyPos, shift);
            PlayAttackAnimation();
        }

        public override void PlayAttackAnimation()
        {
            weaponAnimator.Play("DrawAndRelease");
        }

        public void ShootArrow()
        // Called by: Bow animation event keyframe
        {
            arrow.gameObject.SetActive(true);
            arrow.GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * force);
            arrow.inFlight = true;
        }

        Vector3 CalculateMiss(float chanceToHit)
        {
            Vector3 shift;
            float RNG = Random.Range(0f, 100f);
            List<Vector3> missDirections = new List<Vector3>();
            missDirections.Add(Vector3.up);
            missDirections.Add(Vector3.down);
            missDirections.Add(Vector3.right);
            missDirections.Add(Vector3.left);

            if (RNG > chanceToHit)
            {
                //Debug.Log("Arrow is supposed to miss");
                shift = missDirections[Random.Range(0, missDirections.Count)] * 5;
            }
            else
            {
                //Debug.Log("Arrow is supposed to hit");
                shift = Vector3.zero;
            }

            Debug.Log(shift);
            return shift;
        }

        Arrow SpawnArrow(Vector3 enemyPos, Vector3 shift)
        {
            Vector3 enemyDirection = enemyPos - transform.position;
            Quaternion arrowRotation = Quaternion.LookRotation(enemyDirection + shift, Vector3.up);
            GameObject _arrow = Instantiate(arrowPrefab, transform.position, arrowRotation, discardPool.transform);
            if (shift == Vector3.zero)
            {
                print("IgnoreTerrainObjectsAndTerrain()");
                _arrow.GetComponent<Arrow>().IgnoreTerrainObjectsAndTerrain();
            }
            else
            {
                print("IgnoreCharacters()");
                _arrow.GetComponent<Arrow>().IgnoreCharacters();
            }
            _arrow.SetActive(false); //to hide the arrow before the keyframe event
            return _arrow.GetComponent<Arrow>();
        }
    }
}
