using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


#if UNITY_EDITOR
[CustomEditor(typeof(LODController))]
public class LODControllerButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LODController generator = (LODController)target;
        if (GUILayout.Button("Generate Cubes"))
        {
            generator.Run();
        }
        if (GUILayout.Button("Delete LOD"))
        {
            generator.Release();
        }
    }
}
#endif


public class LODController : MonoBehaviour
{

    //SphereCollider sc = this.gameObject.AddComponent(typeof(SphereCollider)) as SphereCollider;

    //public states
    public float allowPixelError = 1;
    public Camera MainCamera = null;
    public float screenWidth=1920, screenHeight=1080;

   


    // Start is called before the first frame update
    void Start()
    {
        
        
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Release()
    {
         Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach(Transform child in allChildren)
        {
            LODGroup lodGroup = null;
            if(child.gameObject.GetComponent<LODGroup>()==null)
                continue;
            
            lodGroup = child.gameObject.GetComponent<LODGroup>(); 
            DestroyImmediate(lodGroup);
        }
    }

    public void LODOff()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach(Transform child in allChildren)
        {
            LODGroup lodGroup = null;
            if(child.gameObject.GetComponent<LODGroup>()==null)
                continue;
            
            lodGroup = child.gameObject.GetComponent<LODGroup>(); 
            lodGroup.enabled = false;
        }




    }

    public void LODOn()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach(Transform child in allChildren)
        {
            LODGroup lodGroup = null;
            if(child.gameObject.GetComponent<LODGroup>()==null)
                continue;
            
            lodGroup = child.gameObject.GetComponent<LODGroup>(); 
            lodGroup.enabled = true;
        }
    }

    public void Run()
    {
       
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach(Transform child in allChildren)
        {
            // if(child.GetComponent<MeshFilter>() == null)            
            //     continue;
            // Mesh mesh = child.GetComponent<MeshFilter>().mesh;
            // if(mesh != null)
           
            if(MainCamera == null)
                break;
            if(child.GetComponent<Renderer>() == null)            
                continue;
            Renderer rend = child.GetComponent<Renderer>();
            
            //허용 오차범위까지의 거리 구하기
            float distance = Vector3.Magnitude(rend.bounds.size) * screenWidth / (2 * allowPixelError * Mathf.Tan(MainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad));

            //디버깅용
            Debug.Log(child.name);
            Debug.Log("Size : " + Vector3.Magnitude(rend.bounds.size));
            Debug.Log("Distance : " + distance);
            Debug.Log(distance / (float)MainCamera.farClipPlane);
            
            if(distance > 0.3 && distance < 1000)
            {
                LODGroup lodGroup = null;

                if(child.gameObject.GetComponent<LODGroup>()==null)
                {
                    lodGroup = child.gameObject.AddComponent<LODGroup>(); 
                }   
                else
                {
                    lodGroup = child.gameObject.GetComponent<LODGroup>();
                }                    

                LOD[] lods = new LOD[2];

                Renderer[] renderers0 = new Renderer[1];
                renderers0[0] = rend;
                lods[0] = new LOD(distance / (float)MainCamera.farClipPlane, renderers0);

                Renderer[] renderers1 = new Renderer[1];
                renderers1[0] = rend;
                lods[0] = new LOD(1.0f- (distance / (float)MainCamera.farClipPlane), renderers1);
                
                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();                    
            }
            else
            {
                LODGroup lodGroup = null;

                if(child.gameObject.GetComponent<LODGroup>()!=null)
                {                        
                    lodGroup = child.gameObject.GetComponent<LODGroup>();
                    DestroyImmediate(lodGroup);
                }       
            }


            
             
        }
    }


    // void OnGUI()
    // {
    //     if (GUILayout.Button("Enable / Disable"))
    //         lodGroup.enabled = !lodGroup.enabled;

    //     if (GUILayout.Button("Default"))
    //         lodGroup.ForceLOD(-1);

    //     if (GUILayout.Button("Force 0"))
    //         lodGroup.ForceLOD(0);

    //     if (GUILayout.Button("Force 1"))
    //         lodGroup.ForceLOD(1);

    //     if (GUILayout.Button("Force 2"))
    //         lodGroup.ForceLOD(2);

    //     if (GUILayout.Button("Force 3"))
    //         lodGroup.ForceLOD(3);

    //     if (GUILayout.Button("Force 4"))
    //         lodGroup.ForceLOD(4);

    //     if (GUILayout.Button("Force 5"))
    //         lodGroup.ForceLOD(5);

    //     if (GUILayout.Button("Force 6"))
    //         lodGroup.ForceLOD(6);
    // }


}



