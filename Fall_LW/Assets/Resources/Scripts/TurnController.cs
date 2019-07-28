using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Internal dependencies
using FALL.Core;
using FALL.Characters;

public class TurnController : MonoBehaviour
{
    public Character currentActor;
    public Queue<Character> actorQueue;

    private void Awake()
    {
        enabled = false;
    }
    private void OnEnable()
    {
        actorQueue = new Queue<Character>();
        currentActor = GameControl.player;
        GameControl.NewPlayerState(GameControl.PlayerState.Move);
    }

    public bool IsTurn(Character actor)
    {
        return false;
    }

    public void addToQueue(Character character)
    {
        actorQueue.Enqueue(character);
    }

    public void NextActorTurn()
    {
        currentActor = actorQueue.Dequeue();
        actorQueue.Enqueue(currentActor);
        if (actorQueue.Count == 1)
        {
            GameControl.NewPlayerState(GameControl.PlayerState.Exploring);
            return;
        }

        currentActor.RefreshStats();

        if (currentActor.GetType() == typeof(Enemy))
        {
            GameControl.canvas.DisableButtons();
            GameControl.gameControl.DisableMouse();
            currentActor.GetComponent<Enemy>().MakeMove();
        }

        else
        {
            GameControl.canvas.EnableButtons();
            GameControl.gameControl.ReactivateMouse();
        }
    }

    public void RemoveFromQueue(Character actor)
    {
        if (actorQueue == null || actorQueue.Count <= 0) return;
        Queue<Character> newQ = new Queue<Character>();
        foreach (Character _actor in actorQueue)
        {
            if (_actor != actor)
            {
                newQ.Enqueue(_actor);
            }
        }
        actorQueue = newQ;
    }
}
