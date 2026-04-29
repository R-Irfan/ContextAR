using UnityEngine;

public class GazePointObject : MonoBehaviour
{
    public Camera cameraEyes;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = cameraEyes.transform.position + cameraEyes.transform.forward * 2f;
    }
}
