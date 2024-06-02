using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

//인스펙터창 UI에 버튼 및 숨김메뉴 추가
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

    // 쉐이더 파일과 실행할 Material
    public Shader OCShader;
    private Material _OCMaterial;

    // Start is called before the first frame update
    void Start()
    {
        //Shader 실행을 위한 머티리얼 생성
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

    //AABB 박스의 정점 배열 구하는 함수
    Vector4[] GetAABBVerticesFromOCTarget(GameObject parent, int index)
    {

        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();

        //부모의 바운딩 박스는 자식노드를 포함해야한다... 
        //(각개로 변경하기 원하시면 이 부분을 고치시면 됩니다)
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

        //박스가 좀더 크게
        bc.size = Vector3.Scale(bc.size, new Vector3(boundingBoxFactor, boundingBoxFactor, boundingBoxFactor));
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = parent.transform.position + bc.center;
        cube.transform.localScale = bc.size;
        Mesh mesh = cube.GetComponent<MeshFilter>().sharedMesh;
        Debug.Log(mesh.triangles.Length);

        List<Vector4> output = new List<Vector4>();

        //큐브 정점들 저장하기 위함
        Vector4[] vertices = new Vector4[mesh.triangles.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            //점 위치 , 인덱스
            Vector3 p = cube.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]);    //인덱스에서 vertex추출
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
        return output.ToArray();	//바운딩 박스의 Pos와 Index 반환
    }

    void OnRenderObject()
    {
        //최종 렌더링 단계에서 정점단위 입력
        //Graphics.ClearRandomWriteTargets();	
        _OCMaterial.SetPass(0);
        //Graphics.DrawProceduralNow(MeshTopology.Triangles, _Vertices.Count, 1);
        Graphics.DrawProceduralNow(MeshTopology.Points, _CuboidsCenter.Length, 1);

    }

}
