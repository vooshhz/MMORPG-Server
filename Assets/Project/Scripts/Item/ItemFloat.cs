using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemFloat : MonoBehaviour
{
     // Floating animation parameters
    [SerializeField] private float floatAmplitude = 0.1f; // How high/low the item floats
    [SerializeField] private float floatFrequency = 3.0f; // How fast the item floats
    private Vector3 startPosition;
    private float floatTimer = 0f;
    private void Start() 
    {
         // Store the initial position
        startPosition = transform.position;
    }
    private void Update()
    {
        // Floating animation
        FloatItem();
    }

    private void FloatItem()
    {
        // Increment timer
        floatTimer += Time.deltaTime;
        
        // Calculate new Y position
        float newY = startPosition.y + (Mathf.Sin(floatTimer * floatFrequency) * floatAmplitude);
        
        // Apply new position
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // Add this to the ItemFloat script
    public void ResetStartPosition(Vector3 newPosition)
    {
        startPosition = newPosition;
        floatTimer = 0f; // Optional: reset the timer
    }
    }
