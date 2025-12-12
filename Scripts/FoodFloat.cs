using UnityEngine;

public class FoodFloat : MonoBehaviour
{
    [SerializeField] private float waterDrag = 5f;
    [SerializeField] private float waterGravity = 0.3f;
    
    private Rigidbody rb;
    private bool inWater = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water") && !inWater)
        {
            inWater = true;
            rb.linearDamping = waterDrag;
            rb.mass = waterGravity;
        }
    }
}