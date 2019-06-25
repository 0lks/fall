using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
// !! This class is NOT functional
{
    public static InventoryManager Instance;
    //private bool windowOpen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        //windowOpen = false;
    }

    public void ToggleInventoryWindow()
    {

    }
}
