#pragma once

// Reference counting algorithm taken from Concurrent Cycle Collection in Reference Counted Systems
// (Bacon and Rajan, 2001), with modifications to allow for immortal objects. The actual concurrent
// part (which involves red and orange nodes) is not implemented as programs are currently always
// single-threaded.

#include <cstdint>
#include <cstdlib>
#include <functional>
#include <new>

constexpr size_t MAX_NUM_ROOTS = 1024U;

enum class ObjectColor : uint_least8_t {
    black,
    gray,
    white,
    purple,
};

struct ImmortalMarker { };

class Object {
public:
    static inline void* operator new(size_t size)
    {
        void* ptr = malloc(size);
        if (ptr == nullptr) {
            collect_cycles();
            ptr = malloc(size);
            if (ptr == nullptr) {
                throw std::bad_alloc();
            }
        }

        return ptr;
    }

    static inline void* operator new(size_t size, void* location) noexcept
    {
        return ::operator new(size, location);
    }

    /**
     * DO NOT CALL THIS DIRECTLY. This is only to be used in case of initialization failure.
     */
    static inline void operator delete(void* location) noexcept { free(location); }

    void retain() noexcept;
    void release();

    constexpr bool is_unique() const noexcept { return m_refcount < 2U; }

    static void collect_cycles();

    constexpr Object() noexcept : m_refcount(1U), m_color(ObjectColor::black), m_buffered(false) { }
    constexpr Object(ImmortalMarker) noexcept :
        m_refcount(UINTPTR_MAX), m_color(ObjectColor::black), m_buffered(false)
    {
    }
    Object(const Object&) = delete;
    virtual ~Object() = default;

protected:
    virtual void visit_children(std::function<void(Object*)> visitor);

private:
    uintptr_t m_refcount;
    ObjectColor m_color : 3;
    bool m_buffered : 1;

    static inline Object* roots[MAX_NUM_ROOTS];
    void buffer_root();

    void mark_gray();
    void scan_gray();
    void scan_black();
    void collect_white();
};

template<typename T>
class RcPointer final {
public:
    constexpr RcPointer(T* obj) : m_obj(obj) { }
    constexpr RcPointer() : m_obj(nullptr) { }

    RcPointer(const RcPointer<T>& other) : m_obj(other.m_obj)
    {
        if (m_obj != nullptr) {
            m_obj->retain();
        }
    }

    RcPointer(RcPointer<T>&& other) : m_obj(other.m_obj) { other.m_obj = nullptr; }

    template<typename U>
    explicit RcPointer(const RcPointer<U>& other) : m_obj(dynamic_cast<T*>(static_cast<U*>(other)))
    {
        if (m_obj != nullptr) {
            m_obj->retain();
        }
    }

    template<typename U>
    explicit RcPointer(RcPointer<U>&& other) : m_obj(dynamic_cast<T*>(static_cast<U*>(other)))
    {
        other.null_without_release();
    }

    ~RcPointer()
    {
        if (m_obj != nullptr) {
            m_obj->release();
            m_obj = nullptr;
        }
    }

    RcPointer<T>& operator=(const RcPointer<T>& other)
    {
        if (m_obj != nullptr) {
            m_obj->release();
        }
        m_obj = other.m_obj;
        if (m_obj != nullptr) {
            m_obj->retain();
        }
        return *this;
    }

    RcPointer<T>& operator=(RcPointer<T>&& other)
    {
        if (m_obj != nullptr) {
            m_obj->release();
        }
        m_obj = other.m_obj;
        other.m_obj = nullptr;
        return *this;
    }

    void null_without_release() noexcept { m_obj = nullptr; }

    constexpr T& operator*() const { return *m_obj; }
    constexpr T* operator->() const noexcept { return m_obj; }

    constexpr operator T*() const noexcept { return m_obj; }
    constexpr operator bool() const noexcept { return m_obj != nullptr; }

private:
    T* m_obj;
};
