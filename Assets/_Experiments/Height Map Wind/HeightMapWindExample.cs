using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class HeightMapWindExample : MonoBehaviour
{
    public Texture2D heightMap;

    public int cellCount = 10;
    public Vector3[] windVectors;
    public Vector3 windDirection;
    
    public float heightMapWorldSize = 10;
    public float windVecScalar = 0.1f;

    public float heightOffset = 3;

    public Texture2D slopeMap;
    public Texture2D flowMap;


    [ContextMenu("Process")]
    void ProcessWind()
    {
        windVectors = new Vector3[cellCount*cellCount];
        
        // Get the height map dimensions
        for (int x = 0; x < cellCount; x++)
        {
            for (int y = 0; y < cellCount; y++)
            {
                float xNorm = x/(float)(cellCount-1);
                float yNorm = y/(float)(cellCount-1);
                
                int pixelXIndex = (int)(heightMap.width * xNorm);
                int pixelYIndex = (int)(heightMap.height * yNorm);
                
                // Calculate the height at the current cell
                float height = heightMap.GetPixel(pixelXIndex, pixelYIndex).r;
                
                float left0Height = heightMap.GetPixel(pixelXIndex - 2, pixelYIndex).r;
                float left1Height = heightMap.GetPixel(pixelXIndex - 1, pixelYIndex).r;
                float right0Height = heightMap.GetPixel(pixelXIndex + 1, pixelYIndex).r;
                float right1Height = heightMap.GetPixel(pixelXIndex + 2, pixelYIndex).r;
                
                float top0Height =  heightMap.GetPixel(pixelXIndex, pixelYIndex + 2).r;
                float top1Height =  heightMap.GetPixel(pixelXIndex, pixelYIndex + 1).r;
                float bottom0Height =  heightMap.GetPixel(pixelXIndex, pixelYIndex - 2).r;
                float bottom1Height =  heightMap.GetPixel(pixelXIndex, pixelYIndex - 1).r;
               
                float xGrad = (right0Height+right1Height) - (left0Height+left1Height);
                float yGrad = (bottom0Height+bottom1Height) - (top0Height+top1Height);

                // Calculate the slope at the current cell
                Vector3 slope = new Vector3(xGrad, 0, yGrad);
                
                float slopeLength = slope.magnitude;
                slope.Normalize();
                
                Vector3 windVector = Vector3.Reflect(windDirection, slope);
              
                windVector.Normalize();
                
                // storing height in here for now
                windVector.y = height;

                windVectors[x + y * cellCount] = slope;// windVector;
            }
        }
    }

    [ContextMenu("Create Slope Map")]
    private void CreateSlopeMap()
    {
        if (slopeMap != null)
        {
            DestroyImmediate(slopeMap);
            DestroyImmediate(flowMap);
        }

        slopeMap = new Texture2D(heightMap.width, heightMap.height, TextureFormat.RGBAFloat, false);
        flowMap = new Texture2D(heightMap.width, heightMap.height, TextureFormat.RGBAFloat, false);

        for (int x = 0; x < slopeMap.width; x++)
        {
            for (int y = 0; y < slopeMap.height; y++)
            {
                float xNorm = x/(float)(slopeMap.width-1);
                float yNorm = y/(float)(slopeMap.height-1);
                
                int pixelXIndex = (int)(heightMap.width * xNorm);
                int pixelYIndex = (int)(heightMap.height * yNorm);
                
                float left0Height = heightMap.GetPixel(pixelXIndex - 2, pixelYIndex).r;
                float left1Height = heightMap.GetPixel(pixelXIndex - 1, pixelYIndex).r;
                float right0Height = heightMap.GetPixel(pixelXIndex + 1, pixelYIndex).r;
                float right1Height = heightMap.GetPixel(pixelXIndex + 2, pixelYIndex).r;
                
                float top0Height =  heightMap.GetPixel(pixelXIndex, pixelYIndex + 2).r;
                float top1Height =  heightMap.GetPixel(pixelXIndex, pixelYIndex + 1).r;
                float bottom0Height =  heightMap.GetPixel(pixelXIndex, pixelYIndex - 2).r;
                float bottom1Height =  heightMap.GetPixel(pixelXIndex, pixelYIndex - 1).r;
               
                float xGrad = (right0Height+right1Height) - (left0Height+left1Height);
                float yGrad = (bottom0Height+bottom1Height) - (top0Height+top1Height);
                Vector2 slopeGrad = new Vector2(xGrad, yGrad);
                
                float absSlope = slopeGrad.magnitude;
                
                slopeMap.SetPixel(pixelXIndex, pixelYIndex, new Color(xGrad, yGrad, absSlope, 1));
                

                Vector2 windDirectionVec2 = new Vector2(windDirection.x, windDirection.z);
                windDirectionVec2.Normalize();
                slopeGrad.Normalize();
                Vector2 reflectedVector = Vector2.Reflect(windDirectionVec2, slopeGrad);
                flowMap.SetPixel(pixelXIndex, pixelYIndex, new Color(reflectedVector.x, reflectedVector.y, 0, 1));

                //flowMap.SetPixel();
            }
        }
        
        slopeMap.Apply();
        flowMap.Apply();
    }

    public int xIndexSample = 512;
    public int yIndexSample = 512;
    
    [ContextMenu("Sample Slope Map")]
    void SampleSlopeMap()
    {
        int index = xIndexSample + yIndexSample * slopeMap.width;
        
        // Direct access to raw float data
        NativeArray<Vector4> pixelData = slopeMap.GetPixelData<Vector4>(0);
        Debug.Log(pixelData[index]);
    }


    private void OnDrawGizmos()
    {
        if(windVectors.Length == 0) return;
        
        float xStart = -heightMapWorldSize * .5f;
        float zStart = xStart;
        float spacing = heightMapWorldSize / cellCount;
        for (int x = 0; x < cellCount; x++)
        {
            for (int y = 0; y < cellCount; y++)
            {
                
                Vector3 windVector = windVectors[x + y * cellCount];
                float xPos = xStart + x * spacing;
                float zPos = zStart + y * spacing;
                float yPos = windVector.y * heightOffset + .03f;
                Gizmos.DrawSphere(new Vector3(xPos, yPos, zPos), .1f);
                Gizmos.DrawLine(new Vector3(xPos, yPos, zPos), new Vector3(xPos + windVector.x * windVecScalar, yPos, zPos + windVector.z * windVecScalar));
            }
        }
    }
}
