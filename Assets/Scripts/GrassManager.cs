using UnityEngine;
using UnityEngine.UI;

public class GrassManager : MonoBehaviour
{
    [SerializeField] private ComputeShader grassCompute;
    [SerializeField] private RawImage rawImage;
    
    void Start()
    {
        RenderTexture rt = new RenderTexture(256, 256, 0);
        rt.enableRandomWrite = true;
        rt.Create();
        int kernelIndex = grassCompute.FindKernel("CSMain");
        grassCompute.SetTexture(kernelIndex, "Result", rt);
        grassCompute.Dispatch(kernelIndex, 256/8, 256/8, 1);
        rawImage.texture = rt;
    }
}
