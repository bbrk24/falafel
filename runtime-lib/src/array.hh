#pragma once

#include "cow.hh"
#include "panic.hh"
#include "typedefs.hh"
#include <cstddef>
#include <cstring>
#include <initializer_list>
#include <new>
#include <utility>

template<typename T>
struct Array final {
public:
    Array() = default;
    Array(size_t capacity) : m_buffer(capacity) { }

    Array(std::initializer_list<T> list) : m_buffer(list.size())
    {
        if (list.size() > 0) {
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
        if (index < 0) {
            panic("Invalid index");
        }
        return m_buffer[static_cast<size_t>(index)];
    }

    void _indexset(Int index, T&& value)
    {
        if (index < 0) {
            panic("Invalid index");
        }
        m_buffer.ensure_unique();
        m_buffer[static_cast<size_t>(index)] = std::move(value);
    }

    void _indexset(Int index, const T& value)
    {
        if (index < 0) {
            panic("Invalid index");
        }
        m_buffer.ensure_unique();
        m_buffer[static_cast<size_t>(index)] = value;
    }

    Int length() const noexcept { return static_cast<Int>(m_buffer.length()); }

    void visit_children(std::function<void(Object*)> visitor) { visitor(m_buffer); }

    void clear() { m_buffer.clear(); }

private:
    CowBuffer<T> m_buffer;
};
