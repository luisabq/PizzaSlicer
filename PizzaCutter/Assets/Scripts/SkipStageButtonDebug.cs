using UnityEngine;

public class DebugSkipStageButton : MonoBehaviour
{
    public void SkipToNextStage()
    {
        LevelManager levelManager = FindAnyObjectByType<LevelManager>();

        if (levelManager == null)
        {
            Debug.LogError("[DEBUG] Could not find an active LevelManager!");
            return;
        }

        levelManager.DebugSkipToNextStage();
    }
}