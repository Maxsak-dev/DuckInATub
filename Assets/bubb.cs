using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bubb : MonoBehaviour
{
    // Number of axes to test – Unity supports up to 28 in old Input Manager
    public int maxAxes = 20;

    void Update()
    {
        for (int i = 1; i <= maxAxes; i++)
        {
            float value = Input.GetAxisRaw("Axis " + i);
            if (Mathf.Abs(value) > 0.1f) // only show active axes
            {
                Debug.Log($"Axis {i}: {value}");
            }
        }
    }
}
