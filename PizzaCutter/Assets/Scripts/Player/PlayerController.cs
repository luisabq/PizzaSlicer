using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]

    [SerializeField] private float movementSpeed = 8f;
    [SerializeField] private float rotationSpeed = 100f;

    //minimum distance dragged before player starts moving
    [SerializeField] private float touchDeadzone = 10f;

    private CharacterController controller;
    private Vector2 keyboardInput;
    private Vector2 touchStartPosition;

    private void Awake()
    {
       controller = GetComponent<CharacterController>();
        EnhancedTouchSupport.Enable();
       TouchSimulation.Enable();
    }

    public void OnMove(InputValue value)
    {
        keyboardInput = value.Get<Vector2>();
    }

    private void ApplyMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        //Touch
        if (Touch.activeTouches.Count > 0)
        {
            Touch activeTouch = Touch.activeTouches[0];

            //Finds drag origin point
            if (activeTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                touchStartPosition = activeTouch.screenPosition;
            }

            //Gets direction from drag start to current finger position
            Vector2 currentPosition = activeTouch.screenPosition;
            Vector2 touchOffset = currentPosition - touchStartPosition;

            //Normalizes if beyond deadzone
            if (touchOffset.magnitude > touchDeadzone)
            {
                Vector2 normalizedTouchDir = touchOffset.normalized;
                moveDirection = new Vector3(normalizedTouchDir.x, 0f, normalizedTouchDir.y);
            }
        }
        //WASD
        else if (keyboardInput.magnitude > 0.01f)
        {
          
            moveDirection = new Vector3(keyboardInput.x, 0f, keyboardInput.y);
            if (moveDirection.magnitude > 1f) moveDirection.Normalize();
        }


        if (moveDirection.magnitude > 0.01f)
        {
            controller.Move(moveDirection * movementSpeed * Time.deltaTime);
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void Start()
    {
        
    }

    void Update()
    {
        ApplyMovement();
    }
}
