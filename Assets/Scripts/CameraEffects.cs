using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    public Material fogMat;
    public Gradient fogGradient;


    private void Awake()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;

        //getting gradient values
        int numKeys = fogGradient.colorKeys.Length;
        Color[] colors = new Color[numKeys];
        float[] times = new float[numKeys];
        for(int i = 0; i < numKeys; i++)
        {
            colors[i] = fogGradient.colorKeys[i].color;
            colors[i].a = fogGradient.alphaKeys[i].alpha;
            times[i] = fogGradient.alphaKeys[i].time;
            Debug.Log(times[i]);
        }
        fogMat.SetInt("_NumKeys", numKeys);
        fogMat.SetColorArray("_FogColors", colors);
        fogMat.SetFloatArray("_FogTimes", times);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, fogMat);
    }

    private void Update()
    {
        //getting gradient values
        int numKeys = fogGradient.colorKeys.Length;
        Color[] colors = new Color[numKeys];
        float[] times = new float[numKeys];
        for (int i = 0; i < numKeys; i++)
        {
            colors[i] = fogGradient.colorKeys[i].color;
            colors[i].a = fogGradient.alphaKeys[i].alpha;
            times[i] = fogGradient.alphaKeys[i].time;
        }
        fogMat.SetInt("_NumKeys", numKeys);
        fogMat.SetColorArray("_FogColors", colors);
        fogMat.SetFloatArray("_FogTimes", times);

    }
}
