#pragma once

#include "runtime/array.hh"
#include "runtime/optional.hh"
#include "runtime/refcount.hh"
#include "runtime/string.hh"
#include "runtime/stringbuilder.hh"
#include "runtime/typedefs.hh"

inline void print(String* strPointer) { strPointer->print(); }

#include <cmath>
