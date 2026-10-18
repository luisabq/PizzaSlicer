using UnityEngine;
using UnityEngine.AI;

public class AnimKaren : MonoBehaviour

{
    Animator animator;
    private NavMeshAgent agent;
    float speed; 



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();

        agent = GetComponentInParent<NavMeshAgent>();

    }

    // Update is called once per frame
    void Update()
    {
        speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }
}