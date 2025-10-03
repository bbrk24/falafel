#pragma once

#include "refcount.hh"
#include "visitable.hh"
#include <cstddef>
#include <functional>
#include <optional>
#include <type_traits>
#include <utility>

template<typename T>
struct Optional final {
public:
    constexpr Optional() noexcept = default;
    constexpr Optional(std::nullptr_t) noexcept : m_value() { }

    constexpr Optional(const T& value) noexcept(noexcept(std::optional<T>(value))) : m_value(value)
    {
    }

    constexpr Optional(T&& value) noexcept(noexcept(std::optional<T>(value))) : m_value(value) { }
    constexpr Optional(const Optional<T>& other) : m_value(other.m_value) { }
    constexpr Optional(Optional<T>&& other) : m_value(std::move(other.m_value)) { }

    Optional& operator=(const Optional<T>& other)
    {
        m_value = other;
        return *this;
    }

    Optional& operator=(Optional<T>&& other)
    {
        m_value = other;
        return *this;
    }

    constexpr bool has_value() const noexcept { return m_value.has_value(); }
    constexpr bool f_hasValuebb() const noexcept { return has_value(); }

    template<typename F>
    auto or_else(F func) const noexcept(noexcept(func()))
    {
        if (m_value.has_value()) {
            return *m_value;
        } else {
            return func();
        }
    }

    void visit_children(std::function<void(Object*)> visitor)
        requires Visitable<T>
    {
        if (m_value.has_value()) {
            m_value->visit_children(visitor);
        }
    }

private:
    std::optional<T> m_value;
};

template<typename T>
struct Optional<RcPointer<T>> final {
public:
    constexpr Optional() noexcept = default;
    constexpr Optional(const RcPointer<T>& ptr) noexcept : m_value(ptr) { }
    constexpr Optional(RcPointer<T>&& ptr) noexcept : m_value(ptr) { }
    constexpr Optional(T* ptr) noexcept : m_value(ptr) { }

    constexpr bool has_value() const noexcept { return static_cast<T*>(m_value) != nullptr; }
    constexpr bool f_hasValuebb() const noexcept { return has_value(); }

    template<typename F>
    RcPointer<T> or_else(F func) const noexcept(noexcept(func()))
    {
        if (static_cast<T*>(m_value) == nullptr) {
            return func();
        } else {
            return m_value;
        }
    }

    void visit_children(std::function<void(Object*)> visitor)
    {
        visitor(static_cast<Object*>(m_value));
    }

private:
    RcPointer<T> m_value;
};

#define OR_ELSE(x, y) (x).or_else([&] { return y; })
