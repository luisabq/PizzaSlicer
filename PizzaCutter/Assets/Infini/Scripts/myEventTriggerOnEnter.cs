using UnityEngine;

public class PlayAnimationOnTrigger : MonoBehaviour
{
    // Drag the GameObject with the Animator component into this slot in the Inspector
    [SerializeField] private Animator objectAnimator; 
    
    // Type the exact name of your Animation State here
    [SerializeField] private string animationStateName; 

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger area is the player
        if (other.CompareTag("Player"))
        {
            // Play the animation immediately
            objectAnimator.Play(animationStateName, 0, 0.0f);
        }
    }
}