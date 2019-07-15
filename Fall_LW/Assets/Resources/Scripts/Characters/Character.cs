using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    // Don't reinitialize or reassign stats to other variables downstream, reference this instead
    public CharacterDATA stats;
    [HideInInspector]
    public Rigidbody rb;
    protected Animator animator;
    protected TurnController turnController;
    [HideInInspector]
    public int movementAmount;
    [HideInInspector]
    public float remainingHealth;

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

    protected void Awake()
    {
        rb = GetComponentInChildren<Rigidbody>();
        rb.centerOfMass = new Vector3(0, 2, 0);
        rb.ResetCenterOfMass();
        animator = GetComponent<Animator>();
        movementAmount = stats.baseMovementAmount;
        remainingHealth = stats.baseHealthAmount;
    }

    protected void Start()
    {
        turnController = GameControl.turnController;
    }

    public virtual void RefreshStats()
    // Define stat refreshes applicable to all character types here
    {
        movementAmount = stats.baseMovementAmount;
    }

    public virtual void MoveTo(Hex target)
    {
        if (target.blocked) return;
        if (GetType() == typeof(Player))
        {
            GameControl.canvas.DisableButtons();
            GameControl.gameControl.DisableMouse();
            movementAmount -= GameControl.movePath.Count - 1;
            StartCoroutine(MoveCoroutine(GameControl.movePath));
        }
        else
        {
            Queue<Hex> path = GameControl.graph.Path(currentPosition, target);
            // Path is too long to for one dash
            if (movementAmount < path.Count - 1)
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
            StartCoroutine(MoveCoroutine(path));
        }

        GameControl.gameControl.DisableMouse();
    }

    public void Die()
    {
        currentPosition.occupyingCharacter = null;
        turnController.RemoveFromQueue(this);
        if (GetType() == typeof(Enemy)) {
            GameControl.allEnemies.Remove(GetComponent<Enemy>());
            GameControl.nearbyEnemies.Remove(GetComponent<Enemy>());

            if (GameControl.allEnemies.Count <= 0)
            {
                Destroy(gameObject);
                return;
            }


            if (GameControl.nearbyEnemies.Count == 0)
            {
                GameControl.NewPlayerState("EXPLORING");
            }
            Destroy(gameObject);
        }
        else
        {
            GameControl.canvas.DisplayDeathScreen();
            transform.gameObject.SetActive(false);
            GameControl.editingMouse.enabled = false;
            GameControl.playMouse.enabled = false;
        }
    }

    protected virtual IEnumerator MoveCoroutine(Queue<Hex> path)
    {
        if (GetComponent<Animator>() != null)
        {
            animator.SetBool("Moving", true);
        }
        currentPosition.UnHover();
        Hex nextTarget = path.Dequeue();

        Vector3 destination;
        Ray ray = new Ray(nextTarget.transform.position, Vector3.down);
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, 1 << 11 | 1 << 18))
        {
            destination = hitInfo.point;
        }

        else
        {
            throw new System.Exception("Something happened :S Verify that Terrain is on layer 11");
        }

        Vector3 dir = destination - transform.position;
        Vector3 vel = dir.normalized * animator.GetFloat("MovementSpeed") * Time.deltaTime;
        vel = Vector3.ClampMagnitude(vel, dir.magnitude);
        float min = (animator.GetFloat("MovementSpeed") / 20f); // Anti-overshoot

        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        while (Vector3.Distance(transform.position, destination) > min)
        {
            rb.MovePosition(transform.position + (dir * animator.GetFloat("MovementSpeed") / 10 * Time.deltaTime));
            yield return null;
        }

        transform.position = destination;
        currentPosition.occupyingCharacter = null;
        currentPosition = nextTarget;
        currentPosition.occupyingCharacter = this;

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
}
