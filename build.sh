#!/bin/sh

set -eu

config=Debug # "Debug" or "Release"

mkdir -p dist/bin dist/obj

(
    cd compiler
    dotnet build -c "$config"

    dirname=Compiler/bin/$config/net8.0/
    rm -f ../dist/bin/*.dll
    cp "$dirname/Compiler.exe" "$dirname"/*.dll ../dist/bin/
    jq -c <"$dirname/Compiler.runtimeconfig.json" >../dist/bin/Compiler.runtimeconfig.json
    chmod +x ../dist/bin/Compiler.exe
) &

{
    clang_args=
    if [ "$config" = Debug ]; then 
        clang_args='-Og -g2'
    else
        clang_args='-O3 -DNDEBUG -fno-rtti'
    fi

    # shellcheck disable=SC2086
    clang++ $clang_args -std=c++20 -WCL4 -Wnon-gcc -Wimplicit-fallthrough \
        -shared -o dist/obj/libruntime.so -fPIC runtime-lib/src/*.cpp 
} &

(
    cd parser
    node build
    cp dist/index.js ../dist/bin/
    if [ "$config" = Debug ]; then
        cp dist/index.js.map ../dist/bin
    else
        rm -f ../dist/bin/index.js.map
    fi
    chmod +x ../dist/bin/index.js
) &

rm -Rf dist/include/
cp -RL runtime-lib/include/ dist/

wait
