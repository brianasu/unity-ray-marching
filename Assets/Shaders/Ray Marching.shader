Shader "Hidden/Ray Marching/Ray Marching" 
{
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
	#pragma target 3.0
	#pragma profileoption MaxLocalParams=1024 
	#pragma profileoption NumInstructionSlots=4096
	#pragma profileoption NumMathInstructionSlots=4096	
	
	struct v2f {
		float4 pos : POSITION;
		float2 uv[2] : TEXCOORD0;
	};
	
	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	
	sampler2D _NoiseTex;
	sampler2D _FrontTex;
	sampler2D _BackTex;
	
	float4 _LightDir;
	float4 _LightPos;
	
	float _Dimensions;
	
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		
		o.uv[0] = v.texcoord.xy;
		o.uv[1] = v.texcoord.xy;
		#if SHADER_API_D3D9
		if (_MainTex_TexelSize.y < 0)
			o.uv[0].y = 1-o.uv[0].y;
		#endif			
		return o;
	}
	
	#define TOTAL_STEPS 64.0
	#define STEP_CNT 64
	#define STEP_SIZE 1 / 64.0
	#define ROW_STEP_SIZE 1 / 8.0
	#define DIMENSIONS 8.0
	
	float2 toTexPos(float3 pos)
	{
		float2 tex =  float2(
			pos.x / DIMENSIONS + floor(frac(pos.z * DIMENSIONS) * DIMENSIONS) / DIMENSIONS,
			1.0 - (pos.y / DIMENSIONS + floor(pos.z * DIMENSIONS) / DIMENSIONS));
			
		return tex;
			
//		return float2(
//			pos.x / _Dimensions + (floor(pos.z * _Dimensions) / _Dimensions),
//			pos.y);		
	}
	
	half4 raymarch(v2f i, float offset) 
	{
		float3 frontPos = tex2D(_FrontTex, i.uv[1]).xyz;		
		float3 backPos = tex2D(_BackTex, i.uv[1]).xyz;				
		float3 dir = backPos - frontPos;
		float3 pos = frontPos;
		float4 dst = 0;
		float3 stepDist = dir * STEP_SIZE;
		
		for(int k = 0; k < STEP_CNT; k++)
		{
			float4 src = tex2D(_MainTex, toTexPos(pos));
	        
	        //Front to back blending
		    //dst.rgb = dst.rgb + (1 - dst.a) * src.a * src.rgb;
		   	//dst.a   = dst.a   + (1 - dst.a) * src.a;     
	        
	        src.rgb *= src.a;
	        
	        dst = (1.0f - dst.a) * src + dst; 
			pos += stepDist;
		}

    	return dst;
	}

	ENDCG
	
Subshader {
	ZTest Always Cull Off ZWrite Off
	Fog { Mode off }
		
	Pass 
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		half4 frag(v2f i) : COLOR { return raymarch(i, 0); }	
		ENDCG
	}					
}

Fallback off
	
} // shader