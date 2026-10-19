using UnityEngine;

public class RacingBot : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sleep hier het Collider GameObject in dat de Rigidbody heeft.")]
    public Rigidbody carRigidbody;

    [Tooltip("Sleep hier Forward Refrence in.")]
    public Transform forwardReference;

    [Tooltip("Sleep hier je RacePath object in.")]
    public Transform racePath;


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

    [Tooltip("Hoe agressief de AI naar een waypoint stuurt.")]
    public float steeringSensitivity = 1.5f;


    [Header("Path Settings")]
    [Tooltip("Hoe dichtbij de bot bij een waypoint moet komen.")]
    public float waypointReachDistance = 3f;

    [Tooltip("Hoeveel waypoints vooruit de AI kijkt. 1 = huidige waypoint.")]
    public int lookAheadPoints = 1;


    [Header("Corner Settings")]
    public bool slowDownForCorners = true;

    [Tooltip("Vanaf deze hoek begint de AI af te remmen.")]
    public float cornerSlowDownAngle = 35f;

    [Range(0.1f, 1f)]
    public float cornerSpeedMultiplier = 0.65f;


    [Header("Stuck Recovery")]
    public bool enableStuckRecovery = true;

    [Tooltip("Hoe lang de bot bijna stil mag staan.")]
    public float stuckTime = 2.5f;

    [Tooltip("Hoe weinig beweging telt als vastzitten.")]
    public float stuckDistance = 0.05f;


    // ==================================================
    // PRIVATE VARIABLES
    // ==================================================

    private Transform[] waypoints;

    private int currentWaypoint = 0;

    private float currentSpeed = 0f;
    private float currentSteering = 0f;

    private Vector3 lastPosition;
    private float stuckTimer = 0f;


    // ==================================================
    // START
    // ==================================================

    void Start()
    {
        // Check Rigidbody
        if (carRigidbody == null)
        {
            Debug.LogError(
                gameObject.name +
                ": Car Rigidbody is niet ingesteld!"
            );

            enabled = false;
            return;
        }

        // Check Forward Reference
        if (forwardReference == null)
        {
            Debug.LogError(
                gameObject.name +
                ": Forward Reference is niet ingesteld!"
            );

            enabled = false;
            return;
        }

        // Check Race Path
        if (racePath == null)
        {
            Debug.LogError(
                gameObject.name +
                ": Race Path is niet ingesteld!"
            );

            enabled = false;
            return;
        }

        // Waypoints ophalen
        int waypointCount = racePath.childCount;

        if (waypointCount == 0)
        {
            Debug.LogError(
                gameObject.name +
                ": RacePath heeft geen waypoints!"
            );

            enabled = false;
            return;
        }

        waypoints = new Transform[waypointCount];

        for (int i = 0; i < waypointCount; i++)
        {
            waypoints[i] = racePath.GetChild(i);
        }


        // ==================================================
        // RIGIDBODY SETTINGS
        // ==================================================

        carRigidbody.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        carRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        // Auto mag alleen over Y draaien
        carRigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;


        // Zoek waypoint dat het dichtst bij de auto staat
        FindClosestWaypoint();

        lastPosition = carRigidbody.position;
    }


    // ==================================================
    // FIXED UPDATE
    // ==================================================

    void FixedUpdate()
    {
        if (carRigidbody == null)
            return;

        if (waypoints == null || waypoints.Length == 0)
            return;

        // Eerst checken of we waypoint bereikt hebben
        UpdateWaypoint();

        // AI stuurinput berekenen
        float steeringInput = CalculateSteering();

        // Rijden
        HandleMovement();

        // Sturen
        HandleSteering(steeringInput);

        // Vastgelopen?
        HandleStuckDetection();
    }


    // ==================================================
    // WAYPOINT
    // ==================================================

    void UpdateWaypoint()
    {
        Vector3 botPosition = carRigidbody.position;

        Vector3 waypointPosition =
            waypoints[currentWaypoint].position;

        // Hoogte negeren
        botPosition.y = 0f;
        waypointPosition.y = 0f;

        float distance =
            Vector3.Distance(
                botPosition,
                waypointPosition
            );

        // Waypoint bereikt
        if (distance <= waypointReachDistance)
        {
            currentWaypoint++;

            // Laatste waypoint bereikt?
            if (currentWaypoint >= waypoints.Length)
            {
                // Terug naar waypoint 0
                currentWaypoint = 0;
            }
        }
    }


    // ==================================================
    // STEERING CALCULATION
    // ==================================================

    float CalculateSteering()
    {
        // Een waypoint vooruit kijken maakt sturen soepeler
        int targetIndex =
            currentWaypoint + lookAheadPoints - 1;

        targetIndex %= waypoints.Length;

        Transform targetWaypoint =
            waypoints[targetIndex];


        // Richting naar waypoint
        Vector3 directionToTarget =
            targetWaypoint.position -
            forwardReference.position;

        directionToTarget.y = 0f;


        if (directionToTarget.sqrMagnitude < 0.001f)
            return 0f;


        directionToTarget.Normalize();


        // Richting waarin auto kijkt
        Vector3 carForward =
            forwardReference.forward;

        carForward.y = 0f;

        if (carForward.sqrMagnitude < 0.001f)
            return 0f;

        carForward.Normalize();


        // Positief = rechts
        // Negatief = links
        float angle =
            Vector3.SignedAngle(
                carForward,
                directionToTarget,
                Vector3.up
            );


        // Hoek omzetten naar -1 / +1
        float steeringInput =
            angle / 45f;

        steeringInput *= steeringSensitivity;

        steeringInput =
            Mathf.Clamp(
                steeringInput,
                -1f,
                1f
            );


        return steeringInput;
    }


    // ==================================================
    // MOVEMENT
    // ==================================================

    void HandleMovement()
    {
        float targetMaxSpeed = maxSpeed;


        // ==============================================
        // AUTOMATISCH REMMEN VOOR BOCHTEN
        // ==============================================

        if (slowDownForCorners)
        {
            Vector3 directionToWaypoint =
                waypoints[currentWaypoint].position -
                forwardReference.position;

            directionToWaypoint.y = 0f;


            Vector3 currentForward =
                forwardReference.forward;

            currentForward.y = 0f;


            if (
                directionToWaypoint.sqrMagnitude > 0.001f &&
                currentForward.sqrMagnitude > 0.001f
            )
            {
                float cornerAngle =
                    Vector3.Angle(
                        currentForward.normalized,
                        directionToWaypoint.normalized
                    );


                if (cornerAngle >= cornerSlowDownAngle)
                {
                    targetMaxSpeed =
                        maxSpeed *
                        cornerSpeedMultiplier;
                }
            }
        }


        // ==============================================
        // ACCELERATION
        // ==============================================

        float targetSpeed = targetMaxSpeed;

        float accelerationRate;


        if (targetSpeed > currentSpeed)
        {
            accelerationRate = acceleration;
        }
        else
        {
            accelerationRate = deceleration;
        }


        currentSpeed =
            Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                accelerationRate *
                Time.fixedDeltaTime
            );


        // ==============================================
        // VOORUIT RIJDEN
        // ==============================================

        Vector3 moveForward =
            forwardReference.forward;

        moveForward.y = 0f;


        if (moveForward.sqrMagnitude < 0.001f)
            return;


        moveForward.Normalize();


        Vector3 movement =
            moveForward *
            currentSpeed *
            Time.fixedDeltaTime;


        carRigidbody.MovePosition(
            carRigidbody.position +
            movement
        );
    }


    // ==================================================
    // STEERING
    // ==================================================

    void HandleSteering(float steeringInput)
    {
        float steeringRate;


        // AI is aan het sturen
        if (Mathf.Abs(steeringInput) > 0.01f)
        {
            steeringRate =
                steeringAcceleration;
        }

        // AI gaat rechtdoor
        else
        {
            steeringRate =
                steeringReturnSpeed;
        }


        currentSteering =
            Mathf.MoveTowards(
                currentSteering,
                steeringInput,
                steeringRate *
                Time.fixedDeltaTime
            );


        // Niet sturen wanneer vrijwel stil
        if (Mathf.Abs(currentSpeed) < 0.05f)
            return;


        // ==============================================
        // STEERING STRENGTH
        // ==============================================

        float speedPercentage =
            Mathf.Clamp01(
                Mathf.Abs(currentSpeed) /
                Mathf.Max(maxSpeed, 0.01f)
            );


        float steeringStrength =
            Mathf.Lerp(
                minimumSteeringStrength,
                1f,
                speedPercentage
            );


        float rotationAmount =
            currentSteering *
            maxRotationSpeed *
            steeringStrength *
            Time.fixedDeltaTime;


        Quaternion rotation =
            Quaternion.Euler(
                0f,
                rotationAmount,
                0f
            );


        carRigidbody.MoveRotation(
            carRigidbody.rotation *
            rotation
        );
    }


    // ==================================================
    // FIND CLOSEST WAYPOINT
    // ==================================================

    void FindClosestWaypoint()
    {
        float closestDistance =
            Mathf.Infinity;

        int closestWaypointIndex = 0;


        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 botPosition =
                carRigidbody.position;

            Vector3 waypointPosition =
                waypoints[i].position;


            botPosition.y = 0f;
            waypointPosition.y = 0f;


            float distance =
                Vector3.Distance(
                    botPosition,
                    waypointPosition
                );


            if (distance < closestDistance)
            {
                closestDistance = distance;

                closestWaypointIndex = i;
            }
        }


        currentWaypoint =
            closestWaypointIndex;
    }


    // ==================================================
    // STUCK DETECTION
    // ==================================================

    void HandleStuckDetection()
    {
        if (!enableStuckRecovery)
            return;


        float distanceMoved =
            Vector3.Distance(
                carRigidbody.position,
                lastPosition
            );


        if (distanceMoved <= stuckDistance)
        {
            stuckTimer +=
                Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }


        if (stuckTimer >= stuckTime)
        {
            // Opnieuw logisch waypoint zoeken
            FindClosestWaypoint();

            // Snelheid resetten
            currentSpeed = 0f;

            stuckTimer = 0f;
        }


        lastPosition =
            carRigidbody.position;
    }


    // ==================================================
    // DEBUG PATH
    // ==================================================

    void OnDrawGizmosSelected()
    {
        if (racePath == null)
            return;

        if (racePath.childCount == 0)
            return;


        for (int i = 0; i < racePath.childCount; i++)
        {
            Transform point =
                racePath.GetChild(i);


            int nextIndex =
                (i + 1) %
                racePath.childCount;


            Transform nextPoint =
                racePath.GetChild(nextIndex);


            // Punt tekenen
            Gizmos.DrawSphere(
                point.position,
                0.3f
            );


            // Lijn naar volgende punt
            Gizmos.DrawLine(
                point.position,
                nextPoint.position
            );
        }
    }
}