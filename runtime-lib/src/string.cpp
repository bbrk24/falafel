#include "string.hh"
#include "panic.hh"
#include <cerrno>
#include <cstdio>
#include <cstdlib>
#include <system_error>

String* const String::empty = String::allocate_small_utf8(u8"");

static const TypeInfo string_info = TypeInfo { .name = String::allocate_small_utf8(u8"String") };

String::~String() noexcept
{
    if (!is_destroyed()) [[likely]] {
        if (!m_flags.is_immortal && !m_flags.is_small) {
            free(m_data.char8_ptr);
        }
    }
}

const TypeInfo& String::get_type_info_static() noexcept { return string_info; }
const TypeInfo& String::get_type_info_dynamic() const noexcept { return string_info; }

String* String::allocate_runtime_utf8(size_t length)
{
    char8_t* buffer = (char8_t*)calloc(length + 1U, sizeof(char8_t));
    if (buffer == nullptr) [[unlikely]] {
        Object::collect_cycles();
        buffer = (char8_t*)calloc(length + 1U, sizeof(char8_t));
        if (buffer == nullptr) {
            throw std::bad_alloc();
        }
    }

    return new String(
        Flags { .is_small = false, .is_immortal = false },
        Data { .char8_ptr = buffer },
        0U
    );
}

RcPointer<String> String::add(const String* other) const
{
    size_t length = m_length + other->m_length;

    if (length < MAX_SHORT_STRING_LEN) {
        Data data;
        memcpy(data.short_string, buffer_ptr(), m_length * sizeof(char8_t));
        memcpy(
            data.short_string + m_length,
            other->buffer_ptr(),
            other->m_length * sizeof(char8_t)
        );
        data.short_string[length] = u8'\0';
        return new String(Flags { .is_small = true, .is_immortal = false }, data, length);
    }

    char8_t* buffer = (char8_t*)calloc(length + 1U, sizeof(char8_t));
    if (buffer == nullptr) [[unlikely]] {
        Object::collect_cycles();
        buffer = (char8_t*)calloc(length + 1U, sizeof(char8_t));
        if (buffer == nullptr) {
            throw std::bad_alloc();
        }
    }

    memcpy(buffer, buffer_ptr(), m_length);
    memcpy(buffer + m_length, other->buffer_ptr(), other->m_length);
    return new String(
        Flags { .is_small = false, .is_immortal = false },
        Data { .char8_ptr = buffer },
        length
    );
}

Char String::_indexget(Int index) const noexcept
{
    if (index < 0 || index >= m_length) [[unlikely]] {
        panic("Index out of bounds");
    }
    return buffer_ptr()[index];
}

void String::print() const
{
    errno = 0;
    int result = puts(reinterpret_cast<const char*>(buffer_ptr()));
    if (result == EOF) {
        throw std::system_error(errno, std::generic_category());
    }
}

Bool String::is_equal(const String* other) const noexcept
{
    if (other == nullptr || m_length != other->m_length) {
        return false;
    }

    if (buffer_ptr() == other->buffer_ptr()) {
        return true;
    }

    return memcmp(buffer_ptr(), other->buffer_ptr(), m_length * sizeof(char8_t)) == 0;
}
