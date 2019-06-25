using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    Rigidbody rb;

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
        Physics.IgnoreLayerCollision(10, 12, true);
    }
    public void IgnoreTerrainObjectsAndTerrain()
    {
        Physics.IgnoreLayerCollision(12, 14, true);
        Physics.IgnoreLayerCollision(12, 11, true);
        Physics.IgnoreLayerCollision(12, 18, true);
    }

    public void ResetCollision()
    {
        Physics.IgnoreLayerCollision(10, 12, false);
        Physics.IgnoreLayerCollision(12, 14, false);
        Physics.IgnoreLayerCollision(12, 11, false);
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag == "Player") return;

        // Freeze the arrow in place
        rb.isKinematic = true;
        GetComponent<SphereCollider>().enabled = false;

        Enemy enemy = collision.transform.GetComponentInParent<Enemy>();
        if (enemy)
        // Shot didn't miss
        {
            float modifier = 1f;
            if (!enemy.hasDetectedPlayer) modifier = GameControl.player.wieldedWeapon.damageBonusModifier; 
            enemy.remainingHealth -= GameControl.player.wieldedWeapon.damage * modifier;

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

        ResetCollision();
        GameControl.turnController.NextActorTurn();
    }
}
