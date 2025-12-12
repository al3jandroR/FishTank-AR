using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; 

public class TankStateController : MonoBehaviour
{
    [Header("AR Interaction")]
    public XRGrabInteractable grabInteractable;

    [Header("Simulation")]
    public TankFeeder feederScript;
    
    void Start()
    {
        if (feederScript != null)
        {
            feederScript.enabled = false;
        }
    }

    public void LockPlacement()
    {
        if (grabInteractable != null) 
        {
            grabInteractable.enabled = false;
        }

        if (feederScript != null) 
        {
            feederScript.enabled = true;
        }
        
        Debug.Log("Tank Locked. Feeding Mode Active.");
    }
}