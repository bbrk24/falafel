#pragma once

#include "max.hh"
#include "refcount.hh"
#include <cassert>
#include <cstring>
#include <stdexcept>
#include <type_traits>

template<typename T>
concept Visitable = requires(T x, std::function<void(Object*)> visitor) {
    { x.visit_children(visitor) } -> std::same_as<void>;
};

template<typename T>
    requires(!std::is_reference_v<T>)
class CowBuffer final {
    static_assert(sizeof(char) == 1U);

private:
    class Header final : public Object {
    public:
        size_t m_length;

    protected:
        inline void visit_children(std::function<void(Object*)> visitor) final override
        {
            if constexpr (std::is_convertible_v<T, Object*>) {
                T* base_pointer
                    = reinterpret_cast<T*>(reinterpret_cast<char*>(this) + header_offset());
                for (size_t i = 0; i < m_length; ++i) {
                    visitor(base_pointer[i]);
                }
            } else if constexpr (Visitable<T>) {
                T* base_pointer
                    = reinterpret_cast<T*>(reinterpret_cast<char*>(this) + header_offset());
                for (size_t i = 0; i < m_length; ++i) {
                    base_pointer[i].visit_children(visitor);
                }
            }
        }
    };

public:
    constexpr CowBuffer() noexcept : m_pointer(nullptr), m_capacity(0U) { }

    CowBuffer(size_t capacity) : m_pointer(nullptr) { realloc(capacity); }

    CowBuffer(const CowBuffer<T>& other) noexcept :
        m_pointer(other.m_pointer), m_capacity(other.m_capacity)
    {
        static_cast<Object*>(*this)->retain();
    }

    CowBuffer(CowBuffer<T>&& other) noexcept :
        m_pointer(other.m_pointer), m_capacity(other.m_capacity)
    {
        other.m_capacity = 0U;
        other.m_pointer = nullptr;
    }

    ~CowBuffer() { clear(); }

    CowBuffer<T>& operator=(const CowBuffer<T>& other)
    {
        if (m_pointer != nullptr) {
            static_cast<Object*>(*this)->release();
        }

        m_pointer = other.m_pointer;
        m_capacity = other.m_capacity;

        static_cast<Object*>(*this)->retain();
    }

    CowBuffer<T>& operator=(CowBuffer<T>&& other)
    {
        if (m_pointer != nullptr) {
            static_cast<Object*>(*this)->release();
        }

        m_pointer = other.m_pointer;
        m_capacity = other.m_capacity;

        other.m_pointer = nullptr;
        other.m_capacity = 0U;
    }

    void ensure_unique(size_t capacity)
    {
        if (m_pointer == nullptr) {
            realloc(capacity);
            return;
        }

        char* class_header_ptr = m_pointer - header_offset();
        Header* obj_ptr = reinterpret_cast<Header*>(class_header_ptr);

        if (obj_ptr->is_unique()) {
            ensure_capacity_at_least(capacity);
        } else {
            char* old_ptr = m_pointer;
            size_t old_length = length();

            m_pointer = nullptr;
            m_capacity = 0U;
            realloc(max(capacity, old_length));

            if constexpr (std::is_trivially_copy_constructible_v<T>) {
                memcpy(m_pointer, old_ptr, old_length);
            } else {
                T* bptr = reinterpret_cast<T*>(m_pointer);
                const T* old_bptr = reinterpret_cast<T*>(old_ptr);
                for (size_t i = 0; i < old_length; ++i) {
                    new (reinterpret_cast<void*>(bptr + i)) T(old_bptr[i]);
                }
            }

            obj_ptr->release();
        }
    }

    void ensure_unique() { ensure_unique(length()); }

    void realloc(size_t capacity)
    {
        if (capacity < length()) {
            throw std::logic_error("Capacity cannot be less than length");
        }
        size_t total_size = capacity * sizeof(T) + header_offset();
        if (m_pointer == nullptr) {
            if (capacity > 0U) {
                void* location = malloc(total_size);
                Header* header_ptr = new (location) Header();
                header_ptr->m_length = 0U;
                m_pointer = reinterpret_cast<char*>(location) + header_offset();
            }
        } else if (capacity == 0U) {
            static_cast<Object*>(*this)->release();
            m_pointer = nullptr;
        } else {
            void* old_header_ptr = static_cast<void*>(m_pointer - header_offset());
            void* realloc_ptr = ::realloc(old_header_ptr, total_size);
            if (realloc_ptr == nullptr) {
                Object::collect_cycles();
                realloc_ptr = ::realloc(old_header_ptr, total_size);
                if (realloc_ptr == nullptr) {
                    throw std::bad_alloc();
                }
            }
            m_pointer = reinterpret_cast<char*>(realloc_ptr) + header_offset();
        }

        m_capacity = capacity;
    }

    T& operator[](size_t index)
    {
        assert(index < length());
        return base_pointer()[index];
    }

    const T& operator[](size_t index) const
    {
        assert(index < length());
        return base_pointer()[index];
    }

    size_t length() const noexcept
    {
        if (m_pointer == nullptr) {
            return 0U;
        }
        char* class_header_ptr = m_pointer - header_offset();
        Header* obj_ptr = reinterpret_cast<Header*>(class_header_ptr);
        return obj_ptr->m_length;
    }

    size_t& length_mut()
    {
        char* class_header_ptr = m_pointer - header_offset();
        Header* obj_ptr = reinterpret_cast<Header*>(class_header_ptr);
        return obj_ptr->m_length;
    }

    void clear()
    {
        if constexpr (!std::is_trivially_destructible_v<T>) {
            for (size_t i = 0; i < length(); ++i) {
                reinterpret_cast<T*>(m_pointer)[i].~T();
            }
        }

        if (m_pointer != nullptr) {
            static_cast<Object*>(*this)->release();
            m_pointer = nullptr;
        }

        m_capacity = 0U;
    }

    void ensure_capacity_at_least(size_t min_capacity)
    {
        if (m_capacity < min_capacity) {
            realloc(max(min_capacity, m_capacity * 7 / 4));
        }
    }

    T* base_pointer() noexcept { return reinterpret_cast<T*>(m_pointer); }

    const T* base_pointer() const noexcept { return reinterpret_cast<T*>(m_pointer); }

    T* operator+(ptrdiff_t offset)
    {
        assert(offset >= 0 && offset < m_capacity);
        return base_pointer() + offset;
    }

    const T* operator+(ptrdiff_t offset) const
    {
        assert(offset >= 0 && offset < m_capacity);
        return base_pointer() + offset;
    }

    operator Object*() noexcept
    {
        if (m_pointer == nullptr) {
            return nullptr;
        }
        char* class_header_ptr = m_pointer - header_offset();
        Header* obj_ptr = reinterpret_cast<Header*>(class_header_ptr);
        return static_cast<Object*>(obj_ptr);
    }

private:
    char* m_pointer;
    size_t m_capacity;

    consteval static size_t get_padding() noexcept
    {
        if (sizeof(Header) % alignof(T) == 0U) {
            return 0U;
        }

        return alignof(T) - (sizeof(Header) % alignof(T));
    }

    consteval static ptrdiff_t header_offset() noexcept
    {
        return static_cast<ptrdiff_t>(sizeof(Header) + get_padding());
    }
};
