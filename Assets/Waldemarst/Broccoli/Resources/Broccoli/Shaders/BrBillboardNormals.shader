Shader "Broccoli/Billboard Normals" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.4
}
SubShader {
    Cull Off
    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        // vertex input: position, normal
        struct appdata {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f {
            float4 pos : SV_POSITION;
            float2 texcoord : TEXCOORD0;
            fixed4 color : COLOR;
        };

        sampler2D _MainTex;
        float4 _MainTex_ST;
		fixed _Cutoff;
        
        v2f vert (appdata v) {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            
            float3 normal = v.normal;
            o.color.xyz = (mul((float3x3)UNITY_MATRIX_IT_MV, normal)) + 0.5;

            o.color.w = 1.0;
            o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
            return o;
        }
        
        fixed4 frag (v2f i) : SV_Target {
            fixed4 col = tex2D(_MainTex, i.texcoord);
			clip(col.a - _Cutoff);
            fixed4 finalColor = i.color;
            finalColor.a = col.a;
            return finalColor; 
        }
        ENDCG
    }
}
}