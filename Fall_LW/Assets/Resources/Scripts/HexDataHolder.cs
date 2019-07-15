using UnityEngine;
namespace HexData
{
    [CreateAssetMenu(fileName = "HexDataHolder", menuName = "Hex/HexDataHolder")]
    public class HexDataHolder : ScriptableObject
    {
        public Material walkable_Edge;
        public Material black_Edge;
        public Material base_Center;
        public Material emptyCenter;
        public Material hover_Edge;
        public Material editingHexMaterialEdge;
        public Material editingHexMaterialCenter;
        public Material hex_25_Edge;
        public Material hex_25_Center;
        public Material hex_50_Edge;
        public Material hex_50_Center;
        public Material hex_75_Edge;
        public Material hex_75_Center;
        public Material hex_100_Edge;
        public Material hex_100_Center;
        //public Material hex_Danger_Edge;
        public Material hex_Danger_Center;

        public GameObject hexPrefab;
        [HideInInspector]
        public float hexWidth;

        public void SetVariables()
        {
            hexWidth = hexPrefab.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.size.x;
            walkable_Edge.enableInstancing = false;
            black_Edge.enableInstancing = false;
            base_Center.enableInstancing = false;
            emptyCenter.enableInstancing = false;
            hover_Edge.enableInstancing = false;
            editingHexMaterialEdge.enableInstancing = false;
            editingHexMaterialCenter.enableInstancing = false;
            hex_25_Edge.enableInstancing = false;
            hex_25_Center.enableInstancing = false;
            hex_50_Edge.enableInstancing = false;
            hex_50_Center.enableInstancing = false;
            hex_75_Edge.enableInstancing = false;
            hex_75_Center.enableInstancing = false;
            hex_100_Edge.enableInstancing = false;
            hex_100_Center.enableInstancing = false;
            //public Material hex_Danger_Edge;
            hex_Danger_Center.enableInstancing = false;
        }
    }
}
