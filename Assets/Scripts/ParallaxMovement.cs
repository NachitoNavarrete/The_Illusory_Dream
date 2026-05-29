using UnityEngine;

// ParallaxMovement: mueve las texturas de fondo para dar sensación de profundidad.
// - Este script calcula cuánto moverse según la cámara y desplaza las texturas.
public class ParallaxMovement : MonoBehaviour
{
    Transform cam;
    Vector3 camStartPos;
    float originalZ;
    float distance;

    GameObject[] backgrounds;
    Material[] mat;
    float[] backSpeed;
    float farthestBack;

    [Range(0.01f, 0.5f)]
    public float parallax = 0.1f;

    void Start()
    {
        cam = Camera.main.transform;
        camStartPos = cam.position;
        originalZ = transform.position.z;

        int backCount = transform.childCount;
        mat = new Material[backCount];
        backSpeed = new float[backCount];
        backgrounds = new GameObject[backCount];

        for (int i = 0; i < backCount; i++)
        {
            backgrounds[i] = transform.GetChild(i).gameObject;
            mat[i] = backgrounds[i].GetComponent<Renderer>().material;
        }
        BackSpeedCalculate(backCount);
    }

    void BackSpeedCalculate(int backCount)
    {
        for (int i = 0; i < backCount; i++)
        {
            float zDist = backgrounds[i].transform.position.z - cam.position.z;
            if (zDist > farthestBack)
                farthestBack = zDist;
        }

        for (int i = 0; i < backCount; i++)
        {
            float zDist = backgrounds[i].transform.position.z - cam.position.z;
            if (farthestBack != 0f)
                backSpeed[i] = zDist / farthestBack;
            else
                backSpeed[i] = 1f;
        }
    }

    void LateUpdate()
    {
        distance = cam.position.x - camStartPos.x;

        // El contenedor sigue exactamente la X de la cámara (sin -1)
        transform.position = new Vector3(cam.position.x, transform.position.y, originalZ);

        for (int i = 0; i < backgrounds.Length; i++)
        {
            float speed = backSpeed[i] * parallax;
            mat[i].SetTextureOffset("_MainTex", new Vector2(distance, 0) * speed);
        }
    }
}