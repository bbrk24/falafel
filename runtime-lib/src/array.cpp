#include "array.hh"
#include "string.hh"
#include "stringbuilder.hh"

namespace falafel_internal {
std::unordered_map<
    TypeInfo, TypeInfo, std::hash<TypeInfo>, falafel_internal::MethodEquality<TypeInfo>,
    falafel_internal::Mallocator<std::pair<const TypeInfo, TypeInfo>>
>
    array_typeinfos;

String* const array_str = String::allocate_small_utf8(u8"Array<");

const TypeInfo& make_array_info(const TypeInfo& element_info)
{
    StringBuilder sb(3U);
    sb.add_piece(falafel_internal::array_str);
    sb.add_piece(element_info.name);
    sb.add_piece(u8'>');

    RcPointer<String> name = sb.build();
    name->retain();

    auto [iter, _]
        = falafel_internal::array_typeinfos.try_emplace(element_info, TypeInfo { .name = name });
    return iter->second;
}
}
