#pragma once

#include <cstdio>
#include <cstdlib>

[[noreturn]] inline void panic(const char* message) noexcept
{
    fputs(message, stderr);
    fflush(stderr);
    abort();
}
