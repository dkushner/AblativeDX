#define KERNEL_RADIUS 3
#define KERNEL_LENGTH (2 * KERNEL_RADIUS + 1)

/* * * * * * * * * *
 * COMPUTE INPUTS  *
 * * * * * * * * * */
float GaussCoeff[KERNEL_LENGTH] =
{
	0.004431,
	0.05402,
	0.2420,
	0.399,
	0.2420, 
	0.05402, 
	0.004431
};
groupshared float BlurVector[KERNEL_LENGTH];

Texture2D SharpTexture : register(t0);
RWTexture2D<float> BlurTexture : register(u0);

/* * * * * * * * * * * 
 * COMPUTE FUNCTIONS *
 * * * * * * * * * * */
[numthreads(BLK_SIZE, 1, 1)]
void CSBilateralH(uint3 groupID : SV_GroupID, uint3 groupThreadID : SV_GroupThreadID)
{
	int2 textureSize;
	SharpTexture.GetDimensions(textureSize.x, textureSize.y);

	// Sample texture to TLS then sync.
	float depth = SharpTexture[int2(groupThreadID.x, groupID.y)].x;
	BlurVector[groupThreadID.x] = depth;
	GroupMemoryBarrierWithGroupSync();
	
	depth = 0.0;
	[unroll]
	for(int i = -KERNEL_RADIUS; i <= KERNEL_RADIUS; i++)
	{
		int nOffset = groupThreadID.x + i;
		nOffset = clamp(nOffset, 0, textureSize.x - 1);

		depth += GaussCoeff[(KERNEL_RADIUS) + i] * BlurVector[nOffset];
	}
	BlurTexture[int2(groupThreadID.x, groupID.y)] = depth;
}

[numthreads(1, BLK_SIZE, 1)]
void CSBilateralV(uint3 groupID : SV_GroupID, uint3 groupThreadID : SV_GroupThreadID)
{
	int2 textureSize;
	SharpTexture.GetDimensions(textureSize.x, textureSize.y);

	float depth = SharpTexture[int2(groupID.x, groupThreadID.y)].x;
	BlurVector[groupThreadID.y] = depth;
	GroupMemoryBarrierWithGroupSync();

	depth = 0.0;
	[unroll]
	for(int i = -KERNEL_RADIUS; i <= KERNEL_RADIUS; i++)
	{
		int nOffset = groupThreadID.x + i;
		nOffset = clamp(nOffset, 0, textureSize.y - 1);

		depth += GaussCoeff[(KERNEL_RADIUS) + i] * BlurVector[nOffset];
	}
	BlurTexture[int2(groupID.x, groupThreadID.y)] = depth;
}