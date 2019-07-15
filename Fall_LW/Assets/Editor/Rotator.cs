using UnityEditor;
using UnityEngine;

public class Rotator : EditorWindow
{
    public Object objectToRotate;
    public int index = 0;
    public float rotAmount = 0;

    [MenuItem("Window/Rotator")]
    static void init()
    {
        Rotator window = (Rotator) GetWindow(typeof(Rotator));
        window.Show();
    }

    void OnGUI()
    {
        objectToRotate = EditorGUILayout.ObjectField("Object to rotate", objectToRotate, typeof(GameObject), true);
        string[] options = new string[] { "X", "Y", "Z" };
        index = EditorGUILayout.Popup(index, options);
        rotAmount = EditorGUILayout.Slider(rotAmount, -90f, 90f);

        if (GUILayout.Button("Rotate"))
        {
            object[] sceneObjects = GameObject.FindSceneObjectsOfType(typeof(GameObject));
            foreach (Object obj in sceneObjects)
            {
                GameObject gObj = (GameObject) obj;
                if (gObj.name == objectToRotate.name)
                {
                    Debug.Log(gObj.name);
                    Quaternion currentRotation = gObj.transform.rotation;
                    if (index == 0)
                    {
                        gObj.transform.Rotate(new Vector3(1, 0, 0), rotAmount, Space.Self);
                    }
                    else if (index == 1)
                    {
                        gObj.transform.Rotate(new Vector3(0, 1, 0), rotAmount, Space.Self);
                    }
                    else if (index == 2)
                    {
                        gObj.transform.Rotate(new Vector3(0, 0, 1), rotAmount, Space.Self);
                    }
                }
            }
        }
    }

}
