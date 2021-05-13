
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
struct block_descriptor
{
    int behavior_indices[];
    float4 color;
    float density_priority; // Gases between 0 and 1, Air at 1 and others > 1
    int initial_states;
    int block_tags;
};