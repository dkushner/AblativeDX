/* * * * * * * * *
 * PARAM STRUCTS *
 * * * * * * * * */
struct VSPointIn
{
	float3 pos		:	POSITION;
	float4 color	:	COLOR;
};

struct GSParticleIn
{
	float3 pos		:	POSITION;
	float4 color	:	COLOR;
	float radius	:	RADIUS;
};

struct PSParticleIn
{
	float4 pos		:	SV_Position;
	float2 tex		:	TEXCOORD0;
	float3 eyePos	:	TEXCOORD1;
	float radius	:	TEXCOORD2;
	float4 color	:	COLOR;
};

struct PSParticleOut
{
	float4 color	: SV_Target;
	float depth		: SV_Depth;
};

/* * * * * * * * * * * 
 * CONSTANT BUFFERS  *
 * * * * * * * * * * */
cbuffer MatrixBuffer : register(cb1)
{
	float4x4 InvView;
	float4x4 ModelView;
	float4x4 Projection;
	float4x4 ModelViewProjection;
};

cbuffer ImmutableBuffer
{
	float3 Positions[4] =
	{
		float3(-1, 1, 0),
		float3(1, 1, 0),
		float3(-1, -1, 0),
		float3(1, -1, 0)
	};
	float2 Texcoords[4] =
	{
		float2(0, 0),
		float2(1, 0),
		float2(0, 1),
		float2(1, 1)
	};
	float3 LightDirection = float3(-0.5, -0.5, -2.0);
}

/* * * * * * * * * 
 * STATE OBJECTS *
 * * * * * * * * */

BlendState AdditiveBlending
{
	AlphaToCoverageEnable = FALSE;
	BlendEnable[0] = TRUE;
	SrcBlend = SRC_ALPHA;
	DestBlend = ONE;
	BlendOp = ADD;
	SrcBlendAlpha = ZERO;
	DestBlendAlpha = ZERO;
	BlendOpAlpha = ADD;
	RenderTargetWriteMask[0] = 0x0F;
};

BlendState NoBlending
{
    AlphaToCoverageEnable = FALSE;
    BlendEnable[0] = FALSE;
};

DepthStencilState EnableDepth
{
    DepthEnable = TRUE;
    DepthWriteMask = ALL;
	DepthFunc = LESS_EQUAL;
};

DepthStencilState DisableDepth
{
	DepthEnable = FALSE;
	DepthWriteMask = ZERO;
};

/* * * * * * * * * * * 
 *  SHADER FUNCTIONS *
 * * * * * * * * * * */
GSParticleIn VSParticleMain(VSPointIn input)
{
	GSParticleIn output;
	
	output.pos = input.pos;
	output.color = input.color;
	output.radius = 10.0;

	return output;
}

[maxvertexcount(4)]
void GSParticleMain(point GSParticleIn input[1], inout TriangleStream<PSParticleIn> SpriteStream)
{
	PSParticleIn output = (PSParticleIn)0;

	[unroll]
	for(int i = 0; i < 4; i++)
	{
		float3 position = Positions[i] * input[0].radius;
		position = mul(position, (float3x3)InvView) + input[0].pos;
		output.pos = mul(float4(position, 1.0), ModelViewProjection);
		output.eyePos = mul(float4(position, 1.0), ModelView).xyz;

		output.color = input[0].color;
		output.tex = Texcoords[i];
		output.radius = input[0].radius;
		
		SpriteStream.Append(output);
	}
	SpriteStream.RestartStrip();
}

PSParticleOut PSParticleMain(PSParticleIn input)
{
	PSParticleOut output;
	
	float3 norm;
	norm.xy = (input.tex * 2.0) - 1.0;

	float sqrad = dot(norm.xy, norm.xy);
	if(sqrad > 1.0) discard;
	norm.z = -sqrt(1.0 - sqrad);

	float4 pixelpos = float4(input.eyePos + (norm * input.radius), 1.0);
	float4 clspace = mul(pixelpos, Projection);
	float diffuse = max(0.0, dot(norm, LightDirection));
	
	output.depth = clspace.z;
	output.color = diffuse * input.color;

	return output;
}



/* * * * * * * * * * * * * *
 * TECHNIQUE DECLARATIONS  *
 * * * * * * * * * * * * * */
technique11 RenderParticles
{
	pass DensityDepth
	{
		SetVertexShader(CompileShader(vs_5_0, VSParticleMain()));
		SetGeometryShader(CompileShader(gs_5_0, GSParticleMain()));
		SetPixelShader(CompileShader(ps_5_0, PSParticleMain()));

		SetBlendState(NoBlending, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xFFFFFFFF);
		SetDepthStencilState(EnableDepth, 0);
	}
}