#include "typeinfo.hh"
#include "string.hh"

// TODO: include information besides name?

uint64_t TypeInfo::hash() const noexcept
{
    // PJW hash
    const char8_t* s = name->buffer_ptr();
    uint64_t result = 0ULL;

    for (size_t i = 0U; i < name->length(); ++i) {
        result = (result << 8U) + s[i];
        uint64_t high = result & 0xFF00'0000'0000'0000ULL;
        if (high != 0ULL) {
            result ^= high >> 48U;
            result &= ~high;
        }
    }

    return result;
}

bool TypeInfo::is_equal(const TypeInfo& other) const noexcept { return name->is_equal(other.name); }

namespace falafel_internal {
const TypeInfo int_info = TypeInfo { .name = String::allocate_small_utf8(u8"Int") };
const TypeInfo double_info = TypeInfo { .name = String::allocate_small_utf8(u8"Double") };
const TypeInfo float_info = TypeInfo { .name = String::allocate_small_utf8(u8"Float") };
const TypeInfo bool_info = TypeInfo { .name = String::allocate_small_utf8(u8"Bool") };
const TypeInfo void_info = TypeInfo { .name = String::allocate_small_utf8(u8"Void") };
const TypeInfo char_info = TypeInfo { .name = String::allocate_small_utf8(u8"Char") };
}
