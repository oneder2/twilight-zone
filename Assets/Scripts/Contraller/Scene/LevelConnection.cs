using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Levels/Connection")]
public class SceneConnection : ScriptableObject
{
    public static SceneConnection ActiveConnection {get; set;}

}