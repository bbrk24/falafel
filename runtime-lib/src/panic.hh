#pragma once

#include <cstdlib>
#include <cstdio>

[[noreturn]] inline void panic(const char* message) noexcept {
    fputs(message, stderr);
    fflush(stderr);
    abort();
}
