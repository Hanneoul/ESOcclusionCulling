using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

//�ν�����â UI�� ��ư �� ����޴� �߰�
[CustomEditor(typeof(AABBGenerator))]
public class AABBGenerator_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector(); // for other non-HideInInspector fields

        base.OnInspectorGUI();

        AABBGenerator script = (AABBGenerator)target;

        if (GUILayout.Button("Set AABB"))
        {
            script.SetChildObjectListButton();
        }
        if (GUILayout.Button("Reset All AABB"))
        {
            script.ResetChildObjectListButton();
        }



        // draw checkbox for the bool
        script.DebugMode = EditorGUILayout.Toggle("Draw AABB", script.DebugMode);


        //if (script.DebugMode) // if bool is true, show other fields
        //{
        //    EditorGUILayout.BeginHorizontal();


        //    //script.OCDebug_BoundingBox = EditorGUILayout.Toggle("Draw Bounding Box", script.OCDebug_BoundingBox);
        //    //script.CullingDelayFrame = EditorGUILayout.IntField("Frame Delay", script.CullingDelayFrame);
        //    //script.PixErrThreshold = EditorGUILayout.FloatField("Allow Pixel Size for LOD", script.PixErrThreshold);
        //    //script.toggleOC = EditorGUILayout.ObjectField("OC Toggle GUI", script.toggleOC, typeof(Toggle), true);

        //    EditorGUILayout.EndHorizontal();
        //}

        //base.OnInspectorGUI();		
    }
}
#endif


public class AABBGenerator : MonoBehaviour
{
    public bool DebugMode = true;

    // ���̴� ���ϰ� ������ Material
    public Shader OCShader;
    private Material _OCMaterial;

    // Start is called before the first frame update
    void Start()
    {
        //Shader ������ ���� ��Ƽ���� ����
        if (_OCMaterial == null)
            _OCMaterial = new Material(OCShader);
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public void SetChildObjectListButton()
    {
    }

    public void ResetChildObjectListButton()
    {

    }

    float boundingBoxFactor = 1.0f;

    //AABB �ڽ��� ���� �迭 ���ϴ� �Լ�
    Vector4[] GetAABBVerticesFromOCTarget(GameObject parent, int index)
    {

        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();

        //�θ��� �ٿ�� �ڽ��� �ڽĳ�带 �����ؾ��Ѵ�... 
        //(������ �����ϱ� ���Ͻø� �� �κ��� ��ġ�ø� �˴ϴ�)
        for (int i = 0; i < renderers.Length; i++)
        {
            if (hasBounds)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            else
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
        }

        BoxCollider bc = parent.AddComponent<BoxCollider>();
        if (hasBounds)
        {
            bc.center = bounds.center - parent.transform.position;
            bc.size = bounds.size;
        }
        else
        {
            bc.size = bc.center = Vector3.zero;
            bc.size = Vector3.zero;
        }

        //�ڽ��� ���� ũ��
        bc.size = Vector3.Scale(bc.size, new Vector3(boundingBoxFactor, boundingBoxFactor, boundingBoxFactor));
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = parent.transform.position + bc.center;
        cube.transform.localScale = bc.size;
        Mesh mesh = cube.GetComponent<MeshFilter>().sharedMesh;
        Debug.Log(mesh.triangles.Length);

        List<Vector4> output = new List<Vector4>();

        //ť�� ������ �����ϱ� ����
        Vector4[] vertices = new Vector4[mesh.triangles.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            //�� ��ġ , �ε���
            Vector3 p = cube.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]);    //�ε������� vertex����
            vertices[i] = new Vector4(p.x, p.y, p.z, index);
        }

        for (int i = 0; i < (vertices.Length / 6.0); i++)
        {
            output.Add(vertices[6 * i + 1]);
            output.Add(vertices[6 * i + 2]);
            output.Add(vertices[6 * i]);
            output.Add(vertices[6 * i + 5]);
        }

        Destroy(bc);
        Destroy(cube);
        return output.ToArray();	//�ٿ�� �ڽ��� Pos�� Index ��ȯ
    }

    void OnRenderObject()
    {
        //���� ������ �ܰ迡�� �������� �Է�
        //Graphics.ClearRandomWriteTargets();	
        _OCMaterial.SetPass(0);
        //Graphics.DrawProceduralNow(MeshTopology.Triangles, _Vertices.Count, 1);
        Graphics.DrawProceduralNow(MeshTopology.Points, _CuboidsCenter.Length, 1);

    }

}
