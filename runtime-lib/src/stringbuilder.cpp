#include "stringbuilder.hh"
#include <cinttypes>

String* StringBuilder::empty_brackets = String::allocate_small_utf8(u8"[]");
String* StringBuilder::open_bracket = String::allocate_small_utf8(u8"[");
String* StringBuilder::close_bracket = String::allocate_small_utf8(u8"]");
String* StringBuilder::comma_space = String::allocate_small_utf8(u8", ");

void StringBuilder::add_piece(Int piece)
{
    String* str = String::allocate_runtime_utf8(20U);
    str->m_length = static_cast<size_t>(
        snprintf(reinterpret_cast<char*>(str->m_data.char8_ptr), 21U, "%" PRIdFAST32, piece)
    );
    m_pieces.push(str);
}

void StringBuilder::add_piece(Float piece)
{
    String* str = String::allocate_runtime_utf8(15U);
    str->m_length = static_cast<size_t>(
        snprintf(reinterpret_cast<char*>(str->m_data.char8_ptr), 16U, "%.9g", (double)piece)
    );
    m_pieces.push(str);
}

void StringBuilder::add_piece(Double piece)
{
    String* str = String::allocate_runtime_utf8(23U);
    str->m_length = static_cast<size_t>(
        snprintf(reinterpret_cast<char*>(str->m_data.char8_ptr), 24U, "%.17g", piece)
    );
    m_pieces.push(str);
}

static String* stringLiteral_true = String::allocate_small_utf8(u8"true");
static String* stringLiteral_false = String::allocate_small_utf8(u8"false");

void StringBuilder::add_piece(Bool piece)
{
    m_pieces.push(piece ? stringLiteral_true : stringLiteral_false);
}

void StringBuilder::add_piece(Char piece)
{
    String* str = new String(
        String::Flags { .is_small = true, .is_immortal = false },
        String::Data { .short_string = { static_cast<char8_t>(piece), u8'\0' } },
        1U
    );
    m_pieces.push(str);
}

RcPointer<String> StringBuilder::build()
{
    if (m_pieces.length() == 0) {
        return String::empty;
    }
    if (m_pieces.length() == 1) {
        RcPointer<String> result = m_pieces._indexget(0);
        m_pieces.clear();
        return result;
    }

    size_t length = 0U;

    for (Int i = 0; i < m_pieces.length(); ++i) {
        length += m_pieces._indexget(i)->m_length;
    }

    String* result = String::allocate_runtime_utf8(length);

    size_t offset = 0U;
    for (Int i = 0; i < m_pieces.length(); ++i) {
        auto& piece = m_pieces._indexget(i);
        const char8_t* piece_buffer = piece->m_flags.is_small ? piece->m_data.short_string
            : piece->m_flags.is_immortal                      ? piece->m_data.char8_literal
                                                              : piece->m_data.char8_ptr;

        memcpy(result->m_data.char8_ptr + offset, piece_buffer, piece->m_length);
        offset += piece->m_length;
    }

    result->m_length = length;

    m_pieces.clear();

    return result;
}
