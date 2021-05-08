
static int air = 0;
static int flame = 1;
static int oil = 2;
static int water = 3;
static int sand = 4;
static int dirt = 5;
static int stone = 6;
static int metal = 7;
static int border = 8;
static int gas = 9;
static int smoke = 10;
static int wood = 11;
static int coal = 12;
static int spark = 13;
static int lava = 14;
static int hardened_lava = 15;

static int central_chunk_index = 4;

static int chunk_size = 64;

static uint distances[64] =
{
    0, 0, 0, 0,
    1, 0, 0, 0,
    2, 0, 0, 0,
    1, 2, 0, 0,
    3, 0, 0, 0,
    1, 3, 0, 0,
    2, 3, 0, 0,
    1, 2, 3, 0,
    4, 0, 0, 0,
    1, 4, 0, 0,
    2, 4, 0, 0,
    1, 2, 4, 0,
    3, 4, 0, 0,
    1, 3, 4, 0,
    2, 3, 4, 0,
    1, 2, 3, 4
};
static uint bit_count[16] = { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };
static uint direction_x[8] = { 0, -1, 1, -1, 1, 0, -1, 1 };
static uint direction_y[8] = { -1, -1, -1, 0, 0, 1, 1, 1 };