//using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Internal dependencies
using FALL.Core;
using FALL.Characters;

public class CanvasEventHandler : MonoBehaviour
{
    public GameObject menu;
    public GameObject mainMenu;
    public GameObject settings;
    public GameObject gameUI;
    public Button attackButton;
    public Button moveButton;
    public Button sneakButton;
    public Button turnButton;
    public Button camLockButton;
    public GameObject enemyAlert;
    public GameObject deathScreen;
    public GameObject victoryScreen;
    public GameObject hpBar;
    public GameObject subMenu;
    //public TMP_InputField enemyCountField;
    public InputField enemyCountField;
    public Toggle populateToggle;
    public Toggle randomSpawnToggle;

    // To prevent being able to enter the game from main menu before "starting it" by pressing the ESC key.
    public static bool gameEnabled;

    private void Awake()
    {
        menu = transform.GetChild(0).gameObject;
        mainMenu = menu.transform.GetChild(0).gameObject;
        settings = menu.transform.GetChild(1).gameObject;

        gameUI = transform.GetChild(1).gameObject;
        attackButton = gameUI.transform.GetChild(0).gameObject.GetComponent<Button>();
        moveButton = gameUI.transform.GetChild(1).gameObject.GetComponent<Button>();
        sneakButton = gameUI.transform.GetChild(2).gameObject.GetComponent<Button>();
        turnButton = gameUI.transform.GetChild(3).gameObject.GetComponent<Button>();
        camLockButton = gameUI.transform.GetChild(4).gameObject.GetComponent<Button>();
        enemyAlert = gameUI.transform.GetChild(5).gameObject;

        deathScreen = transform.GetChild(2).gameObject;
        victoryScreen = transform.GetChild(3).gameObject;
        hpBar = transform.Find("Init").Find("Simple Bar").gameObject;
        hpBar.transform.parent = gameUI.transform;
        hpBar.SetActive(false);

        subMenu = null;
        GUI.enabled = false;
        GameControl.guiHide = true;
        gameEnabled = false;
    }

    public void ToggleUI()
    {
        if (subMenu)
        // First return to parent menu when Escape is pressed
        {
            if (subMenu.transform.name != "Menu")
            {
                subMenu.SetActive(false);
                subMenu = subMenu.transform.parent.gameObject;
                if (subMenu.transform.name == "Menu")
                {
                    subMenu = null;
                    mainMenu.SetActive(true);
                }
            }
        }

        else if (menu.activeInHierarchy)
        {
            if (!gameEnabled) return;
            menu.SetActive(false);
            gameUI.SetActive(false);

            if (GameControl.activeMouseMode == "Game") gameUI.SetActive(true);
            GUI.enabled = true;
            GameControl.guiHide = false;
        }

        else
        {
            if (!gameEnabled) return;
            menu.SetActive(true);
            gameUI.SetActive(false);
            GUI.enabled = false;
            GameControl.guiHide = true;
        }
    }

    /*
     * Menu events
    */

    public void StartGame()
    {
        GameControl.map.UnHighLightEverything();
        gameEnabled = true;
        deathScreen.SetActive(false);
        victoryScreen.SetActive(false);
        hpBar.SetActive(true);
        menu.SetActive(true); // Bit sloppy, we don't need to go to the main menu here, but ToggleUI below will complain otherwise
        GameControl.playMouse.enabled = false;
        //GameControl.terrain.heightmapPixelError = 20;
        GameControl.terrain.heightmapPixelError = 10;
        GameControl.terrain.drawTreesAndFoliage = true;
        /*
        * INITIALIZATION
        */
        GameControl.orthoCamera.transform.gameObject.SetActive(false);
        GameControl.mainCamera.transform.gameObject.SetActive(true);
        GameControl.gameControl.InitilizeBaseGameState();
        GameControl.editingMouse.enabled = false;
        GameControl.playMouse.enabled = true;
        // // // // // // // // // // // // // // // 

        ToggleUI();
    }

    public void EditMap()
    {
        GameControl.activeMouseMode = "MapEditing";
        GameControl.terrain.heightmapPixelError = 1;
        gameEnabled = true;
        ToggleUI();
        GameControl.editingMouse.enabled = true;
        GameControl.playMouse.enabled = false;
        GameControl.orthoCamera.transform.gameObject.SetActive(true);
        GameControl.mainCamera.transform.gameObject.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenSettings()
    {
        mainMenu.SetActive(false);
        settings.SetActive(true);
        subMenu = settings;
    }

    public void ApplySettings()
    // Toggles get applied regardless
    {
        int enemyCount;
        if (int.TryParse(enemyCountField.text, out enemyCount))
        {
            if (enemyCount > 50) enemyCount = 50;
            if (enemyCount < 0) enemyCount = 0;
        }
        else
        {
            enemyCount = 0;
        }

        GameControl.enemyCount = enemyCount;
    }

    public void OnPopulateToggle()
    {
        GameControl.gameControl.POPULATE = populateToggle.isOn;
    }

    public void OnRandomSpawnsToggle()
    {
        GameControl.gameControl.distributeRandomly = randomSpawnToggle.isOn;
    }

    /*
     * Ingame UI
    */

    public void AttackMode()
    {
        GameControl.NewPlayerState(GameControl.PlayerState.Attack);
    }

    public void MoveMode()
    {
        GameControl.NewPlayerState(GameControl.PlayerState.Move);
    }

    public void SneakMode()
    {
        if (GameControl.playerState == GameControl.PlayerState.Move || GameControl.playerState == GameControl.PlayerState.Exploring)
        {
            GameControl.player.Sneak();
            if (sneakButton.GetComponent<Image>().color == Color.white)
                sneakButton.GetComponent<Image>().color = Color.green;
            else sneakButton.GetComponent<Image>().color = Color.white;
        }
    }

    public void EndTurn()
    {
        if (GameControl.turnController.actorQueue != null) GameControl.turnController.NextActorTurn();
    }


    public void CamLock()
    {
        if (GameControl.mainCamera.GetComponent<PerspectiveCameraMovement>().focusTarget != null)
        {
            GameControl.mainCamera.GetComponent<PerspectiveCameraMovement>().ExitFocus();
            SetColor(camLockButton, Color.white);
        }
        else
        {
            GameControl.mainCamera.GetComponent<PerspectiveCameraMovement>().FocusOnCharacter(GameControl.player);
            SetColor(camLockButton, Color.green);
        }
    }

    public void LoadMainMenu()
    {
        deathScreen.SetActive(false);
        victoryScreen.SetActive(false);
        menu.SetActive(true);
    }

    public void DisplayDeathScreen()
    {
        gameEnabled = false;
        menu.SetActive(false);
        gameUI.SetActive(false);
        deathScreen.SetActive(true);
        GUI.enabled = false;
        GameControl.guiHide = true;
    }

    public void DisplayVictoryScreen()
    {
        gameEnabled = false;
        menu.SetActive(false);
        gameUI.SetActive(false);
        victoryScreen.SetActive(true);
        GUI.enabled = false;
        GameControl.guiHide = true;
    }


    public void DisableButtons()
    {
        attackButton.GetComponent<Button>().interactable = false ;
        moveButton.GetComponent<Button>().interactable = false;
        sneakButton.GetComponent<Button>().interactable = false;
        turnButton.GetComponent<Button>().interactable = false;
        camLockButton.GetComponent<Button>().interactable = false;
    }

    public void EnableButtons()
    {
        attackButton.GetComponent<Button>().interactable = true;
        moveButton.GetComponent<Button>().interactable = true;
        sneakButton.GetComponent<Button>().interactable = true;
        turnButton.GetComponent<Button>().interactable = true;
        camLockButton.GetComponent<Button>().interactable = true;
    }

    public void ResetColors()
    {
        SetColor(attackButton, Color.white);
        SetColor(moveButton, Color.white);
        SetColor(sneakButton, Color.white);
        SetColor(turnButton, Color.white);
        SetColor(camLockButton, Color.white);
        SetColor(enemyAlert, Color.white);
    }

    /*
     * Other
    */

    public void SetColor(Button button, Color color)
    {
        button.GetComponent<Image>().color = color;
    }
    public void SetColor(GameObject button, Color color)
    {
        button.GetComponent<Image>().color = color;
    }
}
