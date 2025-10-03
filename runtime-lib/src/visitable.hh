#pragma once

#include "refcount.hh"
#include <concepts>
#include <functional>

template<typename T>
concept Visitable = requires(T x, std::function<void(Object*)> visitor) {
    { x.visit_children(visitor) } -> std::same_as<void>;
};
