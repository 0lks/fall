using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Internal dependencies
using FALL.Core;
//using FALL.Items.Weapons;

namespace FALL.Characters {
    public class Player : Character
    {
        //EquipmentManager equipmentManager;
        //[HideInInspector] public Weapon wieldedWeapon;
        [HideInInspector] public SimpleHealthBar healthBar;
        [HideInInspector] public List<Hex> attackableHexes;
        [HideInInspector] public List<Hex> highlightedNeighbours;
        public PlayerManager playerManager;

        protected new void Awake()
        {
            base.Awake();
            GameControl.player = this;
            playerManager = GetComponent<PlayerManager>();
        }

        // Stores the step lost when going from upright movement to sneak movement with an odd number of steps left
        [HideInInspector] public bool extraStep;
        [HideInInspector] public bool sneaking = false;

        public void Sneak()
        {
            if (sneaking)
            {
                if (!extraStep) movementAmount *= 2;
                else
                {
                    movementAmount = movementAmount * 2 + 1;
                }
                animator.SetFloat("MovementSpeed", 20f);
            }
            else
            {
                animator.SetFloat("MovementSpeed", 7f);
                if (movementAmount % 2 == 0)
                // The player has an even number of steps left
                {
                    extraStep = false;
                    movementAmount /= 2;
                }
                else
                // The player has an odd number of steps left
                {
                    extraStep = true;
                    movementAmount = Mathf.FloorToInt(movementAmount / 2);
                }
            }
            sneaking = !sneaking;
            GameControl.NewPlayerState(GameControl.PlayerState.Move);
            animator.SetBool("Sneaking", sneaking);
        }

        public void UnHighLightSurrounding()
        // A more economical alternative to UnHighLightEverything() in the Map class
        {
            foreach (Hex hex in highlightedNeighbours)
            {
                hex.Unhighlight();
            }
            highlightedNeighbours.Clear();
        }

        public bool Attack(Character character)
        {
            if (currentPosition.DistanceTo(character.currentPosition) > playerManager.wieldedWeapon.attackDistance ||
                !attackableHexes.Contains(character.currentPosition)) return false;

            movementAmount = 0;
            RotateTowards(character.transform);
            playerManager.wieldedWeapon.AttackTarget(character);
            playerManager.playerAnimator.SetBool("ShootBow", true);
            return true;
        }

        public void DetermineTriggerZone(List<Hex> candidates)
        // Determines which hexes will trigger an enemy
        {
            if (GameControl.allEnemies == null) return;

            List<Enemy> nearbyEnemies = GameControl.nearbyEnemies;

            foreach (Hex hex in candidates)
            {
                foreach (Enemy enemy in nearbyEnemies)
                {
                    if (!enemy.hasDetectedPlayer)
                    {
                        if ((sneaking && hex.DistanceTo(enemy.currentPosition) <= enemy.stats.sneakingPlayerDetectDist)
                            || !sneaking && hex.DistanceTo(enemy.currentPosition) <= enemy.stats.defaultPlayerDetectDist)
                        {
                            hex.inEnemyRange = true;
                        }
                    }
                }
            }
        }

        public override void RefreshStats()
        {
            base.RefreshStats();
            GameControl.playedSoundThisTurn = false;
            GameControl.NewPlayerState(GameControl.PlayerState.Move);
            if (sneaking)
            // If the player was sneaking at the end of last turn, don't reset movement amount to full
            {
                sneaking = false;
                Sneak();
            }
        }
    }
}

