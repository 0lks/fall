﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// Internal dependencies
using FALL.Characters;

namespace FALL.Core {
    public class GameControl : MonoBehaviour
    {
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
        public GameObject canvasGameObject;
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
        public int[,] playerSpawnPoint = new int[1, 3];
        public int playerSpawnX;
        public int playerSpawnY;
        public int playerSpawnZ;
        string sceneName;
        /*
        * Enemy Spawning
        */
        [HideInInspector] public bool POPULATE = false;
        public static int enemyCount = 0;
        [HideInInspector] public int predefinedSpawnsCount;
        [HideInInspector] public bool distributeRandomly;
        //public int[,] predefinedSpawnPoints;
        Stack<int[]> predefinedSpawnPoints = new Stack<int[]>();
        //Input
        public string[] predefinedSpawnPointsAsStrings;
        // // // // // // // // // // // // // // // 
        public enum PlayerState { Exploring, Move, Attack }
        //private static string _playerState = "EXPLORING";
        private static PlayerState _playerState = PlayerState.Exploring;
        public static PlayerState playerState
        {
            get { return _playerState; }
            private set { NewPlayerState(value); }
        }
        public static void NewPlayerState(PlayerState state)
        {
            PlayerState prevstate = _playerState;
            _playerState = state;
            if (_playerState == PlayerState.Move)
            {
                player.currentPosition.HighLightSurroundingMoveState(player.movementAmount);
                player.currentPosition.Highlight();
                canvas.SetColor(canvas.moveButton, Color.green);
                canvas.SetColor(canvas.attackButton, Color.white);
                canvas.EnableButtons();
            }
            else if (_playerState == PlayerState.Attack)
            {
                player.currentPosition.HighLightSurroundingAttackState(player.weapon.attackDistance);
                player.currentPosition.Highlight();
                canvas.SetColor(canvas.attackButton, Color.green);
                canvas.SetColor(canvas.moveButton, Color.white);
                canvas.EnableButtons();
            }

            else if (_playerState == PlayerState.Exploring)
            {
                playedSoundThisTurn = false;
                canvas.SetColor(canvas.turnButton, Color.white);
                player.movementAmount = player.GetBaseExploreMovementAmount();
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

        [HideInInspector] public static Camera mainCamera;
        [HideInInspector] public static Camera orthoCamera;
        public static GameObject discardPool;
        private GameObject wolf;

        public static string activeMouseMode;


        void Awake()
        {
            hexPrefab = (GameObject)Resources.Load("Prefabs/Other/Hex/Hex");
            wolf = (GameObject)Resources.Load("Prefabs/Characters/Wolf/Wolf");
            queueInDistance = _queueInDistance;
            Application.targetFrameRate = 60;
            //QualitySettings.vSyncCount = 0;
            playerSpawnPoint[0, 0] = playerSpawnX;
            playerSpawnPoint[0, 1] = playerSpawnY;
            playerSpawnPoint[0, 2] = playerSpawnZ;
            sceneName = SceneManager.GetActiveScene().name;

                predefinedSpawnsCount = predefinedSpawnPointsAsStrings.Length;
            foreach (string spawnPoint_ in predefinedSpawnPointsAsStrings)
            {
                string[] spawnPoint = spawnPoint_.Split('_');
                int[] spawnPointAsInts = System.Array.ConvertAll(spawnPoint, int.Parse);
                predefinedSpawnPoints.Push(spawnPointAsInts);
            }

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
            //canvas = GameObject.FindGameObjectWithTag("Canvas").GetComponent<CanvasEventHandler>();
            canvasGameObject.SetActive(true);
            canvas = canvasGameObject.GetComponent<CanvasEventHandler>();
            editingMouse = gameObject.AddComponent<MouseManagerMapEditing>();
            playMouse = gameObject.AddComponent<MouseManagerGameMode>();
            allEnemies = new List<Enemy>();
            nearbyEnemies = new List<Enemy>();
            turnController = gameObject.AddComponent<TurnController>();

            Camera[] allCameras = Camera.allCameras;
            mainCamera = allCameras[0];
            orthoCamera = allCameras[1];
            mainCamera.transform.gameObject.SetActive(false);
            //orthoCamera.transform.gameObject.SetActive(true);
            orthoCamera.transform.gameObject.SetActive(false);
            LoadLevel();
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
        /*
        private void RenderHexes(int dist)
        {
            foreach (Hex hex in GameControl.player.currentPosition.GetDistantNeighbours(dist))
            {
                hex.GetComponentInChildren<MeshRenderer>().enabled = true;
            }
        }
        */
        public static HashSet<Hex> playerSurroundingHexes = new HashSet<Hex>();
        private void RenderHexes(int dist)
        // TODO: Move dependency on Player and Hex downstream
        {
            foreach (Hex hex in map.GetAllHexes()) hex.gameObject.SetActive(false);
            foreach (Hex hex in player.currentPosition.GetDistantNeighbours(dist, true))
            {
                hex.gameObject.SetActive(true);
                playerSurroundingHexes.Add(hex);
            }
        }

        public void InitilizeBaseGameState()
        // Called by StartGame(), resets the game to its original state
        {
            canvas.EnableButtons();
            canvas.ResetColors();
            player.GetComponent<Animator>().SetFloat("MovementSpeed", 20f);
            activeMouseMode = "Game";
            if (map.addedHexesThisSession)
            {
                map.WaitAndConstructGraph();
            }
            /*
            if (playerSpawnPoint != null && map.HexExists(playerSpawnPoint))
            {
                player.NewCurrentPosition(map.GetHex(playerSpawnPoint));
            }
            else player.NewCurrentPosition(map.GetAllHexes()[0]);
            */
            if (playerSpawnPoint != null && map.HexExists(playerSpawnPoint[0, 0], playerSpawnPoint[0, 1], playerSpawnPoint[0, 2]))
            {
                player.NewCurrentPosition(map.GetHex(playerSpawnPoint[0, 0], playerSpawnPoint[0, 1], playerSpawnPoint[0, 2]));
            }
            else
            {
                Debug.Log("Spawning nowhere");
                player.NewCurrentPosition(map.GetAllHexes()[0]);
            }

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
            player.remainingHealth = player.GetBaseHealthAmount();
            player.gameObject.SetActive(true);
            player.healthBar = canvas.hpBar.GetComponentInChildren<SimpleHealthBar>();
            player.healthBar.UpdateBar(player.remainingHealth, player.GetBaseHealthAmount());

            NewPlayerState(PlayerState.Exploring);

            //Debug.Log(queueInDistance + 10);
            RenderHexes(queueInDistance + 10);
        }

        public void SaveLevel()
        {
            Debug.Log("Saving level...");
            string fileName = sceneName + ".dat";
            File.Delete(fileName);
            map.Serialize(fileName);
        }

        private void LoadLevel()
        {
            if (File.Exists(sceneName + ".dat"))
            {
                using (Stream stream = File.Open(sceneName + ".dat", FileMode.Open))
                {
                    var bformatter = new BinaryFormatter();
                    MapSerializedContent deserializedMap = (MapSerializedContent)bformatter.Deserialize(stream);
                    map.SetMap(deserializedMap);

                    stream.Close();
                }
            }
            else
            // No existing hex map was found for this scene.
            // Setting up a new one with the first hex at the
            // center of the terrain with a coordinate of 0_0_0.
            {
                Vector3 terrainSize = terrain.terrainData.size;
                Vector3 terrainCenter = new Vector3(terrainSize.x / 2, 510, terrainSize.z / 2);
                GameObject firstHex_ = Instantiate(hexPrefab, terrainCenter, Quaternion.identity, GameControl.map.transform.Find("Hexes"));
                Hex firstHex = firstHex_.GetComponent<Hex>();
                firstHex.x = 0;
                firstHex.y = 0;
                firstHex.z = 0;
                firstHex.setId();
                firstHex.name = firstHex.id;
                map.AddHexToMap(firstHex);
                //firstHex.Highlight();
                map.vertexDisplacer.DisplaceVertices(firstHex);
                orthoCamera.transform.position = new Vector3(
                    firstHex.transform.position.x,
                    orthoCamera.transform.position.y,
                    firstHex.transform.position.z);
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
                //randomHexes = map.GetRandomHexes(enemyCount);
                /*
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
                */

                int totalSpawnCounter = enemyCount;
                int predefinedSpawnCounter = predefinedSpawnsCount;
                HashSet<Hex> usedSpawns = new HashSet<Hex>();

                while (totalSpawnCounter-- > 0)
                {
                    if (predefinedSpawnCounter-- > 0)
                    // There are some unoccupied points where enemies have to spawn
                    {
                        int[] spawnPoint = predefinedSpawnPoints.Pop();
                        if (map.HexExists(spawnPoint[0], spawnPoint[1], spawnPoint[2]))
                        {
                            Hex spawn = map.GetHex(spawnPoint[0], spawnPoint[1], spawnPoint[2]);
                            Enemy _enemy = Instantiate(enemy, map.transform.Find("Characters"));
                            _enemy.NewCurrentPosition(map.GetHex(spawnPoint[0], spawnPoint[1], spawnPoint[2]));
                            _enemy.transform.position = _enemy.currentPosition.GetPositionOnGround();
                            allEnemies.Add(_enemy);
                            usedSpawns.Add(spawn);
                        }
                        else
                        {
                            Debug.Log("Could not find the hex corresponding to the coordinates provided as a spawn point.");
                            //... find random (see below: copy over?)
                        }
                    }

                    else
                    {
                        //TODO: ADD A MAP METHOD TO GET JUST ONE RANDOM HEX
                        Hex randomHex = map.GetRandomHexes(1)[0];
                        //TODO: ADD A CHECK TO PREVENT ENDLESS LOOPS IN A SMALLER MAP WHERE THIS RETURNS THE SAME HEXES AGAIN AND AGAIN
                        while (usedSpawns.Contains(randomHex))
                        {
                            //Debug.Log(randomHex.name);
                            randomHex = map.GetRandomHexes(1)[0];
                        }

                        Enemy _enemy = Instantiate(enemy, map.transform.Find("Characters"));
                        _enemy.NewCurrentPosition(randomHex);
                        _enemy.transform.position = _enemy.currentPosition.GetPositionOnGround();
                        allEnemies.Add(_enemy);
                        usedSpawns.Add(randomHex);
                    }
                }
            }
        }

        public static void SetLayerRecursively(Transform root, int layerNumber)
        {
            foreach (Transform trans in root.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
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
                NewPlayerState(PlayerState.Exploring);
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
                        player.movementAmount = player.GetBaseMovementAmount();
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
                    //enemy.transform.GetChild(0).gameObject.layer = 19;
                    SetLayerRecursively(enemy.transform.GetChild(0), 19);
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
                NewPlayerState(PlayerState.Exploring);
            }
            else
            {
                SetDetectedState(anyDetected);
                if (player.movementAmount <= 0) NewPlayerState(PlayerState.Attack);
                else NewPlayerState(PlayerState.Move);
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

                    /*
                    if (GUI.Button(new Rect(140, 10, 70, 50), "RESET MAP"))
                    {
                        List<Hex> allHexes = map.GetAllHexes();
                        Hex survivorHex = map.GetRandomHexes(1)[0];
                        allHexes.Remove(survivorHex);

                        foreach (Hex hex in allHexes)
                        {
                            hex.DeleteHex();
                        }

                        survivorHex.x = 0;
                        survivorHex.y = 0;
                        survivorHex.z = 0;
                        survivorHex.id = "0_0_0";
                    }
                    */

                    
                    if (GUI.Button(new Rect(220, 10, 140, 50), "Recalculate Vertices"))
                    {
                        HexVertexDisplacer displacer = GameControl.map.vertexDisplacer;
                        List<Hex> allHexes = map.GetAllHexes();
                        foreach (Hex hex in allHexes)
                        {
                            displacer.DisplaceVertices(hex);
                        }
                    }
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
}
