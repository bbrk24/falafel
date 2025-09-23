#pragma once

#include "array.hh"
#include "string.hh"
#include "typedefs.hh"

struct StringBuilder final {
private:
    static String* empty_brackets;
    static String* open_bracket;
    static String* close_bracket;
    static String* comma_space;

public:
    inline StringBuilder(size_t count) : m_pieces(count) { }
    StringBuilder(const StringBuilder&) = default;
    StringBuilder(StringBuilder&&) = default;

    inline void add_piece(String* piece) { m_pieces.push(piece); }
    inline void add_piece(const RcPointer<String>& piece) { m_pieces.push(piece); }
    inline void add_piece(RcPointer<String>&& piece) { m_pieces.push(std::move(piece)); }

    void add_piece(Int piece);
    void add_piece(Float piece);
    void add_piece(Double piece);
    void add_piece(Bool piece);
    void add_piece(Char piece);

    template<typename T>
    void add_piece(Array<T> piece)
    {
        if (piece.length() == 0) {
            m_pieces.push(empty_brackets);
            return;
        }

        StringBuilder inner(static_cast<size_t>(piece.length()) * 2U + 1U);
        inner.add_piece(open_bracket);
        for (Int i = 0; i < piece.length(); ++i) {
            if (i != 0) {
                inner.add_piece(comma_space);
            }
            inner.add_piece(piece._indexget(i));
        }
        inner.add_piece(close_bracket);

        m_pieces.push(inner.build());
    }

    RcPointer<String> build();

    inline void visit_children(std::function<void(Object*)> visitor)
    {
        m_pieces.visit_children(visitor);
    }

private:
    Array<RcPointer<String>> m_pieces;
};
