using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AntiTunneling : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Belangrijk voor snelle objecten
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }
}