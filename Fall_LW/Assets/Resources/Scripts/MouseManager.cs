using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class MouseManager : MonoBehaviour
{
    protected void Start() { }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {}
}