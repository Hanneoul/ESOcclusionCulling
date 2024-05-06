using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleOC : MonoBehaviour
{
    public GameObject OCManager = null;

    Toggle TOC = null;
    

    // Start is called before the first frame update
    void Start()
    {
        TOC = gameObject.GetComponent<Toggle>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void changeStateOC()
	{
        

		if(TOC.isOn)
		{
			OCManager.GetComponent<OcclusionCullingManager>().enabled = true;
		}			
		else
		{
			OCManager.GetComponent<OcclusionCullingManager>().enabled = false;
		}
	}
}



















