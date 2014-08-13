Shader "Hidden/Ray Marching/Render Noise" {
	
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
	CGINCLUDE
	#include "UnityCG.cginc"
	#include "noise.cginc"
	#pragma target 3.0
	//#pragma profileoption MaxLocalParams=1024 
	//#pragma profileoption NumInstructionSlots=4096
	//#pragma profileoption NumMathInstructionSlots=4096	
	
	struct v2f 
	{
		float4 pos : POSITION;
		float2 uv[2] : TEXCOORD0;
	};
	
	float _Dimensions;
	
	float2 toTexPos(float3 pos)
	{
		return float2(
			pos.x / _Dimensions + (floor(pos.z * _Dimensions) / _Dimensions),
			pos.y);			
	}	
	
	float _Slices;
	float _Strength;
	float4 _Scale;
	float4 _Octaves;
	float4 _LightDir;
	
	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	
	float4 _ClipDims;
	
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
	
	fixed4 frag(v2f i) : COLOR
	{
		float dim = _Slices;
		
		float2 uv = i.uv[1];
		
		float3 worldPos = float3(
			frac(uv.x * dim),
			frac(uv.y * dim), 
			(floor(uv.x * dim) / dim + floor((1 - uv.y) * dim)) / dim);
		
		float border = step(worldPos.x, _ClipDims.x) * step(1 - _ClipDims.x, worldPos.x);		
		border *= step(worldPos.y, _ClipDims.y) * step(1 - _ClipDims.y, worldPos.y);		
		border *= step(worldPos.z, _ClipDims.z) * step(1 - _ClipDims.z, worldPos.z);
		
		worldPos *= dim;
    	 
    	float3 offsets = float3(_Time.x * 10, 0, 0); 
    	 
    	float final = 
    		snoise(_Octaves.x * worldPos / _Scale + offsets) + 
    		snoise(_Octaves.y * worldPos / _Scale) + 
    		snoise(_Octaves.z * worldPos / _Scale) +
    		snoise(_Octaves.w * worldPos / _Scale);
	    
	    return pow(final / 4, _Strength) * border;
	}
	
	float4 fragLighting(v2f i) : COLOR
	{
		float2 tc = i.uv[0] + _MainTex_TexelSize.xy;
		
		float2 vPixelSize = _MainTex_TexelSize.xy;
		
		// Compute the necessary offsets:
		float2 o00 = tc + float2( -vPixelSize.x, -vPixelSize.y );
		float2 o10 = tc + float2(          0.0f, -vPixelSize.y );
		float2 o20 = tc + float2(  vPixelSize.x, -vPixelSize.y );

		float2 o01 = tc + float2( -vPixelSize.x, 0.0f          );
		float2 o21 = tc + float2(  vPixelSize.x, 0.0f          );

		float2 o02 = tc + float2( -vPixelSize.x,  vPixelSize.y );
		float2 o12 = tc + float2(          0.0f,  vPixelSize.y );
		float2 o22 = tc + float2(  vPixelSize.x,  vPixelSize.y );

		// Use of the sobel filter requires the eight samples surrounding the current pixel:
		float h00 = tex2D(_MainTex, o00).a;
		float h10 = tex2D(_MainTex, o10).a;
		float h20 = tex2D(_MainTex, o20).a;
		float h01 = tex2D(_MainTex, o01).a;
		float h21 = tex2D(_MainTex, o21).a;
		float h02 = tex2D(_MainTex, o02).a;
		float h12 = tex2D(_MainTex, o12).a;
		float h22 = tex2D(_MainTex, o22).a;
			
		// The Sobel X kernel is:
		//
		// [ 1.0  0.0  -1.0 ]
		// [ 2.0  0.0  -2.0 ]
		// [ 1.0  0.0  -1.0 ]
		float Gx = h00 - h20 + 2.0f * h01 - 2.0f * h21 + h02 - h22;
					
		// The Sobel Y kernel is:
		//
		// [  1.0    2.0    1.0 ]
		// [  0.0    0.0    0.0 ]
		// [ -1.0   -2.0   -1.0 ]
		float Gy = h00 + 2.0f * h10 + h20 - h02 - 2.0f * h12 - h22;
		
		// Generate the missing Z component - tangent
		// space normals are +Z which makes things easier
		// The 0.5f leading coefficient can be used to control
		// how pronounced the bumps are - less than 1.0 enhances
		// and greater than 1.0 smoothes.
		float Gz = 0.1f * sqrt(1.0f - Gx * Gx - Gy * Gy);

		// Make sure the returned normal is of unit length
		float3 normal = normalize(float3(2.0f * Gx, 2.0f * Gy, Gz));		
		
		fixed4 col = tex2D(_MainTex, i.uv[1]);		
		
		fixed light = max(0, dot(normal, _LightDir.xyz));
		
		return fixed4(light.xxx, col.a);
	}	
	
	struct v2fBlur
	{
		float4 pos : POSITION;			
		float2 uv[3] : TEXCOORD0;
	};	
	
	v2fBlur vertSpatialBlur(appdata_base v, float2 mask)
	{
		float2 offs = _MainTex_TexelSize.xy;
	
		v2fBlur o;
 		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
 		o.uv[0] = v.texcoord;
 		o.uv[1] = v.texcoord + mask * offs;
 		o.uv[2] = v.texcoord - mask * offs;
		return o;
	}
	
	float4 fragSpatialBlur (v2fBlur IN) : COLOR
	{
		float4 contribution = tex2D(_MainTex, IN.uv[0]);
		contribution += tex2D(_MainTex, IN.uv[1]);
		contribution += tex2D(_MainTex, IN.uv[2]);
		return contribution / 3;
	}			
	
	ENDCG
	
	
Subshader 
{
	ZTest Always Cull Off ZWrite Off
	Fog { Mode off }
	
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		ENDCG
	}	
	
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment fragLighting
		ENDCG
	}	
	
	Pass //1
	{
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		CGPROGRAM
		#pragma vertex vertBlur
		#pragma fragment fragSpatialBlur
		v2fBlur vertBlur(appdata_base v) { return vertSpatialBlur(v, float2(1, 0)); }
		ENDCG
	}	
	
	Pass //1
	{
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		CGPROGRAM
		#pragma vertex vertBlur
		#pragma fragment fragSpatialBlur
		v2fBlur vertBlur(appdata_base v) { return vertSpatialBlur(v, float2(0, 1)); }
		ENDCG
	}	
	
	Pass //1
	{
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		CGPROGRAM
		#pragma vertex vertBlur
		#pragma fragment fragSpatialBlur
		v2fBlur vertBlur(appdata_base v) { return vertSpatialBlur(v, float2(1, 0)); }
		ENDCG
	}												

}

Fallback off
	
} // shader