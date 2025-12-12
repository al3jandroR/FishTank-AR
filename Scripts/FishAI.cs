using UnityEngine;

public class FishAI : MonoBehaviour
{
    [SerializeField] private Collider boundary;
    [SerializeField] private float speed = 0.05f;
    [SerializeField] private float rotSpeed = 3f;
    [SerializeField] private float foodRange = 0.2f;
    
    private Vector3 target;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (boundary == null)
            boundary = transform.parent?.GetComponent<Collider>();
        
        SetTarget();
    }

    void FixedUpdate()
    {
        CheckFood();
        
        if (Vector3.Distance(transform.position, target) < 0.02f)
            SetTarget();

        Vector3 dir = (target - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.fixedDeltaTime));
        rb.MovePosition(transform.position + transform.forward * speed * Time.fixedDeltaTime);
    }

    void CheckFood()
    {
        GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");
        float closest = foodRange;
        Transform nearestFood = null;

        foreach (GameObject f in foods)
        {
            float d = Vector3.Distance(transform.position, f.transform.position);
            if (d < closest)
            {
                closest = d;
                nearestFood = f.transform;
            }
        }

        if (nearestFood != null) 
            target = nearestFood.position;
    }

    void SetTarget()
    {
        if (boundary == null) return;
        
        Bounds b = boundary.bounds;
        Vector3 center = b.center;
        Vector3 size = b.size * 0.7f;
        
        target = center + new Vector3(
            Random.Range(-size.x/2, size.x/2),
            Random.Range(-size.y/2, size.y/2),
            Random.Range(-size.z/2, size.z/2)
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            Destroy(other.gameObject);
            SetTarget();
        }
    }
}