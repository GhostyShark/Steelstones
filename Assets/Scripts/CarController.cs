using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("References")]
    public Transform carBody;   // De zichtbare auto-body (los van de fysica-bol)
    public Rigidbody rb;        // Rigidbody van de bol (dit object)

    [Header("Movement Settings")]
    public float moveForce = 15f;
    public float maxSpeed = 5f;

    [Header("Steering Settings")]
    public float turnSpeed = 120f; // graden per seconde

    [Header("Body Follow Settings")]
    public Vector3 offset = Vector3.zero; // Eventuele verschuiving t.o.v. het midden van de bol

    void Awake()
    {
        // Dit script hoort op de bol te staan; pak de eigen Rigidbody als er niks is ingevuld.
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        HandleSteering();
        HandleMovement();
    }

    void HandleSteering()
    {
        // A = links, D = rechts — draait de carBody zelf, niet de bol.
        float turn = 0f;

        if (Input.GetKey(KeyCode.A)) turn -= 1f;
        if (Input.GetKey(KeyCode.D)) turn += 1f;

        carBody.Rotate(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f, Space.World);
    }

    void HandleMovement()
    {
        // W = vooruit, S = achteruit — relatief aan de richting waar de carBody heen kijkt.
        float input = 0f;

        if (Input.GetKey(KeyCode.W)) input += 1f;
        if (Input.GetKey(KeyCode.S)) input -= 1f;

        Vector3 forward = carBody.forward;
        forward.y = 0f;
        forward.Normalize();

        rb.AddForce(forward * input * moveForce, ForceMode.Acceleration);

        // Maximumsnelheid (horizontaal)
        Vector3 flatVelocity = rb.linearVelocity;
        flatVelocity.y = 0f;

        if (flatVelocity.magnitude > maxSpeed)
        {
            flatVelocity = flatVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(flatVelocity.x, rb.linearVelocity.y, flatVelocity.z);
        }
    }

    void LateUpdate()
    {
        // De body volgt de positie van de bol, los van de rotatie van de bol zelf.
        if (carBody != null)
            carBody.position = rb.position + offset;
    }
}