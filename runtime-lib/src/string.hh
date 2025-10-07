#pragma once

#include "refcount.hh"
#include "typedefs.hh"
#include <cassert>
#include <cstring>
#include <functional>

class String final : public Object {
    friend struct StringBuilder;

private:
    struct Flags {
        bool is_small : 1;
        bool is_immortal : 1;

        inline bool operator==(const Flags& other) const noexcept
        {
            return is_small == other.is_small && is_immortal == other.is_immortal;
        }
    };

    static constexpr size_t MAX_SHORT_STRING_LEN
        = (2U * sizeof(char8_t*) - sizeof(Flags)) / sizeof(char8_t);

public:
    inline static String* allocate_immortal_utf8(const char8_t* literal)
    {
        size_t length = strlen(reinterpret_cast<const char*>(literal));

        return new String(
            Flags { .is_small = false, .is_immortal = true },
            Data { .char8_literal = literal },
            length,
            ImmortalMarker {}
        );
    }

    inline static String* allocate_small_utf8(const char8_t* literal)
    {
        Data data;

        size_t length = strlen(reinterpret_cast<const char*>(literal));
        assert(length < MAX_SHORT_STRING_LEN);

        memcpy(data.short_string, literal, (length + 1U) * sizeof(char8_t));

        return new String(
            Flags { .is_small = true, .is_immortal = true },
            data,
            length,
            ImmortalMarker {}
        );
    }

    static String* const empty;

    RcPointer<String> add(const String* other) const;

    __attribute__((pure)) Bool is_equal(const String* other) const noexcept;

    inline Bool is_not_equal(const String* other) const noexcept { return !is_equal(other); }

    __attribute__((pure)) Char _indexget(Int index) const noexcept;

    constexpr size_t length() const noexcept { return m_length; }

    void print() const;

    ~String() noexcept;

    constexpr Int f_lengthib() const noexcept { return static_cast<Int>(length()); }

private:
    static String* allocate_runtime_utf8(size_t length);

    union Data {
        char8_t* char8_ptr;
        const char8_t* char8_literal;
        char8_t short_string[MAX_SHORT_STRING_LEN];
    };

    constexpr String(Flags flags, Data data, size_t length) noexcept :
        Object(LeafMarker {}), m_flags(flags), m_data(data), m_length(length)
    {
    }
    constexpr String(Flags flags, Data data, size_t length, ImmortalMarker im) noexcept :
        Object(im), m_flags(flags), m_data(data), m_length(length)
    {
    }

    Flags m_flags;
    Data m_data;
    size_t m_length;
};

inline Void f_printvf(String* s) { s->print(); }
