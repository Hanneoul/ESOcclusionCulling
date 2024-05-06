// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/LODCulling"
{
    
    SubShader
    {
        Cull Off
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			CGPROGRAM
			#pragma vertex VSMain
			#pragma geometry GSMain
			#pragma fragment PSMain
			#pragma target 5.0
			
			//#pragma enable_d3d11_debug_symbols
			//#pragma debug

			struct GS_INPUT 
			{
				float4 vertex : POSITION;
				uint instance : TEXCOORD0;
			};

			struct PS_INPUT 
			{
				float4 vertex : SV_POSITION;
				uint instance : TEXCOORD0;
			};

			RWStructuredBuffer<float> _BufferWriter : register(u1);			//결과를 쓸 UAV 버퍼
			StructuredBuffer<float4> _AABBVertexPositionIndexReader;		// AABBCube를 구성하는 정점의 리스트
			StructuredBuffer<float4> _AABBCuboidCenterReader;				// AABBCube 리스트
			StructuredBuffer<float4> _AABBCuboidScaleReader;				// AABBCube 리스트
			StructuredBuffer<float4> _FrustumPlanes;

			//디버그용 AABB박스 그리기 플래그
			int _DebugDrawAABBFlag;	
			float4 _CameraPos;
			
			float _PixErrThreshold; 
			float _ScreenHeight;
			float _FOV;

			float Box (float3 p, float3 c, float3 s)
			{
				float mx = max(p.x - c.x - s.x, c.x - p.x - s.x);
				float my = max(p.y - c.y - s.y, c.y - p.y - s.y);
				float mz = max(p.z - c.z - s.z, c.z - p.z - s.z);
				return max(max(mx, my), mz);
				//0 이하면 안에 들어갔을 경우임
				// if (result <= 0.0)
		 		// 	return true;
				// else
				// 	return false;				
			}

			bool IsAABBInFrustum(float3 aabbCenter, float aabbRadius)
			{
				// 점과 모든 Frustum 평면의 거리를 계산합니다.
				for (int i = 0; i < 6; i++)
				{
					float distance = dot(_FrustumPlanes[i], float4(aabbCenter, 1.0));
					if (distance < -aabbRadius)
					{
						return false;
					}
				}
				// 모든 Frustum 평면에서 점이 안쪽에 있다는 것을 검사했으므로 true를 반환합니다.
				return true;
			}



			void VSMain (inout float4 vertex : POSITION, out uint instance : TEXCOORD0, uint id : SV_VertexID)
			{
				instance = id;
				if(Box(_CameraPos.xyz, _AABBCuboidCenterReader[id].xyz, _AABBCuboidScaleReader[id].xyz) <= 0.0f)
				{
				 	_BufferWriter[id] = 1.0f;
				}
				else
				{
					_BufferWriter[id] = 0.0f;
				}	

			}


			[maxvertexcount(24)]
			void GSMain(point GS_INPUT vertex[1], inout TriangleStream<PS_INPUT> triStream)
			{
				int id = vertex[0].instance;

				if( IsAABBInFrustum(_AABBCuboidCenterReader[id], length(_AABBCuboidScaleReader[id])))
				{
					//픽셀 에러란 오브젝트가 화면에 Rasterize 했을대 매핑되는 최대 반지름 길이를 pixel단위로 측정한 것이다.
					float pixErr = (length(_AABBCuboidScaleReader[id].xyz)*2) * _ScreenParams.y / (2.0 * distance(_AABBCuboidCenterReader[id].xyz,_CameraPos.xyz) * tan(radians(_FOV / 2.0f))); //오브젝트 위치에서의 프러스텀의 크기 구하기

					if( pixErr > _PixErrThreshold )	//오브젝트 크기가 허용 픽셀보다 클때만
					{		
						PS_INPUT v;
						for (uint j=0; j < 6; j++)
						{
							for (uint i = 0; i < 4; i++)
							{
								v.vertex = UnityObjectToClipPos(float4(_AABBVertexPositionIndexReader[id*24 + (4*j + i)].xyz, 1));
								v.instance = id;
								triStream.Append(v);						
							}
							triStream.RestartStrip();
						}
					}
				}
	
				
			
			}

			[earlydepthstencil]
			float4 PSMain (PS_INPUT In) : SV_TARGET
			{
				_BufferWriter[In.instance] = 1.0f;

				return float4(0.0, 0.0, 1.0, 0.2 * _DebugDrawAABBFlag);	//디버깅용 파란 박스 그림
			}
			ENDCG
		}       
    }
    FallBack "Diffuse"
}
