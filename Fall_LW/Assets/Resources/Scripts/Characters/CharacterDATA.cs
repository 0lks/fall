using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDATA", menuName = "Characters/CharacterDATA")]
public class CharacterDATA : ScriptableObject
{
    public bool friendly;
    public string characterName;
    public float baseHealthAmount;
    public float toughness;
    public int baseMovementAmount;
    public float damageDeal;
    public int defaultPlayerDetectDist;
    public int sneakingPlayerDetectDist;
    //Player only
    public int baseExploreMovementAmount;
}
