Shader "Custom/OcclusionCulling"
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
			#pragma fragment PSMain
			#pragma target 5.0

			RWStructuredBuffer<float4> _BufferWriter : register(u1);
			StructuredBuffer<float4> _AABBPositionIndexReader;
			//Debug 모드인지 아닌지 ( 0 or 1 )
			int _DebugFlag;	

			float4 _CamPos;

			float Box (float3 p, float3 c, float3 s)
			{
				float mx = max(p.x - c.x - s.x, c.x - p.x - s.x);
				float my = max(p.y - c.y - s.y, c.y - p.y - s.y);
				float mz = max(p.z - c.z - s.z, c.z - p.z - s.z);
				return max(max(mx, my), mz);
			}


			void VSMain (inout float4 vertex : POSITION, out uint instance : TEXCOORD0, uint id : SV_VertexID)
			{
				instance = _AABBPositionIndexReader[id].w;
				vertex = mul (UNITY_MATRIX_VP, float4(_AABBPositionIndexReader[id].xyz, 1.0));
			}

			[earlydepthstencil]
			float4 PSMain (float4 vertex : POSITION, uint instance : TEXCOORD0) : SV_TARGET
			{
				_BufferWriter[instance] = vertex;
				return float4(0.0, 0.0, 1.0, 0.2 * _DebugFlag);	//디버깅용 파란 박스 그림
			}
			ENDCG
		}       
    }
    FallBack "Diffuse"
}
