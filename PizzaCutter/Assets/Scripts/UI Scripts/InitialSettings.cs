using UnityEngine;

public class InitialSettings : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //FPS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;


        //Portrait mode only
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;

        Screen.orientation = ScreenOrientation.Portrait;
    }

}
