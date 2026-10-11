
using UnityEngine;

public class PlayerPersistence : MonoBehaviour
{
    private static PlayerPersistence instance;

    private void Awake()
    {
        // no duplicates
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // attempt #4 on keeping playeressentials thru scene changes
        DontDestroyOnLoad(gameObject);
    }
}
