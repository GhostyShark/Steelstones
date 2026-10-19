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
    public float boostDuration = 2f;
    public float boostCooldown = 3f;

    [Header("Drift Settings")]
    public float driftTurnMultiplier = 1.7f;
    public float driftSideSpeed = 2f;
    public float driftSpeedLoss = 0.5f;

    private float currentSpeed = 0f;
    private float currentSteering = 0f;

    private bool isBoosting = false;
    private float boostTimer = 0f;
    private float boostCooldownTimer = 0f;

    private bool isDrifting = false;

    private Rigidbody carRigidbody;

    void Start()
    {
        // Rigidbody ophalen van de Cube
        carRigidbody = cube.GetComponent<Rigidbody>();

        if (carRigidbody == null)
        {
            Debug.LogError("Er zit geen Rigidbody op de Cube!");
            return;
        }

        // Betere physics
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
        HandleBoost();
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

        // ==============================================
        // BOOST
        // ==============================================

        if (isBoosting)
        {
            // Tijdens boost blijft de snelheid hoog
            if (currentSpeed >= 0f)
                currentSpeed = boostSpeed;
            else
                currentSpeed = -boostSpeed;
        }
        else
        {
            // Normale snelheid
            float targetSpeed = input * maxSpeed;

            float rate;

            if (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed))
                rate = acceleration;
            else
                rate = deceleration;

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                rate * Time.fixedDeltaTime
            );
        }

        // Forward richting
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

        // Steering smooth maken
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

        // Niet draaien wanneer auto stilstaat
        if (Mathf.Abs(currentSpeed) < 0.05f)
            return;

        // Hoe sneller je rijdt, hoe sterker steering wordt
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

        // Extra steering tijdens drift
        if (isDrifting)
        {
            rotationSpeed *= driftTurnMultiplier;
        }

        float rotationAmount =
            currentSteering *
            rotationSpeed *
            steeringStrength *
            Time.fixedDeltaTime;

        // Steering omdraaien wanneer je achteruit rijdt
        if (currentSpeed < 0f)
        {
            rotationAmount *= -1f;
        }

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

    void HandleBoost()
    {
        // Cooldown aftellen
        if (boostCooldownTimer > 0f)
        {
            boostCooldownTimer -= Time.deltaTime;
        }

        // SHIFT = BOOST
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (!isBoosting &&
                boostCooldownTimer <= 0f &&
                Mathf.Abs(currentSpeed) > 0.1f)
            {
                isBoosting = true;

                boostTimer = boostDuration;

                // DIRECT naar boost speed
                if (currentSpeed > 0f)
                    currentSpeed = boostSpeed;
                else
                    currentSpeed = -boostSpeed;

                Debug.Log("BOOST START!");
            }
        }

        // Boost timer
        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;

            if (boostTimer <= 0f)
            {
                isBoosting = false;

                boostCooldownTimer =
                    boostCooldown;

                Debug.Log("BOOST END!");
            }
        }
    }

    // ==================================================
    // DRIFT INPUT
    // ==================================================

    void HandleDriftInput()
    {
        // CTRL ingedrukt houden
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

        // Niet driften wanneer je bijna stilstaat
        if (Mathf.Abs(currentSpeed) < 0.5f)
            return;

        // A/D nodig om daadwerkelijk te driften
        if (Mathf.Abs(currentSteering) < 0.1f)
            return;

        Vector3 right = forwardReference.right;

        right.y = 0f;

        right.Normalize();

        // Zijwaartse drift
        Vector3 driftMovement =
            right *
            currentSteering *
            driftSideSpeed *
            Time.fixedDeltaTime;

        carRigidbody.MovePosition(
            carRigidbody.position +
            driftMovement
        );

        // Langzaam snelheid verliezen tijdens drift
        if (!isBoosting)
        {
            currentSpeed =
                Mathf.MoveTowards(
                    currentSpeed,
                    0f,
                    driftSpeedLoss *
                    Time.fixedDeltaTime
                );
        }
    }

    // ==================================================
    // COLLISION
    // ==================================================

    void OnCollisionEnter(Collision collision)
    {
        // Flink snelheid verliezen bij crash
        currentSpeed *= 0.25f;

        // Boost stoppen bij harde crash
        if (isBoosting)
        {
            isBoosting = false;
            boostCooldownTimer = boostCooldown;
        }
    }
}