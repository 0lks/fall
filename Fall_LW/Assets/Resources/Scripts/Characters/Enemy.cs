using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Internal dependencies
using FALL.Core;

namespace FALL.Characters {
    public class Enemy : Character
    {
        public bool hasDetectedPlayer { get; private set; }

        protected new void Awake()
        {
            base.Awake();
            hasDetectedPlayer = animator.GetBool("HasDetectedPlayer");
        }

        public void MoveRandomly()
        {
            List<Hex> options = currentPosition.GetDistantNeighboursConnected(movementAmount);
            options.Remove(currentPosition);
            options.RemoveAll(Hex.Occupied);
            int randomIndex = Random.Range(0, options.Count);
            MoveTo(options[randomIndex]);
            StartCoroutine(WaitForMovement(options[randomIndex]));
        }

        public bool CanDetectPlayer()
        {
            Hex playerLocation = GameControl.player.currentPosition;
            if (((currentPosition.DistanceTo(playerLocation) <= stats.defaultPlayerDetectDist) && !GameControl.player.sneaking)
                || ((currentPosition.DistanceTo(playerLocation) <= stats.sneakingPlayerDetectDist) && GameControl.player.sneaking))
            {
                return true;
            }
            else return false;
        }

        public void HasDetectedPlayer()
        {
            hasDetectedPlayer = true;
            if (animator != null) animator.SetBool("HasDetectedPlayer", true);
            GameControl.SetDetectedState(true);
        }

        public void MakeMove()
        // AI decisions go here
        {
            if (GameControl.player.highlightedNeighbours.Count > 0) GameControl.player.UnHighLightSurrounding();
            if (!hasDetectedPlayer) MoveRandomly();
            else
            {
                MoveToPlayer();
            }
        }

        public Hex destination;
        public void MoveToPlayer()
        {
            Hex playerLocation = GameControl.player.currentPosition;

            List<Hex> _playerLocationNeighbours = playerLocation.GetImmediateNeighboursNoDir();
            List<Hex> playerLocationNeighbours = new List<Hex>();

            if (_playerLocationNeighbours.Contains(currentPosition))
            {
                destination = currentPosition;
            }
            else
            {
                foreach (Hex hex in _playerLocationNeighbours)
                {
                    if (hex.occupyingCharacter != null || hex.blocked) continue;
                    else
                    {
                        playerLocationNeighbours.Add(hex);
                    }
                }
                if (playerLocationNeighbours.Count == 0)
                // It wasn't possible to reach the immediate vicinity of the player
                // The player has either cornered themselves or all the nearest cells are occupied.
                {
                    List<Hex> freeCells = new List<Hex>();
                    int ringIndex = 2;

                    while (freeCells.Count == 0)
                    {
                        // Currently re-checks hexes already found to be occupied in a previous iteration,
                        // but in a normal use case scenario this shouldn't cause noticeable performance loss.
                        List<Hex> ring = playerLocation.GetDistantNeighboursConnected(ringIndex++);
                        ring.RemoveAll(Hex.Occupied);
                        freeCells = ring;
                    }

                    playerLocationNeighbours = freeCells;
                }

                destination = GetClosestHex(playerLocationNeighbours);
            }
            MoveTo(destination);
            StartCoroutine(WaitForMovement(destination));
        }

        private Hex GetClosestHex(List<Hex> hexes)
        {
            Hex currentClosest = null;
            int currentClosestDistance = int.MaxValue;

            foreach (Hex hex in hexes)
            {
                int distanceToHex = currentPosition.DistanceTo(hex);
                if (distanceToHex < currentClosestDistance)
                {
                    currentClosest = hex;
                    currentClosestDistance = distanceToHex;
                }
            }

            return currentClosest;
        }

        public void AttackPlayer()
        {
            if (!hasDetectedPlayer ||
                currentPosition.DistanceTo(GameControl.player.currentPosition) > 1) return;
            else
            {
                transform.LookAt(GameControl.player.transform);
                animator.SetBool("Attacking", true);
            }
        }
    
        // Called by: Wolf attack animation event keyframe
        //public void DamagePlayer()
        void DamagePlayer()
        {
            Player player = GameControl.player;
            player.remainingHealth -= stats.damageDeal;
            player.healthBar.UpdateBar(player.remainingHealth, player.stats.baseHealthAmount);
            animator.SetBool("Attacking", false);
            if (player.remainingHealth <= 0) player.Die();
        }

        public override void RefreshStats()
        {
            base.RefreshStats();
        }

        IEnumerator WaitForMovement(Hex destination)
        {
            while (currentPosition != destination)
            {
                yield return new WaitForSeconds(1);
            }

            if (hasDetectedPlayer) RotateTowards(GameControl.player.transform);
            if (currentPosition.DistanceTo(GameControl.player.currentPosition) == 1) AttackPlayer();

            // Don't continue if the player is dead
            if (GameControl.turnController.actorQueue.Contains(GameControl.player))
            {
                GameControl.turnController.NextActorTurn();
            }
        }
    }
}
