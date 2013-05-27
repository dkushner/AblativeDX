/* * * * * * * * *
 * PARAM STRUCTS *
 * * * * * * * * */
struct VSPointIn
{
	float3 pos		:	POSITION;
};

struct VSPassIn
{
	float4 pos		:	SV_POSITION;
	float2 tex		:	TEXCOORD0;
	float3 eyePos	:	TEXCOORD1;
};

struct GSParticleIn
{
	float3 pos		:	POSITION;
};

struct GSOutput
{
	float4 pos		:	SV_Position;
	float2 tex		:	TEXCOORD0;
	float3 eyePos	:	TEXCOORD1;
};


/* * * * * * * * *
 * INPUT BUFFERS *
 * * * * * * * * */
cbuffer MatrixBuffer
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
	float3 LightDirection = float3(-0.1, -0.1, -1.0);
	float4 FluidColor = float4(0.0, 0.0, 1.0, 1.0);
	float Radius = 10.0;
};

/* * * * * * * * * 
 * STATE OBJECTS *
 * * * * * * * * */
BlendState AdditiveBlending
{
	AlphaToCoverageEnable = FALSE;
	BlendEnable[0] = TRUE;
	SrcBlend = ONE;
	DestBlend = ONE;
	BlendOp = ADD;
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

	return output;
}

GSOutput VSParticlePassThrough(VSPassIn input)
{
	GSOutput output;

	output.pos = input.pos;
	output.tex = input.tex;
	output.eyePos = input.eyePos;

	return output;
}

[maxvertexcount(4)]
void GSParticleMain(point GSParticleIn input[1], inout TriangleStream<GSOutput> SpriteStream)
{
	GSOutput output = (GSOutput)0;

	[unroll]
	for(int i = 0; i < 4; i++)
	{
		float3 position = Positions[i] * Radius;
		position = mul(position, (float3x3)InvView) + input[0].pos;
		output.pos = mul(float4(position, 1.0), ModelViewProjection);
		output.eyePos = mul(float4(position, 1.0), ModelView).xyz;

		output.tex = Texcoords[i];
		
		SpriteStream.Append(output);
	}
	SpriteStream.RestartStrip();
}

float PSParticleDepth(GSOutput input) : SV_Depth
{
	float3 norm;
	norm.xy = (input.tex * 2.0) - 1.0;

	float sqrad = dot(norm.xy, norm.xy);
	if(sqrad > 1.0) discard;
	norm.z = -sqrt(1.0 - sqrad);

	float4 pixelpos = float4(input.eyePos + (norm * Radius), 1.0);
	float4 clspace = mul(pixelpos, Projection);
	float diffuse = max(0.0, dot(norm, LightDirection));
	
	return clspace.z;
}

float PSParticleDensity(GSOutput input) : SV_Target1
{
	float3 norm;
	norm.xy = (input.tex * 2.0) - 1.0;

	float sqrad = dot(norm.xy, norm.xy);
	if(sqrad > 1.0) discard;
	norm.z = -sqrt(1.0 - sqrad);

	float4 pixelpos = float4(input.eyePos + (norm * Radius), 1.0);
	float4 clspace = mul(pixelpos, Projection);
	float diffuse = max(0.0, dot(norm, LightDirection));

	return diffuse * FluidColor;
}


/* * * * * * * * * * * * * *
 * TECHNIQUE DECLARATIONS  *
 * * * * * * * * * * * * * */
GeometryShader gsCompiled = CompileShader(gs_5_0, GSParticleMain());
technique11 RenderParticles
{
	pass DepthPass
	{
		SetVertexShader(CompileShader(vs_5_0, VSParticleMain()));
		SetGeometryShader(ConstructGSWithSO(gsCompiled, "SV_Position.xyzw; TEXCOORD0.xy; TEXCOORD1.xyz"));
		SetPixelShader(CompileShader(ps_5_0, PSParticleDepth()));

		SetDepthStencilState(EnableDepth, 0);
		SetBlendState(NoBlending, float4(0.0, 0.0, 0.0, 0.0), 0xFFFFFFFF);
	}
	
	pass DensityPass
	{
		SetVertexShader(CompileShader(vs_5_0, VSParticlePassThrough()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_5_0, PSParticleDensity()));

		SetDepthStencilState(DisableDepth, 0);
		SetBlendState(AdditiveBlending, float4(0.0, 0.0, 0.0, 0.0), 0xFFFFFFFF);
	}
}