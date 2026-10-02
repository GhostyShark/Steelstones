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
    public float steeringAcceleration = 2.5f;
    public float steeringReturnSpeed = 4f;

    [Range(0f, 1f)]
    public float minimumSteeringStrength = 0.1f;

    private float currentSpeed = 0f;
    private float currentSteering = 0f;

    private Rigidbody carRigidbody;

    void Start()
    {
        // Get Rigidbody from the cube/car
        carRigidbody = cube.GetComponent<Rigidbody>();

        if (carRigidbody == null)
        {
            Debug.LogError("Er zit geen Rigidbody op de Cube!");
            return;
        }

        // Better collision detection
        carRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        // Prevent the car from falling/tipping over
        carRigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleSteering();
    }

    void HandleMovement()
    {
        if (carRigidbody == null)
            return;

        float input = 0f;

        if (Input.GetKey(KeyCode.W))
            input += 1f;

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

        Vector3 forward = forwardReference.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 newPosition =
            carRigidbody.position +
            forward * currentSpeed * Time.fixedDeltaTime;

        carRigidbody.MovePosition(newPosition);
    }

    void HandleSteering()
    {
        if (carRigidbody == null)
            return;

        float steeringInput = 0f;

        if (Input.GetKey(KeyCode.A))
            steeringInput -= 1f;

        if (Input.GetKey(KeyCode.D))
            steeringInput += 1f;

        float steeringRate = steeringInput != 0f
            ? steeringAcceleration
            : steeringReturnSpeed;

        currentSteering = Mathf.MoveTowards(
            currentSteering,
            steeringInput,
            steeringRate * Time.fixedDeltaTime
        );

        float speedPercentage = Mathf.Clamp01(
            Mathf.Abs(currentSpeed) / maxSpeed
        );

        float steeringStrength = Mathf.Lerp(
            minimumSteeringStrength,
            1f,
            speedPercentage
        );

        if (Mathf.Abs(currentSpeed) < 0.05f)
            return;

        float rotationAmount =
            currentSteering *
            maxRotationSpeed *
            steeringStrength *
            Time.fixedDeltaTime;

        if (currentSpeed < 0f)
            rotationAmount *= -1f;

        Quaternion rotation =
            Quaternion.Euler(0f, rotationAmount, 0f);

        carRigidbody.MoveRotation(
            carRigidbody.rotation * rotation
        );
    }

    void OnCollisionEnter(Collision collision)
    {
        // Lose some speed when crashing
        currentSpeed *= 0.25f;
    }
}