using UnityEngine;

public class CharacterDATA : ScriptableObject
{
    public string characterName;
    public float baseHealthAmount;
    public float toughness;
    public int baseMovementAmount;
    public float damageDeal;
}

[CreateAssetMenu(fileName = "EnemyDATA", menuName = "Characters/EnemyDATA")]
public class EnemyDATA : CharacterDATA
{
    public int defaultPlayerDetectDist;
    public int sneakingPlayerDetectDist;
}

[CreateAssetMenu(fileName = "PlayerDATA", menuName = "Characters/PlayerDATA")]
public class PlayerDATA : CharacterDATA
{
    public int baseExploreMovementAmount;
}