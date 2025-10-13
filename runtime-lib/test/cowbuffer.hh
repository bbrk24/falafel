#pragma once

#include "../src/cow.hh"
#include "../src/refcount.hh"
#include <cstdint>
#include <functional>
#include <test_framework.hh>

#ifndef test_assume
#define test_assume(condition, reason) \
    do {                               \
        if (!(condition)) {            \
            test_skip(reason);         \
        }                              \
    } while (false)
#endif

namespace {
struct Counter {
    static inline int8_t count;

    inline Counter() noexcept { ++count; }
    inline Counter(const Counter&) noexcept { ++count; }
    inline Counter(Counter&&) noexcept { }

    inline ~Counter() noexcept { --count; }

    inline Counter& operator=(const Counter&) noexcept { return *this; }
    inline Counter& operator=(Counter&&) noexcept { return *this; }
};

struct VisitCounter {
    int8_t count = 0;

    void visit_children(std::function<void(Object*)> visitor)
    {
        ++count;
        visitor(nullptr);
    }
};
}

testgroup (cowbuffer) {
    testcase (frees_on_dtor) {
        Counter::count = 0;
        {
            CowBuffer<Counter> cb(1U);
            cb.length_mut() = 1U;
            test_assert(Counter::count == 0, "No instances should be created yet");
            new (cb + 0) Counter();
            test_assert(Counter::count == 1, "Should only have created one instance");
        }
        test_assert(Counter::count == 0, "Instance should be destroyed");
    }
    , testcase (does_not_forward_copies)
    {
        Counter::count = 0;
        CowBuffer<Counter> cb(2U);
        cb.length_mut() = 2U;
        new (cb + 0) Counter();
        new (cb + 1) Counter();
        test_assume(Counter::count == 2, "Should have created two instances");
        {
            CowBuffer<Counter> cb2(cb);
            test_assert(Counter::count == 2, "Should not have created additional copies");
        }
        test_assert(Counter::count == 2, "Should not have destroyed instances");
    }
    , testcase (copies_on_ensure_unique)
    {
        Counter::count = 0;
        CowBuffer<Counter> cb(1U);
        cb.length_mut() = 1U;
        new (cb + 0) Counter();
        test_assume(Counter::count == 1, "Should only have created one instance");
        {
            CowBuffer<Counter> cb2(cb);
            test_assert(Counter::count == 1, "Should not have copied yet");
            cb2.ensure_unique();
            test_assert(Counter::count == 2, "ensure_unique should copy");
        }
        test_assert(Counter::count == 1, "Copy should have been destroyed");
    }
    , testcase (realloc_works)
    {
        CowBuffer<int> cb(2U);
        test_assert(cb.capacity() == 2U, "Buffer should have declared capacity");
        test_assert(cb.length() == 0U, "Buffer should start empty");

        cb.length_mut() = 2U;
        cb[0U] = 1;
        cb[1U] = 2;

        cb.realloc(4U);

        test_assert(cb.capacity() == 4U, "realloc should increase capacity");
        test_assert(cb.length() == 2U, "realloc should not change length");
        test_assert(cb[0U] == 1 && cb[1U] == 2, "realloc should not change elements");
    }
    , testcase (realloc_does_not_copy)
    {
        Counter::count = 0;

        CowBuffer<Counter> cb(2U);
        cb.length_mut() = 2U;
        new (cb + 0) Counter();
        new (cb + 1) Counter();

        test_assume(Counter::count == 2, "Should have created two instances");

        cb.realloc(4U);

        test_assume(Counter::count == 2, "realloc should not leak copies");
    }
    , testcase (visits_struct_children)
    {
        CowBuffer<VisitCounter> cb(3U);
        cb.length_mut() = 2U;

        int8_t visit_count = 0;
        std::function<void(Object*)> visitor = [&](Object*) { ++visit_count; };

        static_cast<Object*>(cb)->visit_children(visitor);

        test_assert(visit_count == 2, "visitor should be called twice total");
        test_assert(cb[0U].count == 1, "first child should be visited once");
        test_assert(cb[1U].count == 1, "second child should be visited once");
    }
    , testcase (visits_object_children)
    {
        {
            CowBuffer<RcPointer<Object>> cb(3U);
            cb.length_mut() = 2U;

            Object* obj1 = new Object(LeafMarker {});
            Object* obj2 = new Object(LeafMarker {});

            new (cb + 0) RcPointer<Object>(obj1);
            new (cb + 1) RcPointer<Object>(obj2);

            int8_t visit_count = 0;
            std::function<void(Object*)> visitor = [&](Object* ptr) {
                test_assert(
                    ptr == obj1 || ptr == obj2,
                    "Object pointer should point at allocated object"
                );
                ++visit_count;
            };

            static_cast<Object*>(cb)->visit_children(visitor);

            test_assert(visit_count == 2, "visitor should be called twice total");
        }
        Object::collect_cycles();
    }
};
