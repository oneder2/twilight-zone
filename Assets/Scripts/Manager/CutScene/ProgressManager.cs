// File: Scripts/Manager/CutScene/ProgressManager.cs
using UnityEngine;
using System.Collections.Generic; // Needed for Dictionaries and Lists / 字典和列表所需
using System.Linq; // Needed for LINQ (e.g., for random selection) / LINQ 所需（例如用于随机选择）

// Enum to represent the outcome of dealing with a character
// 用于表示处理角色结果的枚举
public enum CharacterOutcome
{
    NotDealtWith, // 尚未处理 (Not yet dealt with)
    KilledStandard, // 标准击杀/处理 (Standard kill/handling)
    KilledAlternate, // 特殊击杀/处理 (e.g., Crushsis pushed vs jumped) / 特殊击杀/处理（例如 Crushsis 被推下 vs 自己跳下）
    Suicide, // 角色自杀 (e.g., Teacher) / 角色自杀（例如 Teacher）
}


/// <summary>
/// Manages and stores the player's progress through the game loops or key events.
/// 管理并存储玩家在游戏循环或关键事件中的进度。
/// Assumes Singleton pattern and persistence ("Boot" scene).
/// 假设使用单例模式并具有持久性（“Boot”场景）。
/// </summary>
public class ProgressManager : Singleton<ProgressManager>
{
    // --- Loop Progress ---
    [Tooltip("How many full loops the player has completed.\n玩家已完成的完整循环次数。")]
    [SerializeField] private int loopsCompleted = 0;

    // --- Character Progression ---
    [Header("角色进度 (Character Progression)")]
    [Tooltip("Tracks which character is the current main target.\n跟踪哪个角色是当前的主要目标。")]
    [SerializeField] private string currentMainTarget = "Beginner";

    // --- Character Outcomes ---
    [Header("角色结局 (Character Outcomes)")]
    [SerializeField] private CharacterOutcome beginnerOutcome = CharacterOutcome.NotDealtWith;
    [SerializeField] private CharacterOutcome crushsisOutcome = CharacterOutcome.NotDealtWith;
    [SerializeField] private CharacterOutcome friendOutcome = CharacterOutcome.NotDealtWith;
    [SerializeField] private CharacterOutcome crushOutcome = CharacterOutcome.NotDealtWith;
    [SerializeField] private CharacterOutcome teacherOutcome = CharacterOutcome.NotDealtWith;

    // --- Specific Flags ---
    [Header("关键标志 (Key Flags)")]
    [SerializeField] private bool foundFriendLabKey = false;
    [SerializeField] private bool checkedTeacherEvidenceCorrectly = false;
    [SerializeField] private bool hidTeacherEvidence = false;
    [SerializeField] private bool foundCrushClue = false;

    // --- Crush Location Tracking ---
    [Header("Crush 位置 (Crush Location)")]
    [Tooltip("The ID of the location Crush has moved to this loop (Read Only).\n本次循环中 Crush 移动到的位置 ID（只读）。")]
    [SerializeField] private string currentCrushLocationID = "";
    [Tooltip("List of possible location IDs Crush can randomly move to.\nCrush 可以随机移动到的可能位置 ID 列表。")]
    [SerializeField] public List<string> possibleCrushLocationIDs = new List<string> { "NPC_Crush_Rooftop", "NPC_Crush_Classroom2", "NPC_Crush_MeetingRoom", "NPC_Crush_Lab3" }; // Use actual uniqueIDs / 使用实际的 uniqueID

    // --- NEW: Mapping from Location ID to Scene Name ---
    // --- 新增：从位置 ID 到场景名称的映射 ---
    // [Header("Crush 位置到场景映射")]
    [System.Serializable]
    public struct LocationScenePair
    {
        public string locationID; // Matches possibleCrushLocationIDs / 匹配 possibleCrushLocationIDs
        public string sceneName;  // The exact name of the scene file / 场景文件的确切名称
    }
    [Tooltip("Map each possible Crush Location ID to its corresponding scene name.\n将每个可能的 Crush 位置 ID 映射到其对应的场景名称。")]
    [SerializeField] private List<LocationScenePair> crushLocationSceneMap = new List<LocationScenePair>();
    private Dictionary<string, string> _locationToSceneLookup; // Internal lookup / 内部查找字典
    // --- End New ---


    // --- Public Accessors ---
    public int LoopsCompleted => loopsCompleted;
    public string CurrentMainTarget => currentMainTarget;
    public CharacterOutcome BeginnerOutcome => beginnerOutcome;
    public CharacterOutcome CrushsisOutcome => crushsisOutcome;
    public CharacterOutcome FriendOutcome => friendOutcome;
    public CharacterOutcome CrushOutcome => crushOutcome;
    public CharacterOutcome TeacherOutcome => teacherOutcome;
    public bool FoundFriendLabKey => foundFriendLabKey;
    public bool CheckedTeacherEvidenceCorrectly => checkedTeacherEvidenceCorrectly;
    public bool HidTeacherEvidence => hidTeacherEvidence;
    public bool FoundCrushClue => foundCrushClue;
    public string CurrentCrushLocationID => currentCrushLocationID;


    // --- Initialization ---
    protected override void Awake()
    {
        base.Awake();
        BuildLocationSceneLookup(); // Build the lookup dictionary / 构建查找字典
    }

    // --- Build Lookup Dictionary ---
    private void BuildLocationSceneLookup()
    {
        _locationToSceneLookup = new Dictionary<string, string>();
        foreach (var pair in crushLocationSceneMap)
        {
            if (!string.IsNullOrEmpty(pair.locationID) && !string.IsNullOrEmpty(pair.sceneName))
            {
                if (!_locationToSceneLookup.ContainsKey(pair.locationID))
                {
                    _locationToSceneLookup.Add(pair.locationID, pair.sceneName);
                } else { Debug.LogWarning($"[ProgressManager] Duplicate location ID in crushLocationSceneMap: {pair.locationID}"); }
            }
        }
        Debug.Log($"[ProgressManager] Built Location-to-Scene lookup with {_locationToSceneLookup.Count} entries.");
    }


    // --- Public Modifiers ---
    public void CompleteLoop()
    {
        loopsCompleted++;
        Debug.Log($"[ProgressManager] Loop completed! Total loops: {loopsCompleted}");
        SetCurrentMainTarget("Beginner");
        foundCrushClue = false;
        currentCrushLocationID = "";
        ResetCharacterOutcomes();
        EventManager.Instance?.TriggerEvent(new LoopCompletedEvent(loopsCompleted));
    }

    public void SetCurrentMainTarget(string targetName)
    {
        currentMainTarget = targetName;
        Debug.Log($"[ProgressManager] Current main target set to: {currentMainTarget}");
    }

    public void SetCharacterOutcome(string characterName, CharacterOutcome outcome)
    {
        Debug.Log($"[ProgressManager] Setting outcome for {characterName} to {outcome}");
        switch (characterName.ToLower())
        {
            case "beginner": beginnerOutcome = outcome; break;
            case "crushsis":
                crushsisOutcome = outcome;
                EventManager.Instance?.TriggerEvent(new CrushsisOutcomeEvent(outcome));
                break;
            case "friend":
                friendOutcome = outcome;
                EventManager.Instance?.TriggerEvent(new FriendOutcomeEvent(outcome));
                break;
            case "crush":
                crushOutcome = outcome;
                 EventManager.Instance?.TriggerEvent(new CrushOutcomeEvent(outcome));
                break;
            case "teacher":
                teacherOutcome = outcome;
                 EventManager.Instance?.TriggerEvent(new TeacherOutcomeEvent(outcome));
                break;
            default:
                Debug.LogWarning($"[ProgressManager] SetCharacterOutcome called with unknown character name: {characterName}");
                break;
        }
    }

    public void SetFoundFriendLabKey(bool found)
    {
        foundFriendLabKey = found;
        Debug.Log($"[ProgressManager] FoundFriendLabKey set to: {found}");
    }

    public void SetCheckedTeacherEvidenceCorrectly(bool correctly)
    {
        checkedTeacherEvidenceCorrectly = correctly;
        Debug.Log($"[ProgressManager] CheckedTeacherEvidenceCorrectly set to: {correctly}");
    }

    public void SetHidTeacherEvidence(bool hidden)
    {
        hidTeacherEvidence = hidden;
        Debug.Log($"[ProgressManager] HidTeacherEvidence set to: {hidden}");
    }

    public void SetFoundCrushClue(bool found)
    {
        foundCrushClue = found;
        Debug.Log($"[ProgressManager] FoundCrushClue set to: {found}");
    }

    public void SetCurrentCrushLocationID(string locationID)
    {
        currentCrushLocationID = locationID;
        Debug.Log($"[ProgressManager] Crush location set to: {currentCrushLocationID}");
    }

    public void DetermineCrushLocationForLoop()
    {
         if (possibleCrushLocationIDs != null && possibleCrushLocationIDs.Count > 0)
         {
              int randomIndex = Random.Range(0, possibleCrushLocationIDs.Count);
              SetCurrentCrushLocationID(possibleCrushLocationIDs[randomIndex]);
         } else {
              Debug.LogError("[ProgressManager] No possible Crush locations defined in possibleCrushLocationIDs list!");
              SetCurrentCrushLocationID("");
         }
    }

    // --- NEW: Get Scene Name for Location ---
    // --- 新增：获取位置对应的场景名称 ---
    /// <summary>
    /// Gets the scene name associated with a given Crush location ID.
    /// 获取与给定 Crush 位置 ID 关联的场景名称。
    /// </summary>
    /// <param name="locationID">The unique ID of the Crush location NPC. / Crush 位置 NPC 的唯一 ID。</param>
    /// <returns>The name of the scene, or null if the ID is not mapped. / 场景名称，如果 ID 未映射则为 null。</returns>
    public string GetSceneNameForCrushLocation(string locationID)
    {
        if (_locationToSceneLookup == null) BuildLocationSceneLookup(); // Ensure lookup is built / 确保查找字典已构建

        if (_locationToSceneLookup.TryGetValue(locationID, out string sceneName))
        {
            return sceneName;
        }
        Debug.LogError($"[ProgressManager] No scene name mapped for Crush location ID: {locationID}");
        return null; // Or return a default scene name / 或返回默认场景名称
    }
    // --- End New ---


    public void ResetProgress()
    {
        loopsCompleted = 0;
        currentMainTarget = "Beginner";
        ResetCharacterOutcomes();
        foundFriendLabKey = false;
        checkedTeacherEvidenceCorrectly = false;
        hidTeacherEvidence = false;
        foundCrushClue = false;
        currentCrushLocationID = "";
        Debug.Log("[ProgressManager] All progress reset to initial state.");
    }

    private void ResetCharacterOutcomes()
    {
        beginnerOutcome = CharacterOutcome.NotDealtWith;
        crushsisOutcome = CharacterOutcome.NotDealtWith;
        friendOutcome = CharacterOutcome.NotDealtWith;
        crushOutcome = CharacterOutcome.NotDealtWith;
        teacherOutcome = CharacterOutcome.NotDealtWith;
         Debug.Log("[ProgressManager] Character outcomes reset.");
    }
}
