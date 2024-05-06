using System.Collections;
using System.Collections.Generic;
//evil :: Tolist 때문에
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

 #if UNITY_EDITOR
 using UnityEditor;

//인스펙터창 UI에 버튼 및 숨김메뉴 추가
 [CustomEditor(typeof(LODCullingManager))]
 public class LODCullingManager_Editor : Editor
 {
     public override void OnInspectorGUI()
     {
		//DrawDefaultInspector(); // for other non-HideInInspector fields
		
		base.OnInspectorGUI();

		LODCullingManager script = (LODCullingManager)target;

		if (GUILayout.Button("Set OC Objects"))
        {
            script.SetChildObjectListButton();
        }
		if (GUILayout.Button("Reset All OC Objects"))
        {
            script.ResetChildObjectListButton();
        }
		
		

		// draw checkbox for the bool
		script.DebugMode = EditorGUILayout.Toggle("Occlusion Culling Debug", script.DebugMode);
		if (script.DebugMode) // if bool is true, show other fields
		{
			EditorGUILayout.BeginHorizontal();


			script.OCDebug_BoundingBox = EditorGUILayout.Toggle("Draw Bounding Box", script.OCDebug_BoundingBox);
			script.CullingDelayFrame = EditorGUILayout.IntField("Frame Delay", script.CullingDelayFrame);
			//script.PixErrThreshold = EditorGUILayout.FloatField("Allow Pixel Size for LOD", script.PixErrThreshold);
			//script.toggleOC = EditorGUILayout.ObjectField("OC Toggle GUI", script.toggleOC, typeof(Toggle), true);

			EditorGUILayout.EndHorizontal();
		}

		//base.OnInspectorGUI();		
     }
 }
 #endif

public class LODCullingManager : MonoBehaviour
{

	//디버깅용 바운딩박스 렌더링 on/off 플래그
	[HideInInspector]
    public bool DebugMode = true;
	[HideInInspector]
    public bool OCDebug_BoundingBox = false;
	[HideInInspector]
    public int CullingDelayFrame = 1;


	//디버깅 UI용 수치
	[HideInInspector]
    public int CulledObjectNum = 0;
	
   

    

    
    // 쉐이더 파일과 실행할 Material
    public Shader OCShader;
	private Material _OCMaterial;

	
    //쉐이더에서 출력할 UAV 버퍼
    private ComputeBuffer _BufferWriter;

	//쉐이더 읽기전용 버퍼
    private ComputeBuffer _AABBVertexPositionIndexReader;
    private ComputeBuffer _AABBCuboidCenterIndexReader;
    private ComputeBuffer _AABBCuboidScaleIndexReader;
    private ComputeBuffer _ViewFrustum;



    //바운딩 박스 비율 : 1.1 이면 원본 물체보다 1.1배 크게 잡음
    public float boundingBoxFactor = 1.01f;

    //AABB바운딩 박스
    private Vector4[] _CuboidsCenter;	//쉐이더 입력버퍼로 사용됨
    private Vector4[] _CuboidsScale;

	public float PixErrThreshold = 10; //LOD 팩터 
    private List<Vector4> _Vertices;


    //Shader 결과 저장
    private float[] _OcclusionResult;
    private float[] _OcclusionResultCache;

	//OC수행할 ObjectList와 해당 Object들의 MeshRenderer list
    public GameObject[] OCTargetObjects;
    private List<List<Renderer>> _OCTargetMeshRenderers;

	[HideInInspector]
    public bool isInitialized = false;

    

    
	

    void Initialize()
    {
		
        //Shader 실행을 위한 머티리얼 생성
        if (_OCMaterial == null) 
			_OCMaterial = new Material(OCShader);
        
        //타깃 메시 렌더러 리스트의 초기화
		_OCTargetMeshRenderers = new List<List<Renderer>>();

        //Target (UAV) Object 갯수만큼 float
        _BufferWriter = new ComputeBuffer(OCTargetObjects.Length, 4, ComputeBufferType.Default);
        // struct _Frustum
        // {
        // 	public Vector4 Left;
        //     public Vector4 Right;
        //     public Vector4 Down;
        //     public Vector4 Up;		
        //     public Vector4 Near;
        //     public Vector4 Far;
        // };
        // 순으로 저장됨
        
		//그릴지 말지 결정된 결과를 저장하는 버퍼
        _OcclusionResult = new float[OCTargetObjects.Length];		  //현재 프레임
		_OcclusionResultCache = new float[OCTargetObjects.Length];  //이전 프레임
        
        //Shader Output (UAV) 초기화~
		Graphics.ClearRandomWriteTargets();	
		Graphics.SetRandomWriteTarget(1, _BufferWriter, true);	

        //바운딩박스 / 카메라 충돌처리
		_CuboidsCenter = new Vector4[OCTargetObjects.Length];
        _CuboidsScale = new Vector4[OCTargetObjects.Length];
        

        //aabb
		_Vertices = new List<Vector4>();

        for (int i=0; i<OCTargetObjects.Length; i++)
		{
			_OCTargetMeshRenderers.Add(OCTargetObjects[i].GetComponentsInChildren<Renderer>().ToList());
			Vector4[] aabb = GetAABBVerticesFromOCTarget(OCTargetObjects[i], i);	// pos, index 저장됨
			
			_CuboidsCenter[i] = GetCenterFromCubeVertices(aabb);
			_CuboidsScale[i] = GetScaleFromCubeVertices(aabb);
			
			//AABB관련 데이터 정리됨
			_Vertices.AddRange(aabb); //aabb 전체 추가 (한번에 최적화 : 10배정도빠름)            
		}


        //aabb 읽기 전용 쉐이더 생성
		_AABBVertexPositionIndexReader = new ComputeBuffer(_Vertices.Count, 16, ComputeBufferType.Default);
		_AABBCuboidCenterIndexReader = new ComputeBuffer(_CuboidsCenter.Length,16,ComputeBufferType.Default);
        _AABBCuboidScaleIndexReader = new ComputeBuffer(_CuboidsScale.Length,16,ComputeBufferType.Default);
		_ViewFrustum = new ComputeBuffer(6,16,ComputeBufferType.Default);

		_AABBVertexPositionIndexReader.SetData(_Vertices.ToArray());
		_AABBCuboidCenterIndexReader.SetData(_CuboidsCenter);
        _AABBCuboidScaleIndexReader.SetData(_CuboidsScale);       

		//쉐이더 버퍼 세팅
		_OCMaterial.SetBuffer("_AABBVertexPositionIndexReader", _AABBVertexPositionIndexReader);
        _OCMaterial.SetBuffer("_AABBCuboidCenterReader", _AABBCuboidCenterIndexReader);
        _OCMaterial.SetBuffer("_AABBCuboidScaleReader", _AABBCuboidScaleIndexReader);
		_OCMaterial.SetBuffer("_BufferWriter", _BufferWriter);
        _OCMaterial.SetBuffer("_FrustumPlanes", _ViewFrustum);
		
		_OCMaterial.SetInt("_DebugDrawAABBFlag", System.Convert.ToInt32(OCDebug_BoundingBox));
        _OCMaterial.SetFloat("_PixErrThreshold", PixErrThreshold);   
		_OCMaterial.SetFloat("_FOV", Camera.main.fieldOfView); 

		//디버깅용
		//Debug.Log("_AABBCuboidCenterReader value: " + _OCMaterial.GetVector("_AABBCuboidCenterReader"));
        isInitialized = true;
    }

    void Update()
    {
		if (Time.frameCount % CullingDelayFrame != 0) 
			return;	//프레임에 맞춘 딜레이 리턴
		
        
        Camera mainCamera = Camera.main;
        Vector3 cameraPosition = mainCamera.transform.position;
        _OCMaterial.SetVector("_CameraPos", new Vector4(cameraPosition.x, cameraPosition.y, cameraPosition.z, 1));
		
		//Debug.Log(mainCamera.fieldOfView);   
		//디버깅용
		//Debug.Log("cameraPositionVariable value: " + _OCMaterial.GetVector("_CameraPos"));


        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
             
        Vector4[] VFPlanes = new Vector4[6];
        for(int i =0;i<6;i++)
        {
            VFPlanes[i] = new Vector4(frustumPlanes[i].normal.x,frustumPlanes[i].normal.y, frustumPlanes[i].normal.z, frustumPlanes[i].distance );
        }

        _ViewFrustum.SetData(VFPlanes.ToArray());       

        // // 디버깅 : Frustum Planes 출력
        // for (int i = 0; i < 6; i++)
        // {
        //     Debug.Log("Frustum Plane " + i + ": " + frustumPlanes[i]);
        // }  



		_BufferWriter.GetData(_OcclusionResult);

        
        bool state = VectorArrayStateAvailable (_OcclusionResult, _OcclusionResultCache);
		if (!state)
        {         
			int counter = 0;   
            for (int i=0; i<_OCTargetMeshRenderers.Count; i++)
            {
                for (int j=0; j<_OCTargetMeshRenderers[i].Count; j++)
                {
					if(_OcclusionResult[i] > 0.0f)
                    {_OCTargetMeshRenderers[i][j].enabled = true;
					counter++;
					}
					else
					_OCTargetMeshRenderers[i][j].enabled = false;
                }
            }
			CulledObjectNum = counter;
            VectorArrayCopy(_OcclusionResult, _OcclusionResultCache);
        }

        System.Array.Clear(_OcclusionResult, 0, _OcclusionResult.Length);
        _BufferWriter.SetData(_OcclusionResult);
		
			
		
    }


    //AABB 정점으로 중심점 구하기
    Vector4 GetCenterFromCubeVertices (Vector4[] verts)
	{
		Vector3 total = Vector3.zero;
		int length = verts.Length;
		for (int i = 0; i < length; i++)
		{
			total += new Vector3(verts[i].x, verts[i].y, verts[i].z);
		}
		Vector3 r = total / length;
		
		Vector4 output = new Vector4(r.x, r.y, r.z, 1.0f); //정점이니까 동차좌표 1.0f
		return output;
	}

    //AABB의 반지름(대각선의 절반)구하기
    Vector4 GetScaleFromCubeVertices (Vector4[] verts)
	{
		Vector3 min = Vector3.positiveInfinity;
		Vector3 max = Vector3.negativeInfinity;
		for (int i = 0; i < verts.Length; i++)
		{
			Vector3 point = new Vector3(verts[i].x, verts[i].y, verts[i].z);
			min = Vector3.Min(min, point);
			max = Vector3.Max(max, point);
		}

		Vector3 r = (max - min) * 0.5f;
		
		Vector4 output = new Vector4(r.x, r.y, r.z, 0.0f); //벡터니까 동차좌표 0.0f

		return output;
	}

    //AABB 박스의 정점 배열 구하는 함수
    Vector4[] GetAABBVerticesFromOCTarget (GameObject parent, int index)
	{
        
		Bounds bounds = new Bounds (Vector3.zero, Vector3.zero);
		bool hasBounds = false;
		Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();

        //부모의 바운딩 박스는 자식노드를 포함해야한다... 
        //(각개로 변경하기 원하시면 이 부분을 고치시면 됩니다)
		for (int i=0; i<renderers.Length; i++) 
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
		
		for (int i=0; i<vertices.Length; i++)
		{
			//점 위치 , 인덱스
			Vector3 p = cube.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]);    //인덱스에서 vertex추출
			vertices[i] = new Vector4(p.x, p.y, p.z, index);
		}

		for (int i=0; i<(vertices.Length/6.0); i++)
		{
			output.Add(vertices[6*i+1]);
			output.Add(vertices[6*i+2]);
			output.Add(vertices[6*i]);
			output.Add(vertices[6*i+5]);
		}

		Destroy(bc);
		Destroy(cube);
		return output.ToArray();	//바운딩 박스의 Pos와 Index 반환
    }


    bool Vector4ArrayStateAvailable (Vector4[] a, Vector4[] b)
	{
		for (int i=0; i<a.Length; i++)
		{
			bool x = Vector4.Dot(a[i], a[i]) > 0.0f;
			bool y = Vector4.Dot(b[i], b[i]) > 0.0f;
			if (x != y) return false;
		}
		return true;
	}

	bool VectorArrayStateAvailable (float[] a, float[] b)
	{
		for (int i=0; i<a.Length; i++)
		{
			if (a[i] != b[i]) return false;
		}
		return true;
	}

	void Vector4ArrayCopy (Vector4[] source, Vector4[] destination)
	{
		for (int i=0; i<source.Length; i++) 
			destination[i] = source[i];
	}

	void VectorArrayCopy (float[] source, float[] destination)
	{
		for (int i=0; i<source.Length; i++) 
			destination[i] = source[i];
	}

	bool Vector4ArrayIsSame (Vector4[] a, Vector4[] b)
	{
		for (int i=0; i<a.Length; i++)
		{
			if(a[i].x != b[i].x) 
				return false;
			else if(a[i].y != b[i].y)
				return false;
		}
		return true;
	}

    // bool isBoxIntersectCamera (Vector3 camPos, AABBCuboid cuboid)
	// {
	// 	float mx = Mathf.Max(camPos.x - cuboid.Center.x - cuboid.Scale.x, cuboid.Center.x - camPos.x - cuboid.Scale.x);
	// 	float my = Mathf.Max(camPos.y - cuboid.Center.y - cuboid.Scale.y, cuboid.Center.y - camPos.y - cuboid.Scale.y);
	// 	float mz = Mathf.Max(camPos.z - cuboid.Center.z - cuboid.Scale.z, cuboid.Center.z - camPos.z - cuboid.Scale.z);
	// 	float result = Mathf.Max(Mathf.Max(mx, my), mz);
	// 	if (result < 0.0)
	// 	 	return true;
	// 	else
	// 		return false;
	// }




    void OnRenderObject() 
	{	 	
        //최종 렌더링 단계에서 정점단위 입력
		//Graphics.ClearRandomWriteTargets();	
        _OCMaterial.SetPass(0);
        //Graphics.DrawProceduralNow(MeshTopology.Triangles, _Vertices.Count, 1);
        Graphics.DrawProceduralNow(MeshTopology.Points, _CuboidsCenter.Length, 1);

	}


    void OnEnable()
    {
        Initialize();
        
    }

    void OnDisable()
	{		
		_AABBVertexPositionIndexReader.Dispose();
        _AABBCuboidCenterIndexReader.Dispose();
        _AABBCuboidScaleIndexReader.Dispose();
		_BufferWriter.Dispose();
		_ViewFrustum.Dispose();
		
		for (int i=0; i<_OCTargetMeshRenderers.Count; i++)
		{
			for (int j=0; j<_OCTargetMeshRenderers[i].Count; j++)
			{
				_OCTargetMeshRenderers[i][j].enabled = true;
			}
		}
		isInitialized = false;
	}
    

	public void SetChildObjectListButton()
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
		
		OCTargetObjects = trees.ToArray();
	}
	public void ResetChildObjectListButton()
	{
		OCTargetObjects = null;
	}


	public void ButtonEnable()
	{
		if(this.enabled == false)
			this.enabled = true;
	}


	public void ButtonDisable()
	{
		if(this.enabled == true)
			this.enabled = false;
	}

	
}
