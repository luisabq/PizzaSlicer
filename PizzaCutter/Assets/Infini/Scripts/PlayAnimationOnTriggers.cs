using UnityEngine;

public class PlayAnimationOnTriggers : MonoBehaviour
{
    [SerializeField] private Animator objectAnimator; 
    
    [SerializeField] private string animationStateName; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            objectAnimator.Play(animationStateName, 0, 0.0f);
        }
    }
}