using UnityEngine;

public class RotateConstantly : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 100f; // degrees per second
    [SerializeField] Vector3 rotationAxis = Vector3.forward; // default for UI/Image

    void Update()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}