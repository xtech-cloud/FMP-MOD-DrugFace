using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FaceAnalyzer;

public class Test_Plugin : MonoBehaviour
{
    public RawImage rawImage;
    private CameraFaceAnalyzer faceAnalyzer;
    


    // Start is called before the first frame update
    void Start()
    {
        faceAnalyzer = new CameraFaceAnalyzer();
        faceAnalyzer.mono = this;
        faceAnalyzer.targetImage = rawImage;
        faceAnalyzer.Run();
    }

    // Update is called once per frame
    void Update()
    {
        bool detected = faceAnalyzer.DetectFace();
        Debug.Log(detected);
    }
}
