using System;
using UnityEngine;

public class HeightMapWindExample : MonoBehaviour
{
    public Texture2D heightMap;

    public int cellCount = 10;
    public Vector3[] windVectors;
    public Vector3 windDirection;
    
    public float heightMapWorldSize = 10;
    public float windVecScalar = 0.1f;

    public float heightOffset = 3;

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
                print(height);
                float xGrad = heightMap.GetPixel(pixelXIndex + 1, pixelYIndex).r - heightMap.GetPixel(pixelXIndex - 1, pixelYIndex).r;
                float yGrad = heightMap.GetPixel(pixelXIndex, pixelYIndex + 1).r - heightMap.GetPixel(pixelXIndex, pixelYIndex - 1).r;

                // Calculate the slope at the current cell
                Vector3 slope = new Vector3(xGrad, 0, yGrad);
                float slopeLength = slope.magnitude;
                slope.Normalize();
                
                Vector3 windVector = Vector3.Reflect(windDirection, slope);
              
                windVector.Normalize();
                
                // storing height in here for now
                windVector.y = height;
                
                windVectors[x + y * cellCount] = windVector;
            }
        }
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
                Gizmos.DrawLine(new Vector3(xPos, yPos, zPos), new Vector3(xPos + windVector.x * windVecScalar, yPos, zPos + windVector.z * windVecScalar));
            }
        }
    }
}
