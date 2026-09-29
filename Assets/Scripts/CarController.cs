using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("References")]
    public Transform cube;
    public Transform forwardReference;

    [Header("Movement Settings")]
    public float maxSpeed = 4f;
    public float acceleration = 2f;
    public float deceleration = 1.5f;

    [Header("Steering Settings")]
    public float maxRotationSpeed = 120f;

    // How quickly the steering reaches full strength
    public float steeringAcceleration = 2.5f;

    // How quickly the steering returns to center
    public float steeringReturnSpeed = 4f;

    // Minimum percentage of steering available at very low speed
    [Range(0f, 1f)]
    public float minimumSteeringStrength = 0.1f;

    private float currentSpeed = 0f;
    private float currentSteering = 0f;

    void Update()
    {
        HandleSteering();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        Vector3 forward = forwardReference.forward;
        forward.y = 0f;
        forward.Normalize();

        float input = 0f;

        // W = Forward
        if (Input.GetKey(KeyCode.W))
            input += 1f;

        // S = Reverse
        if (Input.GetKey(KeyCode.S))
            input -= 1f;

        float targetSpeed = input * maxSpeed;

        float rate = Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)
            ? acceleration
            : deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            rate * Time.fixedDeltaTime
        );

        cube.position += forward * currentSpeed * Time.fixedDeltaTime;
    }

    void HandleSteering()
    {
        float steeringInput = 0f;

        // A = Left
        if (Input.GetKey(KeyCode.A))
            steeringInput -= 1f;

        // D = Right
        if (Input.GetKey(KeyCode.D))
            steeringInput += 1f;

        // Smoothly build up steering instead of instantly turning
        float steeringRate = steeringInput != 0f
            ? steeringAcceleration
            : steeringReturnSpeed;

        currentSteering = Mathf.MoveTowards(
            currentSteering,
            steeringInput,
            steeringRate * Time.deltaTime
        );

        // Calculate how fast the car is moving compared to max speed
        float speedPercentage = Mathf.Clamp01(
            Mathf.Abs(currentSpeed) / maxSpeed
        );

        // Very little steering at low speed, more steering at higher speed
        float steeringStrength = Mathf.Lerp(
            minimumSteeringStrength,
            1f,
            speedPercentage
        );

        // Prevent steering while practically stationary
        if (Mathf.Abs(currentSpeed) < 0.05f)
            return;

        float rotationAmount =
            currentSteering *
            maxRotationSpeed *
            steeringStrength *
            Time.deltaTime;

        // Reverse steering direction when driving backwards
        if (currentSpeed < 0f)
            rotationAmount *= -1f;

        cube.Rotate(0f, rotationAmount, 0f);
    }
}