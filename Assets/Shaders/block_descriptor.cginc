
struct block_descriptor
{
    float density_priority; // Gases between 0 and 1, Air at 1 and others > 1
    float4 color;
    float base_health;
    int initial_states;
};