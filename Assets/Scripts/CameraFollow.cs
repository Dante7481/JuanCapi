using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float CameraSpeed = 4f;
    public Vector3 desplazamiento;
    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 PosicionDeseada = target.position + desplazamiento;
            Vector3 PosicionSuavizada = Vector3.Lerp(transform.position, PosicionDeseada, CameraSpeed);
            transform.position = PosicionSuavizada;
        }
    }   
}
