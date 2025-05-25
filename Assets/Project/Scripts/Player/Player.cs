using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    private Camera mainCamera;

   public Vector3 GetPlayerViewportPosition()
   {
    if (mainCamera == null)
        return new Vector3(0.5f, 0.5f, 0f); // Default to center of screen

    // Vector3 viewport position for player ((0,0) viewport bottom left, (1,1) viewport top right)

    return mainCamera.WorldToViewportPoint(transform.position);
   }

}
