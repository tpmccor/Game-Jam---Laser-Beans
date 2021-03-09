using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlatformTrigger : MonoBehaviour
{
    public MovePlatform m_platformController;

    private void OnTriggerEnter(Collider other)
    {
        //Keep a reference too every player on the platform
        if (other.gameObject.TryGetComponent<CharacterController>(out CharacterController player))
        {
            m_platformController.AddToPlatform(player);
        }
        else
        {
            //Parent non-player objects to the platform
            //other.transform.parent = transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Remove player from onPlatform list when the jump off
        if (other.gameObject.TryGetComponent<CharacterController>(out CharacterController player))
        {
            m_platformController.RemoveFromPlatform(player);
        }
        else
        {
            //Remove parent when objects are no longer on platform
            //other.transform.parent = null;
        }
    }
}
