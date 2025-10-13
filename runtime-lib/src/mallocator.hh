#pragma once

#include "max.hh"
#include "refcount.hh"
#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <new>
#include <type_traits>

namespace falafel_internal {
template<typename T>
class Mallocator {
    static_assert(alignof(max_align_t) % alignof(T) == 0U);

public:
    using value_type = T;
    using size_type = size_t;
    using is_always_equal = std::true_type;

    template<typename U>
    using rebind = Mallocator<U>;

    constexpr Mallocator() noexcept = default;

    template<typename U>
    constexpr Mallocator(const Mallocator<U>&) noexcept
    {
    }

    template<typename U>
    constexpr Mallocator(Mallocator<U>&& other) noexcept
    {
    }

    constexpr Mallocator<T>& operator=(const Mallocator<T>&) noexcept { }
    constexpr Mallocator<T>& operator=(Mallocator<T>&&) noexcept { }

    T* allocate(size_type n) const
    {
        if (n > max_size()) [[unlikely]] {
            throw std::bad_array_new_length();
        }

        void* ptr = malloc(n * sizeof(T));
        if (ptr == nullptr) [[unlikely]] {
            Object::collect_cycles();
            ptr = malloc(n * sizeof(T));
            if (ptr == nullptr) {
                throw std::bad_alloc();
            }
        }

        return static_cast<T*>(ptr);
    }

    void deallocate(T* p, size_type) const noexcept { free(p); }

    constexpr size_t max_size() const noexcept
    {
        return static_cast<size_t>(min<uintmax_t>(SIZE_MAX, PTRDIFF_MAX) / sizeof(T));
    }

    constexpr bool operator==(Mallocator other) const noexcept { return true; }
};

template<typename T>
struct MethodEquality {
    bool operator()(const T& lhs, const T& rhs) const { return lhs.is_equal(rhs); }

    bool operator()(const T* lhs, const T* rhs) const { return lhs->is_equal(rhs); }
};
}
