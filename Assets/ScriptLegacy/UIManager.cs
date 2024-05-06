using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject LODManager;
    public Text ObjectNum;
    public Text  CulledObjectNum;

    public Button btn;

    private LODCullingManager aa = null;

    // Start is called before the first frame update
    void Start()
    {
        aa = LODManager.GetComponent<LODCullingManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(aa == null)
             aa = LODManager.GetComponent<LODCullingManager>();
        else{
            if(aa.isInitialized)
            {
                //btn.GetComponent<Image>().color = new Color(1,0,0);
                ObjectNum.text = "Object갯수 : " + aa.OCTargetObjects.Length.ToString();
                //Debug.Log("Object갯수 : " + aa.OCTargetObjects.Length.ToString());
                CulledObjectNum.text = "Culled Object갯수 : " + aa.CulledObjectNum.ToString(); 
            }
            else
            {                
                //btn.GetComponent<Image>().color = new Color(0.5f,0.5f,0.5f);
                ObjectNum.text = "Object갯수 : 0";
                ObjectNum.text = "Culled Object갯수 : 0";
            }

        }
    }
}
