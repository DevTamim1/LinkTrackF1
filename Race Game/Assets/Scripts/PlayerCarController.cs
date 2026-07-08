using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 20f;      
    public float turnSpeed = 150f;  

    private float moveInput;
    private float turnInput;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($" Please add a Rigidbody to {gameObject.name}!");
            return;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        moveInput = 0f;
        turnInput = 0f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            moveInput = 1f;
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            moveInput = -1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            turnInput = 1f;
        else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            turnInput = -1f;
    }

    [Header("Acceleration Settings")]
    public float accelerationTime = 1f;
    public float decelerationTime = 0.5f;

    private float currentForwardSpeed = 0f;

    void FixedUpdate()
    {
        if (rb == null) return;

        if (Mathf.Abs(moveInput) > 0.05f)
        {
            float targetSpeed = moveInput * speed;
            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, (speed / accelerationTime) * Time.fixedDeltaTime);
        }
        else
        {
            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, 0f, (speed / decelerationTime) * Time.fixedDeltaTime);
        }

        Vector3 targetVelocity = transform.forward * currentForwardSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        if (Mathf.Abs(currentForwardSpeed) > 0.1f)
        {
            float turnAmount = turnInput * turnSpeed * Time.fixedDeltaTime * Mathf.Sign(currentForwardSpeed);
            Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }
}