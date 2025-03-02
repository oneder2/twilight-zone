using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Levels/Connection")]
public class LevelConnection : ScriptableObject
{
    public static LevelConnection ActiveConnection {get; set;}

}