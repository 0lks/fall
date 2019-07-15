using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseManagerGameMode : MouseManager
{
    private void Awake()
    {
        enabled = false; // Needed to prevent OnEnable from running at start
    }
    protected new void Start()
    {
        base.Start();
    }
    private void OnEnable()
    // Enabled:
    // 1) At the end of a characters movement coroutine (by ReactivateMouse)
    // 2) When switching from MapEditing to GameMode
    // Disabled:
    // 1) When any character starts moving (by DisableMouse)
    // 2) When switching from GameMode to MapEditing
    {
        GameControl.CheckEnemies();
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (GameControl.turnController.enabled)
        /*
         * 
         * COMBAT STATE BEHAVIOUR
         * 
        */
        {
            if (GameControl.turnController.currentActor != GameControl.player) return;

            Ray ray = GameControl.mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo) && GUIUtility.hotControl == 0)
            {
                GameObject hitObject = hitInfo.transform.gameObject;

                if (hitObject.tag == "Hex")
                {
                    Hex hitHex = hitObject.GetComponent<Hex>();
                    if (!hitHex) return;
                    if (GameControl.playerState == "MOVE")
                    {
                        if (Input.GetMouseButtonDown(0))
                        // !!!!! COMBAT STATE BEHAVIOUR
                        {
                            if (GameControl.selectedHex != hitHex && !hitHex.blocked && GameControl.player.highlightedNeighbours.Contains(hitHex))
                            {
                                if (hitHex.Selected()) GameControl.selectedHex = hitHex;
                            }
                            else if (GameControl.selectedHex == hitHex && !hitHex.blocked)
                            {
                                if (hitHex.occupyingCharacter != null) return;
                                else GameControl.player.MoveTo(hitHex);
                            }
                        }
                    }

                    else if (GameControl.playerState == "ATTACK")
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            if (hitHex.occupyingCharacter != null) GameControl.player.Attack(hitHex.occupyingCharacter);
                            else return;
                        }
                    }
                }
                else if (hitObject.tag == "Enemy")
                {
                    if (GameControl.playerState == "ATTACK")
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            GameControl.player.Attack(hitObject.gameObject.GetComponent<Enemy>());
                        }
                    }
                }
            }
        }
        else
        /*
         * 
         * EXPLORATION STATE BEHAVIOUR
         * 
        */
        {
            Ray ray = GameControl.mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo) && GUIUtility.hotControl == 0)
            {
                GameObject hitObject = hitInfo.transform.gameObject;

                if (hitObject.tag == "Hex" && GameControl.playerState != "ATTACK")
                {
                    Hex hitHex = hitObject.GetComponent<Hex>();
                    if (!hitHex) return;
                    if (Input.GetMouseButtonDown(0))
                    // !!!!! EXPLORATION STATE BEHAVIOUR
                    {
                        if (GameControl.selectedHex != hitHex && !hitHex.blocked && GameControl.player.highlightedNeighbours.Contains(hitHex))
                        {
                            if (hitHex.Selected()) GameControl.selectedHex = hitHex;
                            else GameControl.selectedHex = null;
                        }
                        else if (GameControl.selectedHex == hitHex)
                        {
                            if (hitHex.occupyingCharacter != null) return;
                            else GameControl.player.MoveTo(hitHex);
                        }
                    }
                }
            }
        }

    }
}
