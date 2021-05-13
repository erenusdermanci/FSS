
int get_index(int2 pos, int2 buffer_size)
{
    return pos.y * buffer_size.x + pos.x;
}

int get_valid_index(int2 pos, int2 buffer_size)
{
    if (pos.x < 0 || pos.y < 0 || pos.x >= buffer_size.x || pos.y >= buffer_size.y)
        return -1;
    return get_index(pos, buffer_size);
}

int3 get_block_position(int2 pos, int2 buffer_size)
{
    return int3(pos, get_index(pos, buffer_size));
}

int3 get_valid_block_position(int2 pos, int2 buffer_size)
{
    return int3(pos, get_valid_index(pos, buffer_size));
}
