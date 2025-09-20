#include "string.hh"

#include <cuchar>
#include <iostream>
#include <utf8.h>

String::~String() noexcept  {
    if (!m_flags.is_immortal && !m_flags.is_small) {
        if (m_flags.encoding == Encoding::utf16) {
            free(m_data.char16_ptr);
        } else {
            free(m_data.char8_ptr);
        }
    }
}

RcPointer<String> String::add(const String* other) const {
    size_t length = m_length + other->m_length;
    bool is_ascii = m_flags.encoding == Encoding::ascii && other->m_flags.encoding == Encoding::ascii;

    if (m_flags.encoding != Encoding::utf16 && other->m_flags.encoding != Encoding::utf16 && length < MAX_SHORT_STRING_LEN) {
        const char8_t* own_buffer =
            m_flags.is_small ? m_data.short_string
            : m_flags.is_immortal ? m_data.char8_literal
            : m_data.char8_ptr;
        const char8_t* other_buffer =
            other->m_flags.is_small ? other->m_data.short_string
            : other->m_flags.is_immortal ? other->m_data.char8_literal
            : other->m_data.char8_ptr;

        Data data;
        strcpy(reinterpret_cast<char *>(data.short_string), reinterpret_cast<const char *>(own_buffer));
        strcat(reinterpret_cast<char *>(data.short_string), reinterpret_cast<const char *>(other_buffer));
        return new String(
            Flags { .encoding = is_ascii ? Encoding::ascii : Encoding::utf8, .is_small = true, .is_immortal = false },
            data,
            length
        );
    }

    Encoding dest_encoding;
    if (m_flags.encoding == Encoding::ascii) {
        dest_encoding = other->m_flags.encoding;
    } else if (other->m_flags.encoding == Encoding::ascii) {
        dest_encoding = m_flags.encoding;
    } else if (byte_length() < other->byte_length()) {
        dest_encoding = other->m_flags.encoding;
    } else {
        dest_encoding = m_flags.encoding;
    }

    if (dest_encoding == Encoding::utf16) {
        std::u16string own_string;
        std::u16string other_string;

        if (m_flags.encoding == Encoding::utf16) {
            const char16_t* own_buffer = m_flags.is_immortal ? m_data.char16_literal : m_data.char16_ptr;
            own_string = std::u16string(own_buffer);
        } else {
            const char8_t* own_buffer =
                m_flags.is_small ? m_data.short_string
                : m_flags.is_immortal ? m_data.char8_literal
                : m_data.char8_ptr;
            own_string = utf8::utf8to16(std::u8string(own_buffer));
        }

        if (other->m_flags.encoding == Encoding::utf16) {
            const char16_t* other_buffer = other->m_flags.is_immortal ? other->m_data.char16_literal : other->m_data.char16_ptr;
            other_string = std::u16string(other_buffer);
        } else {
            const char8_t* other_buffer =
                other->m_flags.is_small ? other->m_data.short_string
                : other->m_flags.is_immortal ? m_data.char8_literal
                : other->m_data.char8_ptr;
            other_string = utf8::utf8to16(std::u8string(other_buffer));
        }

        length = own_string.size() + other_string.size();

        char16_t* buffer = (char16_t*)calloc(length + 1U, sizeof (char16_t));
        if (buffer == nullptr) {
            Object::collect_cycles();
            char16_t* buffer = (char16_t*)calloc(length + 1U, sizeof (char16_t));
            if (buffer == nullptr) {
                throw std::bad_alloc();
            }
        }

        memcpy(buffer, own_string.data(), own_string.size());
        memcpy(buffer + own_string.size(), other_string.data(), other_string.size());
        return new String(
            Flags { .encoding = Encoding::utf16, .is_small = false, .is_immortal = false },
            Data { .char16_ptr = buffer },
            length
        );
    } else {
        std::u8string own_string;
        std::u8string other_string;

        if (m_flags.encoding == Encoding::utf16) {
            const char16_t* own_buffer = m_flags.is_immortal ? m_data.char16_literal : m_data.char16_ptr;
            own_string = utf8::utf16tou8(std::u16string(own_buffer));
        } else {
            const char8_t* own_buffer =
                m_flags.is_small ? m_data.short_string
                : m_flags.is_immortal ? m_data.char8_literal
                : m_data.char8_ptr;
            own_string = std::u8string(own_buffer);
        }

        if (other->m_flags.encoding == Encoding::utf16) {
            const char16_t* other_buffer = other->m_flags.is_immortal ? other->m_data.char16_literal : other->m_data.char16_ptr;
            other_string = utf8::utf16tou8(std::u16string(other_buffer));
        } else {
            const char8_t* other_buffer =
                other->m_flags.is_small ? other->m_data.short_string
                : other->m_flags.is_immortal ? m_data.char8_literal
                : other->m_data.char8_ptr;
            other_string = std::u8string(other_buffer);
        }

        length = own_string.size() + other_string.size();

        char8_t* buffer = (char8_t*)calloc(length + 1U, sizeof (char8_t));
        if (buffer == nullptr) {
            Object::collect_cycles();
            char8_t* buffer = (char8_t*)calloc(length + 1U, sizeof (char8_t));
            if (buffer == nullptr) {
                throw std::bad_alloc();
            }
        }

        memcpy(buffer, own_string.data(), own_string.size());
        memcpy(buffer + own_string.size(), other_string.data(), other_string.size());
        return new String(
            Flags { .encoding = is_ascii ? Encoding::ascii : Encoding::utf8, .is_small = false, .is_immortal = false },
            Data { .char8_ptr = buffer },
            length
        );
    }
}

void String::print() {
    if (m_flags.encoding == Encoding::utf16) {
        const char16_t* str = m_flags.is_immortal ? m_data.char16_literal : m_data.char16_ptr;
        std::u16string std16str(str);
        std::cout << reinterpret_cast<const char*>(utf8::utf16tou8(std16str).data());
    } else {
        if (m_flags.is_small) {
            std::cout << reinterpret_cast<const char*>(m_data.short_string);
        } else if (m_flags.is_immortal) {
            std::cout << reinterpret_cast<const char*>(m_data.char8_literal);
        } else {
            std::cout << reinterpret_cast<const char*>(m_data.char8_ptr);
        }
    }
    std::cout << '\n';
}

void String::visit_children(std::function<void(Object*)>) {}
