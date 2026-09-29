using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("References")]
    public Transform cube;
    public Transform forwardReference;
    public Rigidbody rb;

    [Header("Movement Settings")]
    public float maxSpeed = 4f;
    public float acceleration = 2f;
    public float deceleration = 1.5f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 120f;

    [Header("Drift / Handbrake Settings")]
    [Tooltip("Hoe sterk zijwaartse snelheid gecorrigeerd wordt (grip) tijdens normaal rijden.")]
    public float normalGrip = 10f;
    [Tooltip("Grip tijdens het handremmen — lager = meer slippen/driften.")]
    public float driftGrip = 1.5f;
    [Tooltip("Extra draaisnelheid tijdens het driften, voor een dynamischer effect.")]
    public float driftRotationMultiplier = 1.5f;
    [Tooltip("Extra afremming terwijl de handrem ingedrukt is.")]
    public float handbrakeDeceleration = 4f;

    private float currentSpeed = 0f;
    private bool isHandbraking = false;

    void Start()
    {
        // Als Rigidbody niet handmatig is ingevuld,
        // pak de Rigidbody van de cube.
        if (rb == null)
            rb = cube.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Handrem status ophalen. GetKey (i.t.t. GetKeyDown) is veilig
        // om in FixedUpdate te lezen, dus we missen geen input.
        isHandbraking = Input.GetKey(KeyCode.Space);

        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        Vector3 forward = forwardReference.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = forwardReference.right;
        right.y = 0f;
        right.Normalize();

        float input = 0f;

        // W = vooruit
        if (Input.GetKey(KeyCode.W))
            input += 1f;

        // S = achteruit
        if (Input.GetKey(KeyCode.S))
            input -= 1f;

        float targetSpeed = input * maxSpeed;

        // Tijdens de handrem remt de auto harder af dan normaal (echte handremwerking)
        float rate = Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)
            ? acceleration
            : (isHandbraking ? handbrakeDeceleration : deceleration);

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            rate * Time.fixedDeltaTime
        );

        // Motorkracht: duwt de auto naar voren/achteren via AddForce
        Vector3 engineForce = forward * currentSpeed;
        rb.AddForce(engineForce, ForceMode.Acceleration);

        // Grip: corrigeert de zijwaartse snelheid, zodat de auto normaal
        // niet zomaar zijwaarts wegglijdt. Tijdens de handrem wordt deze
        // grip veel lager, waardoor de auto kan slippen/driften.
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        float lateralSpeed = Vector3.Dot(horizontalVelocity, right);
        Vector3 lateralVelocity = right * lateralSpeed;

        float grip = isHandbraking ? driftGrip : normalGrip;
        rb.AddForce(-lateralVelocity * grip, ForceMode.Acceleration);

        // Voorkom dat de auto sneller gaat dan maxSpeed
        horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;

            rb.linearVelocity = new Vector3(
                horizontalVelocity.x,
                rb.linearVelocity.y,
                horizontalVelocity.z
            );
        }
    }

    void HandleRotation()
    {
        // Niet draaien als je stilstaat, tenzij je de handrem gebruikt
        // (zo kun je ook vanuit stilstand een drift starten).
        if (Mathf.Abs(currentSpeed) < 0.05f && !isHandbraking)
            return;

        float rotation = 0f;

        // A = links
        if (Input.GetKey(KeyCode.A))
            rotation -= 1f;

        // D = rechts
        if (Input.GetKey(KeyCode.D))
            rotation += 1f;

        // Tijdens het driften draait de auto iets sneller
        float appliedRotationSpeed = isHandbraking
            ? rotationSpeed * driftRotationMultiplier
            : rotationSpeed;

        Quaternion deltaRotation = Quaternion.Euler(
            0f,
            rotation * appliedRotationSpeed * Time.fixedDeltaTime,
            0f
        );

        rb.MoveRotation(rb.rotation * deltaRotation);
    }
}