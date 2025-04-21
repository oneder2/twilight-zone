using UnityEngine;
using System.Collections;

/// <summary>
/// Abstract base class for defining cutscene sequences as ScriptableObjects.
/// Each derived class represents a specific cutscene sequence.
/// </summary>
public abstract class CutsceneSequence : ScriptableObject
{
    /// <summary>
    /// Executes the cutscene sequence logic.
    /// Should be implemented by derived classes as a coroutine.
    /// </summary>
    /// <param name="player">The CutscenePlayer instance executing this sequence.</param>
    /// <returns>An IEnumerator for the sequence execution.</returns>
    public abstract IEnumerator Play(CutscenePlayer player);

    /// <summary>
    /// Applies the final state instantly when the cutscene is skipped.
    /// Should be implemented by derived classes.
    /// </summary>
    /// <param name="player">The CutscenePlayer instance executing this sequence.</param>
    public abstract void ApplySkipState(CutscenePlayer player);
}
