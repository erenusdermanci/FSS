
uint wang_hash(uint seed)
{
    seed = seed ^ 61 ^ seed >> 16;
    seed *= 9;
    seed = seed ^ seed >> 4;
    seed *= 0x27d4eb2d;
    seed = seed ^ seed >> 15;
    return seed;
}

uint seed(uint3 id)
{
    uint seed = 3;
    const uint magic = 0x9e3779b9;
    seed ^= id.x + magic + (seed << 6) + (seed >> 2);
    seed ^= id.y + magic + (seed << 6) + (seed >> 2);
    seed ^= id.z + magic + (seed << 6) + (seed >> 2);
    return seed;
}

float rand(uint seed)
{
    return float(wang_hash(seed)) / 4294967296.0f;
}
