using UnityEngine;
using UnityEngine.InputSystem; 

public class TankFeeder : MonoBehaviour
{
    public GameObject foodPrefab;
    public Transform dropPoint;

    void Update()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            HandleTap(touchPosition);
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 touchPosition = Mouse.current.position.ReadValue();
            HandleTap(touchPosition);
        }
    }

    void HandleTap(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.transform == this.transform || hit.transform.IsChildOf(this.transform))
            {
                SpawnFood();
            }
        }
    }

    void SpawnFood()
    {
        Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position + (transform.up * 0.2f * transform.localScale.y);
        GameObject food = Instantiate(foodPrefab, spawnPos, Quaternion.identity);
    }
}