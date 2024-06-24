using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    public float movementSpeed = 10.0f;
    public float lookSpeed = 60.0f; // Adjust look speed for arrow keys

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    void Update()
    {
        // yaw += lookSpeed * Input.GetAxis("Mouse X");
        // pitch -= lookSpeed * Input.GetAxis("Mouse Y");

        if (Input.GetKey(KeyCode.UpArrow))
        {
            pitch -= lookSpeed * Time.deltaTime * 60;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            pitch += lookSpeed * Time.deltaTime * 60;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            yaw -= lookSpeed * Time.deltaTime * 60;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            yaw += lookSpeed * Time.deltaTime * 60;
        }

        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            movement += transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement -= transform.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement -= transform.right;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement += transform.right;
        }
        transform.Translate(movement * movementSpeed * Time.deltaTime, Space.World);

        if (Input.GetKey(KeyCode.E))
        {
            transform.Translate(Vector3.up * movementSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Translate(Vector3.down * movementSpeed * Time.deltaTime, Space.World);
        }
    }
}
