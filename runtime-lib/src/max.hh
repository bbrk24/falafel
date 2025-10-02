#pragma once

template<typename T>
constexpr T max(T a, T b) noexcept(noexcept(a > b))
{
    return a > b ? a : b;
}
