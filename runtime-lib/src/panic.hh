#pragma once

#include <cstdio>
#include <cstdlib>

[[noreturn]] inline void panic(const char* message) noexcept
{
    fprintf(stderr, "%s\n", message);
    fflush(stderr);
    abort();
}
