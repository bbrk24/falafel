#!/bin/sh

set -u

: ${CXX:=clang++}
CXX="$CXX" make -rsj "$(nproc)" || return $?

tempjson=$(mktemp --suffix .json)
tempcpp=$(mktemp --suffix .cpp)
dist/bin/index.js "examples/$1" >"$tempjson" &&
    dist/bin/Compiler.exe "$tempjson" "$tempcpp" &&
    "$CXX" --std=c++20 -O1 -Idist/include -L"$(pwd)/dist/obj/" -lruntime -o "$1" "$tempcpp"
result=$?
rm "$tempjson" "$tempcpp"

if [ $result -ne 0 ]; then
    return $result
fi

if [ -n "${LD_LIBRARY_PATH+set}" ]; then
    LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:$(pwd)/dist/obj
else
    LD_LIBRARY_PATH=$(pwd)/dist/obj
fi

LD_LIBRARY_PATH=$LD_LIBRARY_PATH "./$1"
result=$?
rm "$1"
return $result
