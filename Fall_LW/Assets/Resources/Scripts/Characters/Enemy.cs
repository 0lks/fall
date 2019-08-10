﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Internal dependencies
using FALL.Core;

namespace FALL.Characters {
    public class Enemy : Character
    {
        [SerializeField] EnemyDATA _stats;
        Hex destination;

        public bool hasDetectedPlayer { get; private set; }

        /*
         * 
         * Getters for initial values specific to Enemy
         * 
        */
        public int GetPlayerDetectDist() { return _stats.defaultPlayerDetectDist; }
        public int GetSneakingPlayerDetectDist() { return _stats.sneakingPlayerDetectDist; }

        /*
         * 
         * Initialization
         * 
        */
        protected new void Awake()
        {
            base.stats = _stats;
            base.Awake();
            hasDetectedPlayer = animator.GetBool("HasDetectedPlayer");
        }

        public override void MoveTo(Hex target)
        {
            if (target.blocked || target == currentPosition) return;
            GameControl.gameControl.DisableMouse();

            Queue<Hex> path = GameControl.graph.Path(currentPosition, target);
            //Path is too long to for one dash
            //if (movementAmount < path.Count - 1)
            if (movementAmount < path.Count)
            {
                int steps = movementAmount;
                Queue<Hex> shortenedPath = new Queue<Hex>();
                while (steps-- > 0)
                {
                    if (steps == 0) GetComponent<Enemy>().destination = path.Peek();
                    shortenedPath.Enqueue(path.Dequeue());
                }
                path = shortenedPath;
            }
            movementAmount -= path.Count - 1;
            //path.Dequeue(); //TODO: The first element is the current location - fix the problem in Graph.
            StartCoroutine(MoveCoroutine(path));
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
            if (((currentPosition.DistanceTo(playerLocation) <= GetPlayerDetectDist()) && !GameControl.player.sneaking)
                || ((currentPosition.DistanceTo(playerLocation) <= GetSneakingPlayerDetectDist()) && GameControl.player.sneaking))
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

        public void MoveToPlayer()
        {
            Hex playerLocation = GameControl.player.currentPosition;

            List<Hex> _playerLocationNeighbours = playerLocation.GetImmediateNeighboursNoDir();
            List<Hex> playerLocationNeighbours = new List<Hex>();

            if (_playerLocationNeighbours.Contains(currentPosition))
            {
                //destination = currentPosition;
                return;
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
            player.healthBar.UpdateBar(player.remainingHealth, player.GetBaseHealthAmount());
            animator.SetBool("Attacking", false);
            if (player.remainingHealth <= 0) player.Die();
        }

        public override void RefreshStats()
        {
            base.RefreshStats();
        }

        public override void Die()
        {
            currentPosition.occupyingCharacter = null;
            GameControl.turnController.RemoveFromQueue(this);
            GameControl.allEnemies.Remove(GetComponent<Enemy>());
            GameControl.nearbyEnemies.Remove(GetComponent<Enemy>());
            if (GameControl.allEnemies.Count <= 0)
            {
                Destroy(gameObject);
                return;
            }
            if (GameControl.nearbyEnemies.Count == 0)
            {
                GameControl.NewPlayerState(GameControl.PlayerState.Exploring);
            }
            Destroy(gameObject);
        }

        protected override void PlaceWeaponInHand()
        {
            throw new System.NotImplementedException();
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
