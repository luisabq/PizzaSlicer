using UnityEngine;

public class UIAnimation : MonoBehaviour
{
    
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        
    }
}
