using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Internal dependencies
using FALL.Core;
using FALL.Characters;

namespace FALL.Items.Weapons {
    public class Arrow : MonoBehaviour
    {
        Rigidbody rb;
        float lifeTime = 3f;
        public bool inFlight = false;

        private void Update()
        {
            if (inFlight) ReduceLifetime();
        }

        private void ReduceLifetime()
        {
            if (lifeTime < 0) Destroy(this.gameObject);
            else lifeTime -= Time.deltaTime;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnBecameInvisible()
        {
            Destroy(this);
        }

        public void IgnoreCharacters()
        {
            Physics.IgnoreLayerCollision(12, 14, false);
            Physics.IgnoreLayerCollision(12, 11, false);
            Physics.IgnoreLayerCollision(12, 18, false);
            Physics.IgnoreLayerCollision(12, 10, true);
            Physics.IgnoreLayerCollision(12, 19, true);
        
        }
        public void IgnoreTerrainObjectsAndTerrain()
        {
            Physics.IgnoreLayerCollision(12, 14, true);
            Physics.IgnoreLayerCollision(12, 11, true);
            Physics.IgnoreLayerCollision(12, 18, true);
            Physics.IgnoreLayerCollision(12, 10, false);
            Physics.IgnoreLayerCollision(12, 19, false);
        }
        /*
        public void ResetCollision()
        {
            Physics.IgnoreLayerCollision(10, 12, false);
            Physics.IgnoreLayerCollision(12, 14, false);
            Physics.IgnoreLayerCollision(12, 11, false);
            Physics.IgnoreLayerCollision(10, 19, false);
        }
        */
        private void OnTriggerEnter(Collider collision)
        {
            Debug.Log("Arrow trigger event");
            if (collision.tag == "Player") return;

            // Freeze the arrow in place
            rb.isKinematic = true;
            GetComponent<SphereCollider>().enabled = false;

            Enemy enemy = collision.transform.GetComponentInParent<Enemy>();
            if (enemy)
            // Shot didn't miss
            {
                float modifier = 1f;
                if (!enemy.hasDetectedPlayer) modifier = GameControl.player.weapon.damageBonusModifier; 
                enemy.remainingHealth -= GameControl.player.weapon.damage * modifier;

                if (enemy.remainingHealth <= 0)
                {
                    enemy.Die();
                    if (GameControl.allEnemies.Count <= 0) GameControl.canvas.DisplayVictoryScreen();
                    Destroy(gameObject);
                }

                else
                {
                    transform.SetParent(enemy.GetComponentInChildren<SkinnedMeshRenderer>().rootBone.transform); //WOLF
                    enemy.HasDetectedPlayer();
                }
            }

            //ResetCollision();
            GameControl.turnController.NextActorTurn();
        }
    }
}
