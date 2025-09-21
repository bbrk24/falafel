#include <cstddef>
#include <cstdint>

uint64_t hash_name(const unsigned char* name) noexcept
{
    uint64_t h = 0;
    size_t i = 0;
    while (name[i]) {
        h = (h << 8) + name[i];
        uint64_t high = 0xFF00'0000'0000'0000ULL & h;
        if (high != 0) {
            h = (h ^ (high >> 48)) & 0x00FF'FFFF'FFFF'FFFFULL;
        }
        ++i;
    }
    return h;
}
