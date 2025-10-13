#pragma once

#include "../src/array.hh"
#include "../src/refcount.hh"
#include "../src/string.hh"
#include "../src/typeinfo.hh"
#include <test_framework.hh>

namespace {
inline String* const object_str = String::allocate_small_utf8(u8"Object");
inline String* const string_str = String::allocate_small_utf8(u8"String");
inline String* const double_str = String::allocate_small_utf8(u8"Double");
inline String* const array_of_char_str = String::allocate_immortal_utf8(u8"Array<Char>");
inline String* const array_of_string_str = String::allocate_immortal_utf8(u8"Array<String>");

inline String* const subject_str = String::allocate_immortal_utf8(
    u8"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt "
    u8"ut labore et dolore magna aliqua."
);
}

testgroup (typeinfo) {
    testcase (get_static_nongeneric) {
        auto obj_info = get_type_info<Object>();
        test_assert(obj_info.name->is_equal(object_str), "Object info should have name 'Object'");

        auto str_info = get_type_info<String>();
        test_assert(str_info.name->is_equal(string_str), "String info should have name 'String'");

        auto double_info = get_type_info<Double>();
        test_assert(
            double_info.name->is_equal(double_str),
            "Double info should have name 'Double'"
        );
    }
    , testcase (get_dynamic)
    {
        Object* obj = new Object();
        try {
            auto obj_info = obj->get_type_info_dynamic();
            test_assert(
                obj_info.name->is_equal(object_str),
                "Object info should have name 'Object'"
            );
            delete obj;
        } catch (...) {
            delete obj;
            throw;
        }

        obj = static_cast<Object*>(subject_str);
        {
            auto str_info = obj->get_type_info_dynamic();
            test_assert(
                str_info.name->is_equal(string_str),
                "String info should have name 'String'"
            );
        }
    }
    , testcase (array)
    {
        {
            auto arr_char_info = get_type_info<Array<Char>>();
            test_assert(
                arr_char_info.name->is_equal(array_of_char_str),
                "Array<Char> info should have name 'Array<Char>'"
            );
        }

        {
            auto arr_str_info = get_type_info<Array<String>>();
            test_assert(
                arr_str_info.name->is_equal(array_of_string_str),
                "Array<String> info should have name 'Array<String>'"
            );
        }
    }
};
