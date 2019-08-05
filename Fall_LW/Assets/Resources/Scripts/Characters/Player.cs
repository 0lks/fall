using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Internal dependencies
using FALL.Core;
//using FALL.Items.Weapons;

namespace FALL.Characters {
    public class Player : Character
    {
        [SerializeField] PlayerDATA _stats;
        [HideInInspector] public SimpleHealthBar healthBar;
        [HideInInspector] public List<Hex> attackableHexes;
        [HideInInspector] public List<Hex> highlightedNeighbours;
        public PlayerManager playerManager;

        /*
         * Getters for initial values specific to Player
        */
        public int GetBaseExploreMovementAmount() { return _stats.baseExploreMovementAmount; }

        protected new void Awake()
        {
            base.stats = _stats;
            base.Awake();
            GameControl.player = this;
            playerManager = GetComponent<PlayerManager>();
        }

        // Stores the step lost when going from upright movement to sneak movement with an odd number of steps left
        [HideInInspector] public bool extraStep;
        [HideInInspector] public bool sneaking = false;

        public override void MoveTo(Hex target)
        {
            if (target.blocked || target == currentPosition) return;
            GameControl.canvas.DisableButtons();
            GameControl.gameControl.DisableMouse();

            //movementAmount -= GameControl.movePath.Count - 1;
            movementAmount -= GameControl.movePath.Count;
            //GameControl.movePath.Dequeue(); //TODO: The first element is the current location - fix the problem in Graph.
            StartCoroutine(MoveCoroutine(GameControl.movePath));
            GameControl.gameControl.DisableMouse();
        }

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
                        if ((sneaking && hex.DistanceTo(enemy.currentPosition) <= enemy.GetSneakingPlayerDetectDist())
                            || !sneaking && hex.DistanceTo(enemy.currentPosition) <= enemy.GetPlayerDetectDist())
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

        public override void Die()
        {
            currentPosition.occupyingCharacter = null;
            GameControl.turnController.RemoveFromQueue(this);
            GameControl.canvas.DisplayDeathScreen();
            transform.gameObject.SetActive(false);
            GameControl.editingMouse.enabled = false;
            GameControl.playMouse.enabled = false;
        }
    }
}

