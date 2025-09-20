#!/bin/sh

set -u

./build.sh || return $?

tempjson=$(mktemp --suffix .json)
tempcpp=$(mktemp --suffix .cpp)
dist/bin/index.js "examples/$1" >"$tempjson" \
    && dist/bin/Compiler.exe "$tempjson" "$tempcpp" \
    && clang++ --std=c++20 -O1 -Idist/include -o "$1" dist/obj/*.so "$tempcpp"
result=$?
rm "$tempjson" "$tempcpp"

if [ $result -ne 0 ]; then
    return $result
fi

"./$1"
