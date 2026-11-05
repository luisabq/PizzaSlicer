using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    
    [SerializeField] private GameObject transitionCanvasObject;
    [SerializeField] private Animator transitionAnimator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(ExecuteTransition(sceneName));
    }

    private IEnumerator ExecuteTransition(string sceneName)
    {
       
        if (transitionCanvasObject != null) transitionCanvasObject.SetActive(true);
        if (transitionAnimator != null)
        {
            transitionAnimator.enabled = true;
            transitionAnimator.Rebind();
            transitionAnimator.Update(0f);
            transitionAnimator.SetTrigger("Wipe");
        }

        //idk how this stuff really works man
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        //DELAY FOR TRANSITION
        float wipeDuration = GetAnimationClipLength("ScreenWipe");


        float halfDuration = wipeDuration > 0f ? wipeDuration * 0.5f : 0.5f;

        //delay waiting for halfpoint of screentransition
        float elapsed = 0f;
        while (elapsed < halfDuration || asyncLoad.progress < 0.9f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }


        //screen transition should be at halfpoint now, aka covered
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

       //lets last half of animation play out
        yield return new WaitForSecondsRealtime(halfDuration);
        yield return new WaitForSecondsRealtime(halfDuration);

        //hides it until needed again
        if (transitionCanvasObject != null) transitionCanvasObject.SetActive(false);
    }



    private float GetAnimationClipLength(string clipName)
    {
       

        foreach (var clip in transitionAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }
        return 0f;
    }

}






