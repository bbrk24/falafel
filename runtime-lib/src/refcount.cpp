#include "refcount.hh"
#include "panic.hh"
#include <cstddef>

static_assert(MAX_NUM_ROOTS <= PTRDIFF_MAX);

static size_t num_roots = 0U;

Object::~Object() noexcept
{
    if (m_buffered) {
        panic("Destroying buffered root");
    }
    m_destroyed = true;
}

void Object::visit_children(std::function<void(Object*)>) { }

void Object::retain() noexcept
{
    if (m_destroyed) [[unlikely]] {
        panic("Retaining zombie object");
    }

    if (m_refcount == UINTPTR_MAX) {
        return;
    }

    ++m_refcount;

    if (m_refcount == UINTPTR_MAX) [[unlikely]] {
        panic("Object refcount is too high");
    }

    if (m_color != ObjectColor::green) {
        m_color = ObjectColor::black;
    }
}

void Object::release()
{
    if (m_refcount == UINTPTR_MAX || m_destroyed) {
        return;
    }

    --m_refcount;
    if (m_refcount == 0U) {
        visit_children([](auto child) {
            if (child != nullptr) {
                child->release();
            }
        });
        m_color = ObjectColor::black;
        if (!m_buffered) {
            this->~Object();
            free(this);
            return;
        }
    } else if (m_color != ObjectColor::purple && m_color != ObjectColor::green) {
        m_color = ObjectColor::purple;
        buffer_root();
    }
}

void Object::buffer_root()
{
    if (m_refcount == UINTPTR_MAX) {
        return;
    }

    if (!m_buffered) {
        Object::roots[num_roots] = this;
        m_buffered = true;
        ++num_roots;

        if (num_roots >= MAX_NUM_ROOTS) {
            Object::collect_cycles();
        }
    }
}

void Object::mark_gray()
{
    if (m_color != ObjectColor::gray) {
        m_color = ObjectColor::gray;
        visit_children([](auto child) {
            if (child == nullptr || child->m_refcount == UINTPTR_MAX || child->m_destroyed) {
                return;
            }

            child->m_refcount--;
            if (child->m_color != ObjectColor::green) {
                child->mark_gray();
            }
        });
    }
}

void Object::scan_gray()
{
    if (m_refcount == UINTPTR_MAX) {
        return;
    } else if (m_refcount > 0U) {
        scan_black();
    } else {
        m_color = ObjectColor::white;
        visit_children([](auto child) {
            if (child != nullptr && child->m_color == ObjectColor::gray && !child->m_destroyed) {
                child->scan_gray();
            }
        });
    }
}

void Object::scan_black()
{
    m_color = ObjectColor::black;
    visit_children([](auto child) {
        if (child == nullptr || child->m_refcount == UINTPTR_MAX || child->m_destroyed) {
            return;
        }
        child->m_refcount++;
        if (child->m_color != ObjectColor::black && child->m_color != ObjectColor::green) {
            child->scan_black();
        }
    });
}

void Object::collect_white()
{
    if (m_refcount == UINTPTR_MAX) {
        return;
    }
    if (m_color == ObjectColor::white && !m_buffered) {
        m_color = ObjectColor::black;
        visit_children([](auto child) {
            if (child != nullptr && !child->m_destroyed) {
                child->collect_white();
            }
        });
        this->~Object();
        free(this);
        return;
    }
}

void Object::collect_cycles()
{
    static size_t removal_indices[MAX_NUM_ROOTS];
    ptrdiff_t removal_indices_count = 0;

    // Mark
    for (size_t i = 0U; i < num_roots; ++i) {
        auto* obj = roots[i];
        if (obj->m_color == ObjectColor::purple) {
            obj->mark_gray();
        } else {
            obj->m_buffered = false;
            removal_indices[removal_indices_count] = i;
            ++removal_indices_count;
            if (obj->m_color == ObjectColor::black && obj->m_refcount == 0U) {
                obj->~Object();
                free(obj);
            }
        }
    }

    for (ptrdiff_t i = removal_indices_count - 1; i >= 0; --i) {
        size_t index = removal_indices[i];
        if (index > num_roots) {
            continue;
        }
        if (index < num_roots) {
            roots[index] = roots[num_roots - 1];
        }
        --num_roots;
    }

    // Scan
    for (size_t i = 0U; i < num_roots; ++i) {
        auto* obj = roots[i];
        if (obj->m_color == ObjectColor::gray) {
            obj->scan_gray();
        }
    }

    // Collect
    for (size_t i = 0U; i < num_roots; ++i) {
        auto* obj = roots[i];
        obj->m_buffered = false;
        obj->collect_white();
    }
    num_roots = 0U;
}
