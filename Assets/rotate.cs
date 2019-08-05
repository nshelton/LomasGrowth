using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotate : MonoBehaviour
{
    public float speedY = 0.1f;
    public Vector3 axis = Vector3.up;

    void Update()
    {
        transform.Rotate(axis, speedY * Time.deltaTime);
    }
}
