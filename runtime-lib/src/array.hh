#pragma once

#include "cow.hh"
#include "mallocator.hh"
#include "panic.hh"
#include "typedefs.hh"
#include "typeinfo.hh"
#include <cstddef>
#include <cstring>
#include <functional>
#include <new>
#include <unordered_map>
#include <utility>

class String;

namespace falafel_internal {
extern std::unordered_map<
    TypeInfo, TypeInfo, std::hash<TypeInfo>, falafel_internal::MethodEquality<TypeInfo>,
    falafel_internal::Mallocator<std::pair<const TypeInfo, TypeInfo>>
>
    array_typeinfos;

extern String* const array_str;

const TypeInfo& make_array_info(const TypeInfo& element_info);
}

template<typename T>
struct Array final {
public:
    Array() noexcept = default;
    Array(size_t capacity) : m_buffer(capacity) { }

    Array(std::initializer_list<T> list) : m_buffer(list.size())
    {
        if (list.size() > 0U) {
            m_buffer.length_mut() = list.size();
            memcpy(m_buffer.base_pointer(), list.begin(), list.size() * sizeof(T));
        }
    }

    void push(T&& el)
    {
        m_buffer.ensure_unique(m_buffer.length() + 1U);
        void* location = static_cast<void*>(m_buffer + static_cast<ptrdiff_t>(m_buffer.length()));
        new (location) T(std::move(el));
        m_buffer.length_mut()++;
    }

    void push(const T& el)
    {
        m_buffer.ensure_unique(m_buffer.length() + 1U);
        void* location = static_cast<void*>(m_buffer + static_cast<ptrdiff_t>(m_buffer.length()));
        new (location) T(el);
        m_buffer.length_mut()++;
    }

    void pop()
    {
        assert(m_buffer.length() > 0U);
        m_buffer.ensure_unique();
        m_buffer[m_buffer.length() - 1U].~T();
        m_buffer.length_mut()--;
    }

    const T& _indexget(Int index) const
    {
        if (index < 0) [[unlikely]] {
            panic("Invalid index");
        }
        return m_buffer[static_cast<size_t>(index)];
    }

    void _indexset(Int index, T&& value)
    {
        if (index < 0) [[unlikely]] {
            panic("Invalid index");
        }
        m_buffer.ensure_unique();
        m_buffer[static_cast<size_t>(index)] = std::move(value);
    }

    void _indexset(Int index, const T& value)
    {
        if (index < 0) [[unlikely]] {
            panic("Invalid index");
        }
        m_buffer.ensure_unique();
        m_buffer[static_cast<size_t>(index)] = value;
    }

    size_t length() const noexcept { return m_buffer.length(); }

    void visit_children(std::function<void(Object*)> visitor) { visitor(m_buffer); }

    void clear() { m_buffer.clear(); }

    static const TypeInfo& get_type_info_static()
    {
        const TypeInfo& element_info = get_type_info<T>();
        auto iter = falafel_internal::array_typeinfos.find(element_info);
        if (iter != falafel_internal::array_typeinfos.end()) {
            return iter->second;
        } else {
            return falafel_internal::make_array_info(element_info);
        }
    }

    Int f_lengthib() const noexcept { return static_cast<Int>(length()); }
    Void f_clearvb() { clear(); }
    Void f_popvb() { pop(); }
    Void f_pushvh(T&& el) { push(std::move(el)); }
    Void f_pushvh(const T& el) { push(el); }

private:
    CowBuffer<T> m_buffer;
};
