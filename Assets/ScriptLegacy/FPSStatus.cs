using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSStatus : MonoBehaviour
{
    [Range(10, 150)]
    public int fontSize = 50;
    public Color color = new Color(.0f, .0f, .0f, 1.0f);
    public float width, height;

    public int frameCountDelay = 5;
    int count = 0;
                float total = 0;

    float printedFps = 0;
    void OnGUI()
    {
        Rect position = new Rect(width, height, Screen.width, Screen.height);

        float fps = 1.0f / Time.deltaTime;
        float ms = Time.deltaTime * 1000.0f;
        count++;
        
        string text = string.Format("{0:N1} FPS ({1:N1}ms)", printedFps, ms);

        if(count<frameCountDelay)
        {
            total += fps;     
        }
        else
        {
            printedFps = total/(float)(count-1);
            count = 0;
            total = 0;
            
        }
           

        GUIStyle style = new GUIStyle();

        style.fontSize = fontSize;
        style.normal.textColor = color;

        GUI.Label(position, text, style);
    }
}
