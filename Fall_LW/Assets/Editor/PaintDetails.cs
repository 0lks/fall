using UnityEngine;
using UnityEditor;

public class PaintDetails : EditorWindow
{
    int selectedAction = 0;
    private Object PaintObject;
    //public Object Parent;
    private Object Parent;
    static float objMaxScale = 1.0f;
    static float objMaxScaleUpX = 0.0f;
    static float objMaxScaleUpY = 0.0f;
    static float objMaxScaleUpZ = 0.0f;
    public string[] options;
    public int index = 0;
    GameObject parent;

    // Spawnable objects
    Object[] stuff;

    [MenuItem("Window/PaintDetails")]
    static void Init()
    {
        PaintDetails window = (PaintDetails)GetWindow(typeof(PaintDetails));
        window.Show();
    }

    void OnGUI()
    {
        float width = position.width - 5;
        float height = 50;

        int optionsCount = stuff.Length;
        options = new string[optionsCount];
        for (int i = 0; i < optionsCount; i++)
        {
            options[i] = stuff[i].name;
        }

        string[] actionLabels = new string[] { "DONT PAINT", "Paint"};
        selectedAction = GUILayout.SelectionGrid(selectedAction, actionLabels, 2, GUILayout.Width(width), GUILayout.Height(height));
        if (selectedAction == 1)
        {
            foreach (Object obj in stuff)
            {
                GameObject obj_ = (GameObject) obj;
                obj_.GetComponent<LODGroup>().enabled = false;
            }
        }
        else
        {
            foreach (Object obj in stuff)
            {
                GameObject obj_ = (GameObject)obj;
                obj_.GetComponent<LODGroup>().enabled = true;
            }
        }
        Parent = GameObject.FindGameObjectWithTag("GroundObjectsParent");
        GUILayout.Label("Random upscale limit:");
        objMaxScale = EditorGUILayout.Slider(objMaxScale, 1f, 4f);

        GUILayout.Label("Random upscale limit (X axis):");
        objMaxScaleUpX = EditorGUILayout.Slider(objMaxScaleUpX, 0f, 2f);
        GUILayout.Label("Random upscale limit (Y axis):");
        objMaxScaleUpY = EditorGUILayout.Slider(objMaxScaleUpY, 0f, 2);
        GUILayout.Label("Random upscale limit (Z axis):");
        objMaxScaleUpZ = EditorGUILayout.Slider(objMaxScaleUpZ, 0f, 2f);
        parent = (GameObject)Parent;
        GUILayout.Label("Selected object:");
        index = EditorGUILayout.Popup(index, options);
        PaintObject = stuff[index];
    }

    void OnScene(SceneView sceneview)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && Event.current.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (selectedAction == 1)
            {
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, 1 << 11 | 1 << 18))
                {
                    if (!PaintObject)
                    {
                        Debug.Log("Define the object first.");
                        return;
                    }
                    else
                    {
                        GameObject obj = (GameObject) PrefabUtility.InstantiatePrefab(PaintObject);
                        //Vector3 originalRotation = obj.transform.rotation.eulerAngles;
                        obj.transform.position = hitInfo.point;
                        Vector3 normal = hitInfo.normal;
                        obj.transform.up = normal;
                        //obj.transform.Rotate(new Vector3(1,0,0), 90f); // Necessary because the rotation of the Imported model gets reset by "obj.transform.up = normal;"
                        
                        //float randomRot = Random.Range(1f, 179f);
                        //obj.transform.RotateAround(obj.transform.position, obj.transform.forward, randomRot);
                        float randomScale = Random.Range(1f, objMaxScale);
                        obj.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                        obj.transform.localScale = new Vector3
                            (
                            obj.transform.localScale.x + Random.Range(0f, objMaxScaleUpX),
                            obj.transform.localScale.x + Random.Range(0f, objMaxScaleUpY),
                            obj.transform.localScale.x + Random.Range(0f, objMaxScaleUpZ)
                            );


                        Transform target = parent.transform;
                        if (obj.tag == "Rock")
                        {
                            target = target.Find("Rocks");
                        }
                        else if (obj.tag == "Plant")
                        {
                            target = target.Find("Plants");
                        }
                        // ...

                        obj.transform.SetParent(target);

                        Undo.RegisterCreatedObjectUndo(obj, "Paint object");
                    }

                }
            }
        }
    }
    void OnEnable()
    {
        SceneView.onSceneGUIDelegate -= OnScene;
        SceneView.onSceneGUIDelegate += OnScene;

        stuff = Resources.LoadAll("Prefabs/Painter");
    }
}
