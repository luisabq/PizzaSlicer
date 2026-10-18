using UnityEngine;

public class AnimationStateControllerPlayer : MonoBehaviour

{
    Animator animator;

    private CharacterController cc;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();


    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Update is running");

        Vector3 horizontalVelocity = cc.velocity;
        horizontalVelocity.y = 0;

        float horizontalSpeed = horizontalVelocity.magnitude;

        animator.SetFloat("Speed", horizontalSpeed);

       // Debug.Log("Speed: " + horizontalSpeed);
    }
}