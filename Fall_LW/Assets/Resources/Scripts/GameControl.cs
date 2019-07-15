using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GameControl : MonoBehaviour {
    public static Map map;
    public static GameObject hexPrefab;
    public static Graph graph;
    public static GameControl gameControl;
    public static Player player;
    public static TurnController turnController;
    public static List<Enemy> allEnemies;
    public static List<Enemy> nearbyEnemies;
    public static Terrain terrain;
    public static CanvasEventHandler canvas;
    public static MouseManagerMapEditing editingMouse;
    public static MouseManagerGameMode playMouse;
    public static bool guiHide;
    public static bool playedSoundThisTurn = false;
    //public static Shader xRay;
    //public static Shader noXRay;
    Stopwatch sw;
    public static Hex selectedHex;
    public static Queue<Hex> movePath;
    public static int queueInDistance;
    public int _queueInDistance;
    public string playerSpawnPoint;

    /*
     * Enemy Spawning
    */
    [HideInInspector]
    public bool POPULATE;
    public static int enemyCount = 1;
    [HideInInspector]
    public bool distributeRandomly;
    public string[] spawnPoints;
    // // // // // // // // // // // // // // // 

    private static string _playerState = "EXPLORING";
    public static string playerState
    {
        get { return _playerState; }
        private set { NewPlayerState(value); }
    }
    public static void NewPlayerState(string state) //replace w enum
    {
        string prevstate = _playerState;
        _playerState = state;
        if (_playerState == "MOVE")
        {
            player.currentPosition.HighLightSurroundingMoveState(player.movementAmount);
            player.currentPosition.Highlight();
            canvas.SetColor(canvas.moveButton, Color.green);
            canvas.SetColor(canvas.attackButton, Color.white);
            canvas.EnableButtons();
        }
        else if (_playerState == "ATTACK")
        {
            player.currentPosition.HighLightSurroundingAttackState(player.wieldedWeapon.attackDistance);
            player.currentPosition.Highlight();
            canvas.SetColor(canvas.attackButton, Color.green);
            canvas.SetColor(canvas.moveButton, Color.white);
            canvas.EnableButtons();
        }

        else if (_playerState == "EXPLORING")
        {
            playedSoundThisTurn = false;
            canvas.SetColor(canvas.turnButton, Color.white);
            player.movementAmount = player.stats.baseExploreMovementAmount;
            player.currentPosition.HighLightSurroundingMoveState(player.movementAmount);
            player.currentPosition.Highlight();
            turnController.enabled = false;
            SetDetectedState(false);
            gameControl.ReactivateMouse();
            canvas.EnableButtons();
        }
    }

    private static bool _detectedState = false;
    public static bool detectedState
    {
        get { return _detectedState; }
        private set { SetDetectedState(value); }
    }
    public static void SetDetectedState(bool boolVal)
    {
        if (boolVal == true)
        {
            _detectedState = true;
            canvas.SetColor(canvas.enemyAlert, Color.red);
        }
        else
        {
            _detectedState = false;
            canvas.SetColor(canvas.enemyAlert, Color.white);
        }
    }

    [HideInInspector]
    public static Camera mainCamera;
    [HideInInspector]
    public static Camera orthoCamera;
    public static GameObject discardPool;
    private GameObject wolf;

    public static string activeMouseMode;


    void Awake () {
        hexPrefab = (GameObject) Resources.Load("Prefabs/Other/Hex/Hex");
        wolf = (GameObject) Resources.Load("Prefabs/Characters/Wolf/Wolf");
        //xRay = (Shader)Resources.Load("Custom Shaders/XRay");
        //noXRay = (Shader)Resources.Load("Materials/XRay/NOXRay");
        queueInDistance = _queueInDistance;
        Application.targetFrameRate = 60;

        if (gameControl == null)
        {
            gameControl = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sw = new Stopwatch();
        terrain = GameObject.FindGameObjectWithTag("Terrain").GetComponent<Terrain>();
        canvas = GameObject.FindGameObjectWithTag("Canvas").GetComponent<CanvasEventHandler>();
        editingMouse = gameObject.AddComponent<MouseManagerMapEditing>();
        playMouse = gameObject.AddComponent<MouseManagerGameMode>();
        LoadLevel();
        allEnemies = new List<Enemy>();
        nearbyEnemies = new List<Enemy>();
        turnController = gameObject.AddComponent<TurnController>();

        Camera[] allCameras = Camera.allCameras;
        mainCamera = allCameras[0];
        orthoCamera = allCameras[1];
        mainCamera.transform.gameObject.SetActive(false);
        orthoCamera.transform.gameObject.SetActive(true);

        discardPool = GameObject.FindGameObjectWithTag("DiscardPool");

        activeMouseMode = "MapEditing";
        gameObject.GetComponent<MouseManagerMapEditing>().enabled = true;
        gameObject.GetComponent<MouseManagerGameMode>().enabled = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canvas.ToggleUI();
        }
    }

    public void InitilizeBaseGameState()
    // Called by StartGame(), resets the game to its original state
    {
        canvas.EnableButtons();
        canvas.ResetColors();
        player.GetComponent<Animator>().SetFloat("MovementSpeed", 20f);
        activeMouseMode = "Game";
        if (playerSpawnPoint != null && map.HexExists(playerSpawnPoint))
        {
            player.NewCurrentPosition(map.GetHex(playerSpawnPoint));
        }
        else player.NewCurrentPosition(map.GetAllHexes()[0]);
        player.transform.position = player.currentPosition.GetPositionOnGround();
        mainCamera.GetComponent<PerspectiveCameraMovement>().SetInitialPosition(player);

        player.sneaking = false;
        player.extraStep = false;
        canvas.SetColor(canvas.sneakButton, Color.white);
        player.GetComponentInChildren<Animator>().SetBool("Sneaking", false);

        List<Enemy> allEnemiesCopy;
        if (allEnemies != null)
        {
            allEnemiesCopy = new List<Enemy>(allEnemies);
            foreach (Enemy enemy in allEnemiesCopy) enemy.Die();
        }

        turnController.enabled = false;

        if (POPULATE)
        {
            allEnemies = new List<Enemy>();
            nearbyEnemies = new List<Enemy>();
            PopulateWorldWithEnemy(wolf.GetComponent<Enemy>()); // can be optimized by fetching wolves from a pool
        }
        else
        {
            nearbyEnemies = null;
            allEnemies = null;
        }
        player.remainingHealth = player.stats.baseHealthAmount;
        player.gameObject.SetActive(true);
        player.healthBar = canvas.hpBar.GetComponentInChildren<SimpleHealthBar>();
        player.healthBar.UpdateBar(player.remainingHealth, player.stats.baseHealthAmount);

        NewPlayerState("EXPLORING");
    }

    public void SaveLevel()
    {
        Debug.Log("Saving level...");
        File.Delete("start.dat");
        map.Serialize();
    }

    private void LoadLevel()
    {
        if (File.Exists("start.dat"))
        {
            using (Stream stream = File.Open("start.dat", FileMode.Open))
            {
                var bformatter = new BinaryFormatter();
                MapSerializedContent deserializedMap = (MapSerializedContent)bformatter.Deserialize(stream);
                map.SetMap(deserializedMap);

                stream.Close();
            }
        }
    }

    private void PopulateWorldWithEnemy(Enemy enemy)
    {
        if (enemyCount <= 0) return;
        List<Hex> randomHexes;

        if (distributeRandomly)
        {
            randomHexes = map.GetRandomHexes(enemyCount);
            foreach (Hex hex in randomHexes)
            {
                Enemy _enemy = Instantiate(enemy, map.transform.Find("Characters"));
                _enemy.NewCurrentPosition(hex);
                _enemy.transform.position = _enemy.currentPosition.GetPositionOnGround();
                allEnemies.Add(_enemy);
            }
        }
        else
        {
            // Keep track of the hexes that are going to be occupied by an enemy
            randomHexes = map.GetRandomHexes(enemyCount);
            for (int i = 0; i < spawnPoints.Length; i++)
            // First occupy the hexes that definitely must contain an enemy
            {
                if (i > enemyCount - 1) return;
                Hex requiredHex = map.GetHex(spawnPoints[i]);
                if (!map.GetHex(spawnPoints[i]))
                {
                    Debug.Log("A hex provided as a spawn point was not found!");
                    return;
                }
                Enemy _enemy = Instantiate(enemy, map.transform.Find("Characters"));
                _enemy.NewCurrentPosition(map.GetHex(spawnPoints[i]));
                _enemy.transform.position = _enemy.currentPosition.GetPositionOnGround();
                allEnemies.Add(_enemy);

                // If the random hexes we found happened to contain some of the predefined hexes,
                // then we remove those from the random selection.
                // Otherwise we remove one other random hex to reduce the count.
                if (randomHexes.Count == 0) continue;
                if (randomHexes.Contains(requiredHex)) randomHexes.Remove(requiredHex);
                else
                {
                    randomHexes.RemoveAt(randomHexes.Count - 1);
                }
            }
            if (randomHexes.Count == 0) return;
            else
            // There need to be more enemies than the number of predefined spawnpoints.
            // For this we use the random hexes we found before.
            {
                while (randomHexes.Count > 0)
                {
                    Enemy _enemy = Instantiate(enemy, map.transform.Find("Characters"));
                    Hex randomHex = randomHexes[randomHexes.Count - 1];
                    _enemy.NewCurrentPosition(randomHex);
                    _enemy.transform.position = _enemy.currentPosition.GetPositionOnGround();
                    allEnemies.Add(_enemy);
                    randomHexes.RemoveAt(randomHexes.Count - 1);
                }
            }
        }
    }

    public static void CheckEnemies()
    // Checks if we are approaching any enemies that should be added to the turn queue
    /* Called:
     * 1) By MouseManagerGameMode OnEnable(), which is enabled after exiting a movement command or at the beginning of the turn.
    */
    {
        if (allEnemies == null)
        {
            NewPlayerState("EXPLORING");
            Debug.Log("No enemies found.");
            return;
        }
        Enemy closestOutsideEnemy = null;
        int closestOutsideEnemyDistance = int.MaxValue;
        bool anyDetected = false;
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy.hasDetectedPlayer) anyDetected = true;
            if (!nearbyEnemies.Contains(enemy) && enemy.currentPosition.DistanceTo(player.currentPosition) <= queueInDistance)
            {
                if (turnController.enabled == false)
                {
                    player.movementAmount = player.stats.baseMovementAmount;
                    canvas.SetColor(canvas.turnButton, Color.yellow);
                    turnController.enabled = true;
                    nearbyEnemies.Add(enemy);
                    turnController.addToQueue(enemy);
                    turnController.addToQueue(player);
                }
                else
                {
                    nearbyEnemies.Add(enemy);
                    turnController.addToQueue(enemy);
                }

                // Assign the child containing the renderer to layer 19 so that it is rendered with XRay.
                enemy.transform.GetChild(1).gameObject.layer = 19;
            }
            else
            {
                if (!closestOutsideEnemy && !nearbyEnemies.Contains(enemy))
                {
                    closestOutsideEnemy = enemy;
                    closestOutsideEnemyDistance = player.currentPosition.DistanceTo(enemy.currentPosition);
                }
                else if (!nearbyEnemies.Contains(enemy))
                {
                    int distance = player.currentPosition.DistanceTo(enemy.currentPosition);
                    if (distance < closestOutsideEnemyDistance)
                    {
                        closestOutsideEnemyDistance = distance;
                        closestOutsideEnemy = enemy;
                    }
                }
            }
        }

        // The closest wolf outside of combat range will make a sound at this point.
        // Need to check if the sound has already been played because CheckEnemies() is also called
        // at the end of every move command.
        if (closestOutsideEnemy != null && !playedSoundThisTurn)
        {
            closestOutsideEnemy.GetComponent<AudioSource>().Play();
            playedSoundThisTurn = true;
        }

        if (nearbyEnemies.Count == 0)
        {
            NewPlayerState("EXPLORING");
        }
        else
        {
            SetDetectedState(anyDetected);
            if (player.movementAmount <= 0) NewPlayerState("ATTACK");
            else NewPlayerState("MOVE");
        }
    }

    public static float orthoDistancePlus = 0.0f;

    private void OnGUI()
    {
        if (!guiHide)
        {
            if (activeMouseMode == "MapEditing")
            {
                if (GUI.Button(new Rect(10, 10, 70, 50), "SAVE"))
                {
                    SaveLevel();
                }
                orthoDistancePlus = GUI.HorizontalSlider(new Rect(85, 10, 50, 50), orthoDistancePlus, 0f, 100f);
            }
        }
    }

    public void DisableMouse()
    {
        GetComponent<MouseManagerMapEditing>().enabled = false;
        GetComponent<MouseManagerGameMode>().enabled = false;
    }

    public void ReactivateMouse()
    {
        if (activeMouseMode == "MapEditing") GetComponent<MouseManagerMapEditing>().enabled = true;
        else GetComponent<MouseManagerGameMode>().enabled = true;
    }
}


