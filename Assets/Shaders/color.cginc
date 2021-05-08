#include "random.cginc"

float get_random_shift_amount(uint seed, float base_amount)
{
    return base_amount * (rand(seed) - 0.5f) * 2.0f;
}

float3 shift_color_component(float3 rgb, float amount)
{
    return clamp(rgb + rgb * amount, 0.0f, 1.0f);
}

float4 shift_color(float4 color, float base_amount, uint seed)
{
    return float4(shift_color_component(color.rgb, get_random_shift_amount(seed, base_amount)), color.a);
}