using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleLOD : MonoBehaviour
{
    public GameObject LODManager = null;

    Toggle LOD = null;
    

    // Start is called before the first frame update
    void Start()
    {
        LOD = gameObject.GetComponent<Toggle>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void changeStateOC()
	{
        if(LOD.isOn)
		{
			LODManager.GetComponent<LODController>().LODOn();
		}			
		else
		{
			LODManager.GetComponent<LODController>().LODOff();
		}
	}
}



















