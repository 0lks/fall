using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Internal dependencies
using FALL.Core;

namespace FALL.Characters {
    public abstract class Character : MonoBehaviour
    {
        // Don't reinitialize or reassign stats to other variables downstream, reference this instead
        protected CharacterDATA stats;
        public Rigidbody rb { get; private set; }
        [HideInInspector] public Animator animator;
        [HideInInspector] public int movementAmount;
        [HideInInspector] public float remainingHealth;

        private Hex _currentPosition;
        public Hex currentPosition
        {
            get { return _currentPosition; }
            private set { NewCurrentPosition(value); }
        }
        public void NewCurrentPosition(Hex hex)
        {
            hex.occupyingCharacter = this;
            _currentPosition = hex;
        }

        /*
         * Getters for initial values
        */
        public int GetBaseMovementAmount() { return stats.baseMovementAmount; }
        public float GetBaseHealthAmount() { return stats.baseHealthAmount; }

        protected void Awake()
        {
            rb = GetComponentInChildren<Rigidbody>();
            rb.centerOfMass = new Vector3(0, 2, 0);
            rb.ResetCenterOfMass();
            //animator = GetComponent<Animator>();
            animator = GetComponentInChildren<Animator>();
            movementAmount = stats.baseMovementAmount;
            remainingHealth = stats.baseHealthAmount;
        }

        public virtual void RefreshStats()
        // Define stat refreshes applicable to all character types here
        {
            movementAmount = stats.baseMovementAmount;
        }

        public abstract void MoveTo(Hex target);
        public abstract void Die();

        protected virtual IEnumerator MoveCoroutine(Queue<Hex> path)
        {
            if (animator != null) animator.SetBool("Moving", true);
            currentPosition.UnHover();
            Hex nextHex = path.Dequeue();
            Vector3 destination = nextHex.GetPositionOnGround();
            Vector3 dir = destination - transform.position;
            Vector3 vel = dir.normalized * animator.GetFloat("MovementSpeed") * Time.deltaTime;
            vel = Vector3.ClampMagnitude(vel, dir.magnitude);
            float minDistance = (animator.GetFloat("MovementSpeed") / 20f); // Anti-overshoot

            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            while (Vector3.Distance(transform.position, destination) > minDistance)
            {
                rb.MovePosition(transform.position + vel);
                yield return null;
            }

            transform.position = destination;
            currentPosition.occupyingCharacter = null;
            Hex lastPosition = currentPosition;
            currentPosition = nextHex;
            currentPosition.occupyingCharacter = this;

            Hex.Direction moveDirection = Hex.GetDirectionalRelation(lastPosition, currentPosition);
            UpdateHexRender(currentPosition, moveDirection);

            if (GameControl.nearbyEnemies != null)
            {
                foreach (Enemy enemy in GameControl.nearbyEnemies)
                {
                    if (enemy.CanDetectPlayer()) enemy.HasDetectedPlayer();
                }
            }

            if (path.Count > 0) StartCoroutine(MoveCoroutine(path));
            else
            {
                Vector3 eulerAngles = transform.rotation.eulerAngles;
                eulerAngles = new Vector3(0, eulerAngles.y, eulerAngles.z);
                transform.rotation = Quaternion.Euler(eulerAngles);

                if (GetComponent<Animator>() != null)
                {
                    animator.SetBool("Moving", false);
                }

                if ((GameControl.turnController.enabled && GameControl.turnController.currentActor == GameControl.player)
                    || !GameControl.turnController.enabled)
                {
                    GameControl.gameControl.ReactivateMouse();
                    GameControl.canvas.EnableButtons();
                }
            }
        }

        public void RotateTowards(Transform targetTransform)
        {
            Vector3 targetDirection = targetTransform.position - transform.position;
            rb.transform.rotation = Quaternion.LookRotation(targetDirection, Vector3.up);

            //Maintain an upright position
            Vector3 eulerAngles = rb.transform.rotation.eulerAngles;
            eulerAngles = new Vector3(0, eulerAngles.y, eulerAngles.z);
            rb.transform.rotation = Quaternion.Euler(eulerAngles);
        }

        private void UpdateHexRender(Hex newPos, Hex.Direction moveDirection)
        // Kontrollida: kui tegemist on vaenalasega, siis renderi niipalju hexe kui on
        // selle vaenlase max liikumisraadius. Mängija jaoks tekita eraldi inspektoris muudetav
        // väärtus, mis ulatub kaugemale kui max liikumiskaugus
        {
            int renderDistance = GameControl.queueInDistance + 10;

            Hex.Direction forwardLeft;
            Hex.Direction forwardRight;
            Hex.Direction backwardLeft;
            Hex.Direction backwardRight;
            Hex.Direction oppositeDir;
            if (moveDirection == Hex.Direction.NE)
            {
                oppositeDir = Hex.Direction.SW;
                forwardLeft = Hex.Direction.W;
                forwardRight = Hex.Direction.SE;
                backwardLeft = Hex.Direction.E;
                backwardRight = Hex.Direction.NW;
            }
            else if (moveDirection == Hex.Direction.E)
            {
                oppositeDir = Hex.Direction.W;
                forwardLeft = Hex.Direction.NW;
                forwardRight = Hex.Direction.SW;
                backwardLeft = Hex.Direction.SE;
                backwardRight = Hex.Direction.NE;
            }
            else if (moveDirection == Hex.Direction.SE)
            {
                oppositeDir = Hex.Direction.NW;
                forwardLeft = Hex.Direction.NE;
                forwardRight = Hex.Direction.W;
                backwardLeft = Hex.Direction.SW;
                backwardRight = Hex.Direction.E;
            }
            else if (moveDirection == Hex.Direction.SW)
            {
                oppositeDir = Hex.Direction.NE;
                forwardLeft = Hex.Direction.E;
                forwardRight = Hex.Direction.NW;
                backwardLeft = Hex.Direction.W;
                backwardRight = Hex.Direction.SE;
            }
            else if (moveDirection == Hex.Direction.W)
            {
                oppositeDir = Hex.Direction.E;
                forwardLeft = Hex.Direction.SE;
                forwardRight = Hex.Direction.NE;
                backwardLeft = Hex.Direction.NW;
                backwardRight = Hex.Direction.SW;
            }
            else
            {
                oppositeDir = Hex.Direction.SE;
                forwardLeft = Hex.Direction.SW;
                forwardRight = Hex.Direction.E;
                backwardLeft = Hex.Direction.NE;
                backwardRight = Hex.Direction.W;
            }

            Hex forwardHex;
            Hex backwardHex;
            Hex neighbour;

            if (this.GetType().Name == "Enemy")
            {
                /*
                    * Turn on new neighbours 
                */
                //bool forwardIsTemp = false;

                try
                {
                    forwardHex = newPos.GetNeighbour(moveDirection, renderDistance);
                    forwardHex.gameObject.SetActive(true);
                }
                catch
                {
                    /*
                        * There is no existing hex in this forward position, therefore we
                        * create a temporary one with just the data necessary for the calculations below.
                        * We don't instantiate this hex or add it to the map. It is removed at the end of this function.
                    */

                    int[] axis = Hex.GetAxis(moveDirection);
                    forwardHex = new Hex(
                        newPos.x + axis[0] * renderDistance,
                        newPos.y + axis[1] * renderDistance,
                        newPos.z + axis[2] * renderDistance);
                    //Debug.Log("getNeighbour did not find a forward hex, creating a temporary one");
                    //forwardIsTemp = true;
                }
                //if (!forwardIsTemp) forwardHex.gameObject.SetActive(true);


                for (int i = renderDistance; i > 0; i--)
                {
                    try
                    {
                        neighbour = forwardHex.GetNeighbour(forwardLeft, i);
                        neighbour.gameObject.SetActive(true);
                    }
                    catch { }
                    try
                    {
                        neighbour = forwardHex.GetNeighbour(forwardRight, i);
                        neighbour.gameObject.SetActive(true);
                    }
                    catch { }
                }

                /*
                    * Turn off old neighbours
                */

                try
                {
                    backwardHex = newPos.GetNeighbour(oppositeDir, renderDistance + 1);
                    if (!GameControl.playerSurroundingHexes.Contains(backwardHex)) backwardHex.gameObject.SetActive(false);
                }
                catch
                {
                    /*
                        * There is no existing hex in this backward position, therefore we
                        * create a temporary one with just the data necessary for the calculations below.
                        * We don't instantiate this hex or add it to the map. It is removed at the end of this function.
                    */
                    int[] axis = Hex.GetAxis(oppositeDir);
                    backwardHex = new Hex(
                        newPos.x + axis[0] * (renderDistance + 1),
                        newPos.y + axis[1] * (renderDistance + 1),
                        newPos.z + axis[2] * (renderDistance + 1));
                }

                for (int i = renderDistance; i > 0; i--)
                {
                    try
                    {
                        neighbour = backwardHex.GetNeighbour(backwardLeft, i);
                        if (!GameControl.playerSurroundingHexes.Contains(neighbour)) neighbour.gameObject.SetActive(false);
                    }
                    catch { }
                    try
                    {
                        neighbour = backwardHex.GetNeighbour(backwardRight, i);
                        if (!GameControl.playerSurroundingHexes.Contains(neighbour)) neighbour.gameObject.SetActive(false);
                    }
                    catch { }
                }
            }
            else
            // Player radius
            {
                /*
                    * Turn on new neighbours 
                */
                //bool forwardIsTemp = false;

                try
                {
                    forwardHex = newPos.GetNeighbour(moveDirection, renderDistance);
                    forwardHex.gameObject.SetActive(true);
                    GameControl.playerSurroundingHexes.Add(forwardHex);
                }
                catch
                {
                    /*
                        * There is no existing hex in this forward position, therefore we
                        * create a temporary one with just the data necessary for the calculations below.
                        * We don't instantiate this hex or add it to the map. It is removed at the end of this function.
                    */

                    int[] axis = Hex.GetAxis(moveDirection);
                    forwardHex = new Hex(
                        newPos.x + axis[0] * renderDistance,
                        newPos.y + axis[1] * renderDistance,
                        newPos.z + axis[2] * renderDistance);
                    //Debug.Log("getNeighbour did not find a forward hex, creating a temporary one");
                    //forwardIsTemp = true;
                }
                //if (!forwardIsTemp) forwardHex.gameObject.SetActive(true);


                for (int i = renderDistance; i > 0; i--)
                {
                    try
                    {
                        neighbour = forwardHex.GetNeighbour(forwardLeft, i);
                        neighbour.gameObject.SetActive(true);
                        GameControl.playerSurroundingHexes.Add(neighbour);
                    }
                    catch { }
                    try
                    {
                        neighbour = forwardHex.GetNeighbour(forwardRight, i);
                        neighbour.gameObject.SetActive(true);
                        GameControl.playerSurroundingHexes.Add(neighbour);
                    }
                    catch { }
                }

                /*
                    * Turn off old neighbours
                */

                try
                {
                    backwardHex = newPos.GetNeighbour(oppositeDir, renderDistance + 1);
                    backwardHex.gameObject.SetActive(false);
                    GameControl.playerSurroundingHexes.Remove(backwardHex);
                }
                catch
                {
                    /*
                        * There is no existing hex in this backward position, therefore we
                        * create a temporary one with just the data necessary for the calculations below.
                        * We don't instantiate this hex or add it to the map. It is removed at the end of this function.
                    */
                    int[] axis = Hex.GetAxis(oppositeDir);
                    backwardHex = new Hex(
                        newPos.x + axis[0] * (renderDistance + 1),
                        newPos.y + axis[1] * (renderDistance + 1),
                        newPos.z + axis[2] * (renderDistance + 1));
                }

                for (int i = renderDistance; i > 0; i--)
                {
                    try
                    {
                        neighbour = backwardHex.GetNeighbour(backwardLeft, i);
                        neighbour.gameObject.SetActive(false);
                        GameControl.playerSurroundingHexes.Remove(neighbour);
                    }
                    catch { }
                    try
                    {
                        neighbour = backwardHex.GetNeighbour(backwardRight, i);
                        neighbour.gameObject.SetActive(false);
                        GameControl.playerSurroundingHexes.Remove(neighbour);
                    }
                    catch { }
                }
            }
        }
    }
}

