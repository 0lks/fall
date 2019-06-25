using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : Character
{
    //EquipmentManager equipmentManager;
    public Weapon wieldedWeapon;
    public List<Hex> attackableHexes;
    public SimpleHealthBar healthBar;
    public string spawnPoint;
    public List<Hex> highlightedNeighbours;

    protected new void Awake()
    {
        base.Awake();
        GameControl.player = this;
    }

    protected new void Start()
    {
        base.Start();

        if (spawnPoint != null && GameControl.map.HexExists(spawnPoint))
        {
            NewCurrentPosition(GameControl.map.GetHex(spawnPoint));
        }

        else NewCurrentPosition(GameControl.map.GetAllHexes()[0]);

        transform.position = currentPosition.GetPositionOnGround();
        GameControl.mainCamera.GetComponent<PerspectiveCameraMovement>().SetInitialPosition(this);

        //equipmentManager = EquipmentManager.Instance;
        //wieldedWeapon = equipmentManager.GetWeapon();

        wieldedWeapon = GetComponentInChildren<Bow>();

        if (wieldedWeapon != null)
        {
            wieldedWeapon.gameObject.GetComponent<Bow>().Equip();
            //wieldedWeapon.gameObject.GetComponent<Weapon>().Equip();
        }
    }

    // Stores the step lost when going from upright movement to sneak movement with an odd number of steps left
    public bool extraStep;
    [HideInInspector]
    public bool sneaking = false;

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
        GameControl.NewPlayerState("MOVE");
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
        if (currentPosition.DistanceTo(character.currentPosition) > wieldedWeapon.attackDistance ||
            !attackableHexes.Contains(character.currentPosition)) return false;

        movementAmount = 0;
        RotateTowards(character.transform);
        wieldedWeapon.Target(character);
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
        GameControl.NewPlayerState("MOVE");
        if (sneaking)
        // If the player was sneaking at the end of last turn, don't reset movement amount to full
        {
            sneaking = false;
            Sneak();
        }
    }
}
