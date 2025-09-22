#pragma once

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wsign-compare"

#include "runtime/array.hh"
#include "runtime/refcount.hh"
#include "runtime/string.hh"
#include "runtime/stringbuilder.hh"
#include "runtime/typedefs.hh"

inline void print0(String* strPointer) { strPointer->print(); }

#pragma clang diagnostic pop

#include <cmath>
