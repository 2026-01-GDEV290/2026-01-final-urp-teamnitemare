using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    public float sensitivity = 2.0f; // Чувствительность мыши
    public float moveSpeed = 5.0f; // Скорость перемещения
    public float fastMoveSpeed = 10.0f; // Ускоренная скорость перемещения
    public float smoothTime = 0.12f; // Время сглаживания

    private bool isRotating = false;
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Vector3 smoothMoveVelocity;

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Проверяем нажатие правой кнопки мыши
        {
            isRotating = true;
        }
        if (Input.GetMouseButtonUp(1)) // Проверяем отпускание правой кнопки мыши
        {
            isRotating = false;
        }

        if (isRotating)
        {
            // Вращение камеры по осям мыши
            float rotateHorizontal = Input.GetAxis("Mouse X") * sensitivity;
            float rotateVertical = Input.GetAxis("Mouse Y") * sensitivity;

            yaw += rotateHorizontal;
            pitch -= rotateVertical;

            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }

        // Перемещение камеры
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        float moveUpDown = Input.GetAxis("Jump");

        Vector3 moveDirection = (transform.forward * moveVertical) + (transform.right * moveHorizontal) + (transform.up * moveUpDown);

        float currentMoveSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed; // Проверка нажатия Shift
        Vector3 targetVelocity = moveDirection * currentMoveSpeed;

        // Сглаживание перемещения
        transform.position = Vector3.SmoothDamp(transform.position, transform.position + targetVelocity, ref smoothMoveVelocity, smoothTime);
    }
}
