#pragma once

#include <initializer_list>
#include <new>
#include <utility>

#include "cow.hh"

template<typename T>
struct Array final {
public:
    Array() = default;
    Array(size_t capacity) : m_buffer(capacity) { }

    Array(std::initializer_list<T> list) : m_buffer(list.size())
    {
        memcpy(m_buffer.base_pointer(), list.begin(), list.size());
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

    const T& get(size_t index) const { return m_buffer[index]; }

    void set(size_t index, T&& value)
    {
        m_buffer.ensure_unique();
        m_buffer[index] = std::move(value);
    }

    void set(size_t index, const T& value)
    {
        m_buffer.ensure_unique();
        m_buffer[index] = value;
    }

    constexpr size_t length() const noexcept { return m_buffer.length(); }

    void visit_children(std::function<void(Object*)> visitor) { visitor(m_buffer); }

    void clear() { m_buffer.clear(); }

private:
    CowBuffer<T> m_buffer;
};
