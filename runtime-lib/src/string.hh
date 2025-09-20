#pragma once

#include "max.hh"
#include "refcount.hh"
#include <cassert>
#include <cstring>

enum class Encoding : uint_least8_t {
    ascii,
    utf8,
    utf16,
};

class String final : public Object {
private:
    struct Flags {
        Encoding encoding : 2;
        bool is_small : 1;
        bool is_immortal : 1;
    };

    static constexpr size_t MAX_SHORT_STRING_LEN = (2U * max(sizeof(char8_t*), sizeof(char16_t*)) - sizeof(Flags)) / sizeof(char8_t);

public:
    inline static String* allocate_immortal_ascii(const char8_t* literal)
    {
        size_t length = 0U;
        while (literal[length]) {
            assert(literal[length] <= 0x7F);
            ++length;
        }

        return new String(
            Flags { .encoding = Encoding::ascii, .is_small = false, .is_immortal = true },
            Data { .char8_literal = literal },
            length,
            ImmortalMarker {}
        );
    }

    inline static String* allocate_immortal_utf8(const char8_t* literal)
    {
        size_t length = 0U;
        while (literal[length]) {
            ++length;
        }

        return new String(
            Flags { .encoding = Encoding::utf8, .is_small = false, .is_immortal = true },
            Data { .char8_literal = literal },
            length,
            ImmortalMarker {}
        );
    }

    inline static String* allocate_immortal_utf16(const char16_t* literal)
    {
        size_t length = 0U;
        while (literal[length]) {
            ++length;
        }

        String* str = new String(
            Flags { .encoding = Encoding::utf16, .is_small = false, .is_immortal = true },
            Data { .char16_literal = literal },
            length,
            ImmortalMarker {}
        );
        assert(str->m_flags.encoding == Encoding::utf16);
        return str;
    }

    inline static String* allocate_small_utf8(const char8_t* literal)
    {
        Data data;

        size_t length = 0U;
        while (literal[length]) {
            ++length;
            assert(length < MAX_SHORT_STRING_LEN);
        }

        memcpy(
            data.short_string,
            literal,
            (length + 1U) * sizeof(char8_t)
        );

        return new String(
            Flags { .encoding = Encoding::utf8, .is_small = true, .is_immortal = true },
            data,
            length,
            ImmortalMarker {}
        );
    }

    inline static String* allocate_small_ascii(const char8_t* literal)
    {
        Data data;

        size_t length = 0U;
        while (literal[length]) {
            assert(literal[length] <= 0x7F);
            ++length;
            assert(length < MAX_SHORT_STRING_LEN);
        }

        memcpy(
            data.short_string,
            literal,
            (length + 1U) * sizeof(char8_t)
        );

        return new String(
            Flags { .encoding = Encoding::ascii, .is_small = true, .is_immortal = true },
            data,
            length,
            ImmortalMarker {}
        );
    }

    RcPointer<String> add(const String* other) const;

    constexpr size_t byte_length() const noexcept
    {
        if (m_flags.encoding == Encoding::utf16) {
            return m_length * sizeof(char16_t);
        } else {
            return m_length * sizeof(char8_t);
        }
    }

    void print();

    ~String() noexcept;

protected:
    virtual void visit_children(std::function<void(Object*)> visitor) override;

private:
    union Data {
        char8_t* char8_ptr;
        char16_t* char16_ptr;
        const char8_t* char8_literal;
        const char16_t* char16_literal;
        char8_t short_string[MAX_SHORT_STRING_LEN];
    };

    constexpr String(Flags flags, Data data, size_t length) noexcept : Object(), m_flags(flags), m_data(data), m_length(length) { }
    constexpr String(Flags flags, Data data, size_t length, ImmortalMarker im) noexcept : Object(im), m_flags(flags), m_data(data), m_length(length) { }

    Flags m_flags;
    Data m_data;
    size_t m_length;
};

inline void print(String* strPointer)
{
    strPointer->print();
}
