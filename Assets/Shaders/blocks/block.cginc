
struct block
{
    int type;
    int states;
    float lifetime;
    float2 velocity;
    float4 color;
};

struct lockedIndex
{
    int index;
    int lock;
};
