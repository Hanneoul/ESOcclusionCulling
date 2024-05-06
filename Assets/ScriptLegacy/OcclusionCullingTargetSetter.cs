using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif




#if UNITY_EDITOR
[CustomEditor(typeof(OcclusionCullingTargetSetter))]
public class OcclusionCullingTargetSetterButton : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        OcclusionCullingTargetSetter generator = (OcclusionCullingTargetSetter)target;
        if (GUILayout.Button("Set Objects"))
        {
            generator.RunButton();
        }
    }
}
#endif


public class OcclusionCullingTargetSetter : MonoBehaviour
{
    public OcclusionCullingManager manager = null;
    public void RunButton()
	{
		List<GameObject> trees = new List<GameObject>();
		Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach(Transform child in allChildren)
        {
            // if(child.GetComponent<MeshFilter>() == null)            
            //     continue;
            // Mesh mesh = child.GetComponent<MeshFilter>().mesh;
            // if(mesh != null)
          
    
            if(child.GetComponent<Renderer>() == null)            
                continue;
            
            trees.Add(child.gameObject);
        }
		
		manager.OCTargetObjects = trees.ToArray();
	}
}
