#pragma once

#include "typedefs.hh"
#include <cstdint>
#include <typeindex>

class String;

struct TypeInfo {
    String* name;

    uint64_t hash() const noexcept;
    bool is_equal(const TypeInfo& other) const noexcept;
};

namespace falafel_internal {
extern const TypeInfo int_info;
extern const TypeInfo double_info;
extern const TypeInfo float_info;
extern const TypeInfo bool_info;
extern const TypeInfo void_info;
extern const TypeInfo char_info;
}

template<typename T>
const TypeInfo& get_type_info() noexcept
{
    return T::get_type_info_static();
}

template<>
constexpr const TypeInfo& get_type_info<Int>() noexcept
{
    return falafel_internal::int_info;
}

template<>
constexpr const TypeInfo& get_type_info<Double>() noexcept
{
    return falafel_internal::double_info;
}

template<>
constexpr const TypeInfo& get_type_info<Float>() noexcept
{
    return falafel_internal::float_info;
}

template<>
constexpr const TypeInfo& get_type_info<Bool>() noexcept
{
    return falafel_internal::bool_info;
}

template<>
constexpr const TypeInfo& get_type_info<Void>() noexcept
{
    return falafel_internal::void_info;
}

template<>
constexpr const TypeInfo& get_type_info<Char>() noexcept
{
    return falafel_internal::char_info;
}

namespace std {
template<>
struct hash<TypeInfo> {
    inline size_t operator()(const TypeInfo& ti) const noexcept { return ti.hash() % SIZE_MAX; }
};
}
