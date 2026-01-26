#define THRESHOLD(c) { if (c.r>_Threshold) c.r=1.0; if (c.g>_Threshold) c.g=1.0; if (c.b>_Threshold) c.b=1.0; }
#define CHECKMAX(c) { if (c.r>1.0) c.r=1.0; if (c.g>1.0) c.g=1.0; if (c.b>1.0) c.b=1.0; }
/*
				if (texelsPerPixelX > 1.0)
				{
					float xIter = smoothstep(0, 1, (texelsPerPixelX-1));

					float blur = _BlurFactorX * xIter;
					//return i.color * blur;
					float4 ret = float4(0,0,0,0);
					return Blur1D(i.uv, float2(1, 0), blur) + Blur1D(i.uv, float2(0.5, 0), blur);
					//ret = Blur1D(i.uv, float2(1, 0), blur) + Blur1D(i.uv, float2(0.5, 0), blur);
					//ret = Blur1D(i.uv, float2(1, 0), blur);
					for (float x = 0; x < xIter; x++)
					{
						//ret += Blur1D(i.uv, float2(1, 0)/, blur);
						//ret += Blur1D(i.uv, float2(1, 0), blur/(x+1.0));
						ret += Blur1D(i.uv, float2(1, 0), x/xIter);
					}
					THRESHOLD(ret);
					ret.a = 1.0;
					return ret;
				}
equivalent to:
				GAUSS(texelsPerPixelX, float2(1,0));
*/
#define GAUSS(GAUSS_arg, GAUSS_direction, GAUSS_blurFactor) \
{ if (GAUSS_arg > 1.0) \
{ \
	float GAUSS_iter = smoothstep(0, 1, (GAUSS_arg-1)); \
	float GAUSS_blur = GAUSS_blurFactor * GAUSS_iter; \
	float4 GAUSS_ret = float4(0,0,0,0); \
	for (float GAUSS_g = 0; GAUSS_g <= GAUSS_iter*3; GAUSS_g++) \
	{ \
		GAUSS_ret += Blur1D(i.uv, GAUSS_direction, GAUSS_blur/(GAUSS_g+1.0)); \
	} \
	GAUSS_ret *= GAUSS_iter; \
	return GAUSS_ret * 0.5; \
} \
}
#define LIT(c) (c.r>0 || c.g>0 || c.b>0)
