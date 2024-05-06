using System.Collections;
using System.Collections.Generic;
//evil :: Tolist 때문에 꼭 써야하는걸까
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
 #if UNITY_EDITOR
 using UnityEditor;
 #endif


 #if UNITY_EDITOR
 [CustomEditor(typeof(OcclusionCullingManager))]
 public class OcclusionCullingManager_Editor : Editor
 {
     public override void OnInspectorGUI()
     {
         //DrawDefaultInspector(); // for other non-HideInInspector fields
		 base.OnInspectorGUI();
         OcclusionCullingManager script = (OcclusionCullingManager)target;
 

		if (GUILayout.Button("Set OC Objects"))
        {
            script.SetChildObjectListButton();
        }
		if (GUILayout.Button("Reset All OC Objects"))
        {
            script.ResetChildObjectListButton();
        }


         // draw checkbox for the bool
         script.OCDebug = EditorGUILayout.Toggle("Occlusion Culling Debug", script.OCDebug);
         if (script.OCDebug) // if bool is true, show other fields
         {
               EditorGUILayout.BeginHorizontal();
        
        
			 script.OCDebug_BoundingBox = EditorGUILayout.Toggle("Draw Bounding Box", script.OCDebug_BoundingBox);
             script.FrameDelay = EditorGUILayout.IntField("Frame Delay", script.FrameDelay);
			 //script.toggleOC = EditorGUILayout.ObjectField("OC Toggle GUI", script.toggleOC, typeof(Toggle), true);

			 EditorGUILayout.EndHorizontal();
         }

		//base.OnInspectorGUI();

		
     }
 }
 #endif


public class OcclusionCullingManager : MonoBehaviour
{

	[Header("Occlusion Culling 대상 메쉬들")]
	[HideInInspector] // HideInInspector makes sure the default inspector won't show these fields.
     public bool OCDebug;
 
	[HideInInspector]
	// 디버그용으로 AABB 그리기
    public bool OCDebug_BoundingBox = false;

	[HideInInspector]
	// 딜레이타임 비율
	public int FrameDelay = 1;
	    
    
    // 쉐이더 파일
    public Shader OCShader;
    public float boundingBoxFactor = 1.01f;
	public GameObject[] OCTargetObjects;


    //쉐이더용 material과 renderer
    private Material _FinalRendringMaterial;
    private List<List<Renderer>> _MeshRenderers;



    //쉐이더용 컴퓨트버퍼 (AABB 기반 차폐테스트용 )
    private ComputeBuffer _BufferWriter;	
	private ComputeBuffer _AABBPositionIndexReader;

    private Vector4[] _Elements;
    private Vector4[] _Cache;
    private List<Vector4> _Vertices;



	struct AABBCube
	{
		public Vector3 Center;
		public Vector3 Scale;
	};
    private AABBCube[] _Cuboids;


    
    
    void Initialize()
    {
		//Shader 실행을 위한 머티리얼 생성
        if (_FinalRendringMaterial == null) 
			_FinalRendringMaterial = new Material(OCShader);
        
		_MeshRenderers = new List<List<Renderer>>();

		//1. 프러스텀 컬링
		//2. LOD 수행
		//3. OC 수행

        //Early-Z용 Shader 변수들
		//Target (UAV) Object 갯수만큼 float4
        _BufferWriter = new ComputeBuffer(OCTargetObjects.Length, 16, ComputeBufferType.Default);
			
		
		_Elements = new Vector4[OCTargetObjects.Length];
		_Cache = new Vector4[OCTargetObjects.Length];
        
		//바운딩박스 / 카메라 충돌처리
		_Cuboids = new AABBCube[OCTargetObjects.Length];

		if (_Cache.Length > 0) 
			_Cache[0] = Vector4.one;
		

		//aabb
		_Vertices = new List<Vector4>();

		//초기화~
		Graphics.ClearRandomWriteTargets();	
		Graphics.SetRandomWriteTarget(1, _BufferWriter, false);	//Shader Output (UAV) 설정

        for (int i=0; i<OCTargetObjects.Length; i++)
		{
			_MeshRenderers.Add(OCTargetObjects[i].GetComponentsInChildren<Renderer>().ToList());
			Vector4[] aabb = GetAABBVerticesFromOCTarget(OCTargetObjects[i], i);	// pos, index 저장됨
			
			_Cuboids[i].Center = GetCenterFromCubeVertices(aabb);
			_Cuboids[i].Scale = GetScaleFromCubeVertices(aabb);

			//AABB관련 데이터 정리됨
			_Vertices.AddRange(aabb); //aabb 전체 추가 (한번에 최적화 : 10배정도빠름)
		}

		//aabb 읽기 전용 ()
		_AABBPositionIndexReader = new ComputeBuffer(_Vertices.Count, 16, ComputeBufferType.Default);
		_AABBPositionIndexReader.SetData(_Vertices.ToArray());

		//쉐이더 버퍼 세팅
		_FinalRendringMaterial.SetBuffer("_AABBPositionIndexReader", _AABBPositionIndexReader);
		_FinalRendringMaterial.SetBuffer("_BufferWriter", _BufferWriter);
		
		_FinalRendringMaterial.SetInt("_DebugFlag", System.Convert.ToInt32(OCDebug_BoundingBox));

				
    }

    //AABB 박스의 정점 배열 구하는 함수
    Vector4[] GetAABBVerticesFromOCTarget (GameObject parent, int index)
	{
        
		Bounds bounds = new Bounds (Vector3.zero, Vector3.zero);
		bool hasBounds = false;
		Renderer[] renderers = parent.GetComponentsInChildren<Renderer>();
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
		
		//큐브 정점들 저장하기 위함
		Vector4[] vertices = new Vector4[mesh.triangles.Length];
				
		for (int i=0; i<vertices.Length; i++)
		{
			//점 위치 , 인덱스
			Vector3 p = cube.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]);
			vertices[i] = new Vector4(p.x, p.y, p.z, index);
		}
		Destroy(bc);
		Destroy(cube);
		return vertices;	//바운딩 박스의 Pos와 Index 반환
    }

    //AABB 정점으로 중심점 구하기
    Vector3 GetCenterFromCubeVertices (Vector4[] verts)
	{
		Vector3 total = Vector3.zero;
		int length = verts.Length;
		for (int i = 0; i < length; i++)
		{
			total += new Vector3(verts[i].x, verts[i].y, verts[i].z);
		}
		return total / length;
	}

    //AABB의 반지름(대각선의 절반)구하기
    Vector3 GetScaleFromCubeVertices (Vector4[] verts)
	{
		Vector3 min = Vector3.positiveInfinity;
		Vector3 max = Vector3.negativeInfinity;
		for (int i = 0; i < verts.Length; i++)
		{
			Vector3 point = new Vector3(verts[i].x, verts[i].y, verts[i].z);
			min = Vector3.Min(min, point);
			max = Vector3.Max(max, point);
		}
		return (max - min) * 0.5f;
	}

	//카메라와 박스와 부딪히는지 체크하는 용도
	bool isBoxIntersectCamera (Vector3 camPos, AABBCube cuboid)
	{
		float mx = Mathf.Max(camPos.x - cuboid.Center.x - cuboid.Scale.x, cuboid.Center.x - camPos.x - cuboid.Scale.x);
		float my = Mathf.Max(camPos.y - cuboid.Center.y - cuboid.Scale.y, cuboid.Center.y - camPos.y - cuboid.Scale.y);
		float mz = Mathf.Max(camPos.z - cuboid.Center.z - cuboid.Scale.z, cuboid.Center.z - camPos.z - cuboid.Scale.z);
		float result = Mathf.Max(Mathf.Max(mx, my), mz);
		if (result < 0.0)
		 	return true;
		else
			return false;
	}

	Vector3 _CachCamPos;
	Vector3 _CachCamRight;
	Vector3 _CachCamUp;
	Vector3 _CachCamDir;

	Vector3 pos;
	Vector3 right;
	Vector3 up;
	Vector3 dir;


	bool OCCompleted = true;


	// Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Vector3 pos = Camera.main.transform.position;
		// Vector3 right = Camera.main.transform.right;
		// Vector3 up = Camera.main.transform.up;
		// Vector3 dir = Camera.main.transform.forward;

		if (Time.frameCount % FrameDelay != 0) 
			return;	//프레임에 맞춘 딜레이 리턴

		
		//if(OCCompleted)
		{
			_BufferWriter.GetData(_Elements);
			bool state = Vector4ArrayStateAvailable (_Elements, _Cache);
			if (!state)
			{
				
				for (int i=0; i<_MeshRenderers.Count; i++)
				{
					for (int j=0; j<_MeshRenderers[i].Count; j++)
					{

						//박스 안에 카메라가 들어가있을 경우
						if(isBoxIntersectCamera(pos,_Cuboids[i]))
							_MeshRenderers[i][j].enabled = true;
						else
							_MeshRenderers[i][j].enabled = (Vector4.Dot(_Elements[i], _Elements[i]) > 0.0f);
					}
				}
				Vector4ArrayCopy(_Elements, _Cache);
			}
			
			System.Array.Clear(_Elements, 0, _Elements.Length);
			_BufferWriter.SetData(_Elements);
		}
			
		
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

	void Vector4ArrayCopy (Vector4[] source, Vector4[] destination)
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

	
    // Unity Events
    void OnEnable()
    {
        Initialize();
    }

    void OnRenderObject() 
	{	

		// if(_CachCamPos == pos && _CachCamRight == right && _CachCamUp == up && _CachCamDir == dir)
		// {
			
		// 	OCCompleted = false;
		// }
		// else
		// {
		 	
			_FinalRendringMaterial.SetPass(0);
			Graphics.DrawProceduralNow(MeshTopology.Triangles, _Vertices.Count, 1);
			OCCompleted = true;

			// _CachCamPos = pos;
			// _CachCamRight = right;
			// _CachCamUp = up;
			// _CachCamDir = dir;
		//}
		
	}

    void OnDisable()
	{
		
		_AABBPositionIndexReader.Dispose();
		_BufferWriter.Dispose();
		
		
		for (int i=0; i<_MeshRenderers.Count; i++)
		{
			for (int j=0; j<_MeshRenderers[i].Count; j++)
			{
				_MeshRenderers[i][j].enabled = true;
			}
		}
	}

	
	public Toggle toggleOC = null;

	void OnApplicationFocus(bool hasFocus)
	{
		//this.enabled = hasFocus;			
		
		if(toggleOC != null)
			this.enabled = toggleOC.isOn;
		else
			this.enabled = hasFocus;
	}

	// float Box (float3 p, float3 c, float3 s)
	// {
	// 	float mx = max(p.x - c.x - s.x, c.x - p.x - s.x);
	// 	float my = max(p.y - c.y - s.y, c.y - p.y - s.y);
	// 	float mz = max(p.z - c.z - s.z, c.z - p.z - s.z);
	// 	return max(max(mx, my), mz);
	// }

	// [numthreads(8,1,1)]
	// void CSMain (uint threadId : SV_DispatchThreadID)
	// {
	// 	uint numStructs, stride;
	// 	_AABB.GetDimensions(numStructs, stride);
	// 	if (threadId >= numStructs) return;
	// 	float result = Box(_Point.xyz, _AABB[threadId].Center, _AABB[threadId].Scale);
	// 	if (result < 0.0) _Intersection[0] = threadId;
	// }
	
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
	public void setFrameDelay1()
	{
		FrameDelay = 1;
	}
	public void setFrameDelay2()
	{
		FrameDelay = 2;
	}
	public void setFrameDelay3()
	{
		FrameDelay = 3;
	}

}
