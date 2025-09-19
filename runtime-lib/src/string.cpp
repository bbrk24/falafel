#include "string.hh"

#include <cuchar>
#include <iostream>

String::~String() noexcept  {
    if (!m_flags.is_immortal && !m_flags.is_small) {
        if (m_flags.encoding == Encoding::utf16) {
            free(m_data.char16_ptr);
        } else {
            free(m_data.char8_ptr);
        }
    }
}

void String::print() {
    if (m_flags.encoding == Encoding::utf16) {
        const char16_t* str;
        if (m_flags.is_immortal) {
            str = m_data.char16_literal;
        } else {
            str = m_data.char16_ptr;
        }

        char buffer[4];
        mbstate_t state;
        for (size_t i = 0; i < m_length; ++i)
        {
            size_t rc = c16rtomb(buffer, str[i], &state);
            if (rc != (size_t)-1)
                std::cout << std::string_view{buffer, rc};
        }
    } else {
        if (m_flags.is_small) {
            std::cout << reinterpret_cast<const char*>(m_data.short_string);
        } else if (m_flags.is_immortal) {
            std::cout << reinterpret_cast<const char*>(m_data.char8_literal);
        } else {
            std::cout << reinterpret_cast<const char*>(m_data.char8_ptr);
        }
    }
}

void String::visit_children(std::function<void(Object*)>) {}
