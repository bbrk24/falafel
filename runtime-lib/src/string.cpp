#include "string.hh"

#include <iostream>

String* const String::empty = String::allocate_small_utf8(u8"");

String::~String() noexcept
{
    if (!m_flags.is_immortal && !m_flags.is_small) {
        free(m_data.char8_ptr);
    }
}

String* String::allocate_runtime_utf8(size_t length)
{
    char8_t* buffer = (char8_t*)calloc(length + 1U, sizeof(char8_t));
    if (buffer == nullptr) {
        Object::collect_cycles();
        char8_t* buffer = (char8_t*)calloc(length + 1U, sizeof(char8_t));
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
    const char8_t* own_buffer = m_flags.is_small ? m_data.short_string
        : m_flags.is_immortal                    ? m_data.char8_literal
                                                 : m_data.char8_ptr;
    const char8_t* other_buffer = other->m_flags.is_small ? other->m_data.short_string
        : other->m_flags.is_immortal                      ? other->m_data.char8_literal
                                                          : other->m_data.char8_ptr;

    if (length < MAX_SHORT_STRING_LEN) {
        Data data;
        strcpy(
            reinterpret_cast<char*>(data.short_string),
            reinterpret_cast<const char*>(own_buffer)
        );
        strcat(
            reinterpret_cast<char*>(data.short_string),
            reinterpret_cast<const char*>(other_buffer)
        );
        return new String(Flags { .is_small = true, .is_immortal = false }, data, length);
    }

    char8_t* buffer = (char8_t*)calloc(length + 1U, sizeof(char8_t));
    if (buffer == nullptr) {
        Object::collect_cycles();
        char8_t* buffer = (char8_t*)calloc(length + 1U, sizeof(char8_t));
        if (buffer == nullptr) {
            throw std::bad_alloc();
        }
    }

    memcpy(buffer, own_buffer, m_length);
    memcpy(buffer + m_length, other_buffer, other->m_length);
    return new String(
        Flags { .is_small = false, .is_immortal = false },
        Data { .char8_ptr = buffer },
        length
    );
}

void String::print()
{
    if (m_flags.is_small) {
        std::cout << reinterpret_cast<const char*>(m_data.short_string);
    } else if (m_flags.is_immortal) {
        std::cout << reinterpret_cast<const char*>(m_data.char8_literal);
    } else {
        std::cout << reinterpret_cast<const char*>(m_data.char8_ptr);
    }
    std::cout << '\n';
}

void String::visit_children(std::function<void(Object*)>) { }
