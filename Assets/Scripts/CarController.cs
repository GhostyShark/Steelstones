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

    [Header("Boost Settings")]
    public float boostSpeed = 10f;

    [Header("Drift Settings")]
    public float driftTurnMultiplier = 1.7f;
    public float driftSideSpeed = 2f;
    public float driftSpeedLoss = 0.5f;

    private float currentSpeed = 0f;
    private float currentSteering = 0f;

    private bool isBoosting = false;
    private bool isDrifting = false;

    private Rigidbody carRigidbody;

    void Start()
    {
        // Rigidbody ophalen
        carRigidbody = cube.GetComponent<Rigidbody>();

        if (carRigidbody == null)
        {
            Debug.LogError("Er zit geen Rigidbody op de Cube!");
            return;
        }

        // Betere collision detection
        carRigidbody.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        carRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        // Voorkomt dat de auto omvalt
        carRigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        HandleBoostInput();
        HandleDriftInput();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleSteering();
        HandleDrift();
    }

    // ==================================================
    // MOVEMENT
    // ==================================================

    void HandleMovement()
    {
        if (carRigidbody == null)
            return;

        float input = 0f;

        if (Input.GetKey(KeyCode.W))
            input = 1f;

        if (Input.GetKey(KeyCode.S))
            input = -1f;

        // Bepaal topsnelheid
        float targetMaxSpeed = isBoosting
            ? boostSpeed
            : maxSpeed;

        float targetSpeed = input * targetMaxSpeed;

        // Acceleratie / vertraging
        float rate;

        if (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed))
            rate = acceleration;
        else
            rate = deceleration;

        // Tijdens boost sneller accelereren
        if (isBoosting && input != 0f)
            rate = acceleration * 4f;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            rate * Time.fixedDeltaTime
        );

        // Voorwaartse richting
        Vector3 forward = forwardReference.forward;

        forward.y = 0f;
        forward.Normalize();

        // Auto bewegen
        Vector3 movement =
            forward *
            currentSpeed *
            Time.fixedDeltaTime;

        carRigidbody.MovePosition(
            carRigidbody.position + movement
        );
    }

    // ==================================================
    // STEERING
    // ==================================================

    void HandleSteering()
    {
        if (carRigidbody == null)
            return;

        float steeringInput = 0f;

        if (Input.GetKey(KeyCode.A))
            steeringInput = -1f;

        if (Input.GetKey(KeyCode.D))
            steeringInput = 1f;

        float steeringRate;

        if (steeringInput != 0f)
            steeringRate = steeringAcceleration;
        else
            steeringRate = steeringReturnSpeed;

        currentSteering = Mathf.MoveTowards(
            currentSteering,
            steeringInput,
            steeringRate * Time.fixedDeltaTime
        );

        // Niet sturen wanneer je stilstaat
        if (Mathf.Abs(currentSpeed) < 0.05f)
            return;

        float speedPercentage =
            Mathf.Clamp01(
                Mathf.Abs(currentSpeed) / maxSpeed
            );

        float steeringStrength =
            Mathf.Lerp(
                minimumSteeringStrength,
                1f,
                speedPercentage
            );

        float rotationSpeed = maxRotationSpeed;

        // Tijdens drift scherper sturen
        if (isDrifting)
            rotationSpeed *= driftTurnMultiplier;

        float rotationAmount =
            currentSteering *
            rotationSpeed *
            steeringStrength *
            Time.fixedDeltaTime;

        // Achteruit = steering omdraaien
        if (currentSpeed < 0f)
            rotationAmount *= -1f;

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                rotationAmount,
                0f
            );

        carRigidbody.MoveRotation(
            carRigidbody.rotation * rotation
        );
    }

    // ==================================================
    // BOOST
    // ==================================================

    void HandleBoostInput()
    {
        // SHIFT INHOUDEN = BOOST
        isBoosting =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
    }

    // ==================================================
    // DRIFT INPUT
    // ==================================================

    void HandleDriftInput()
    {
        // CTRL INHOUDEN = DRIFT
        isDrifting =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
    }

    // ==================================================
    // DRIFT
    // ==================================================

    void HandleDrift()
    {
        if (!isDrifting)
            return;

        if (carRigidbody == null)
            return;

        // Niet driften als je bijna stilstaat
        if (Mathf.Abs(currentSpeed) < 0.5f)
            return;

        // Je moet A of D gebruiken
        if (Mathf.Abs(currentSteering) < 0.1f)
            return;

        Vector3 right = forwardReference.right;

        right.y = 0f;
        right.Normalize();

        // Zijwaarts glijden
        Vector3 driftMovement =
            right *
            currentSteering *
            driftSideSpeed *
            Time.fixedDeltaTime;

        carRigidbody.MovePosition(
            carRigidbody.position +
            driftMovement
        );

        // Beetje snelheid verliezen tijdens drift
        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            0f,
            driftSpeedLoss * Time.fixedDeltaTime
        );
    }

    // ==================================================
    // COLLISION
    // ==================================================

    void OnCollisionEnter(Collision collision)
    {
        // Snelheid verliezen wanneer je ergens tegenaan rijdt
        currentSpeed *= 0.25f;
    }
}