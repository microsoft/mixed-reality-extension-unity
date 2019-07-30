Shader "AvStreamPlugin/NV12"
{
	Properties
	{
		_Y("Texture", 2D) = "black" {}
		_UV("Texture", 2D) = "gray" {}
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Y;
			sampler2D _UV;
			float4 _Y_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _Y);
				return o;
			}

			fixed3 yuvToRGB(float y, float u, float v)
			{
				float rr = saturate(1.164 * (y - (16.0 / 255.0)) + 1.793 * (v - 0.5));
				float gg = saturate(1.164 * (y - (16.0 / 255.0)) - 0.534 * (v - 0.5) - 0.213 * (u - 0.5));
				float bb = saturate(1.164 * (y - (16.0 / 255.0)) + 2.115 * (u - 0.5));
				return fixed3(rr, gg, bb);
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed y = tex2D(_Y, i.uv).r;
				fixed2 uv = tex2D(_UV, i.uv).rg;
				fixed3 rgb = yuvToRGB(y, uv.r, uv.g);
				#if !UNITY_COLORSPACE_GAMMA
					return fixed4(GammaToLinearSpace(rgb), 1.0);
				#else
					return fixed4(rgb, 1.0);
				#endif
			}
			
			ENDCG
		}
	}
}
