#pragma once

#include "refcount.hh"
#include "typedefs.hh"
#include <cassert>
#include <cstring>
#include <functional>

class String final : public Object {
    friend struct StringBuilder;
    friend struct TypeInfo;

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

    Bool is_equal(const String* other) const noexcept __attribute__((pure));

    inline Bool is_not_equal(const String* other) const noexcept { return !is_equal(other); }

    Char _indexget(Int index) const noexcept __attribute__((pure));

    constexpr size_t length() const noexcept { return m_length; }

    void print() const;

    ~String() noexcept;

    constexpr Int f_lengthib() const noexcept { return static_cast<Int>(length()); }

    inline virtual RcPointer<String> f_toStringsb() override { return this; }

    static const TypeInfo& get_type_info_static() noexcept;
    virtual const TypeInfo& get_type_info_dynamic() const noexcept override;

private:
    static String* allocate_runtime_utf8(size_t length);

    constexpr const char8_t* buffer_ptr() const noexcept
    {
        return m_flags.is_small   ? m_data.short_string
            : m_flags.is_immortal ? m_data.char8_literal
                                  : m_data.char8_ptr;
    }

    union __attribute__((packed)) Data {
        char8_t* char8_ptr;
        const char8_t* char8_literal;
        char8_t short_string[MAX_SHORT_STRING_LEN];
    };

    constexpr String(Flags flags, Data data, size_t length) noexcept :
        Object(LeafMarker {}), m_data(data), m_flags(flags), m_length(length)
    {
    }
    constexpr String(Flags flags, Data data, size_t length, ImmortalMarker im) noexcept :
        Object(im), m_data(data), m_flags(flags), m_length(length)
    {
    }

    Data m_data;
    Flags m_flags;
    size_t m_length;
};

inline Void f_printvf(String* s) { s->print(); }
