#!/bin/sh

./build.sh || return $?

tempfile=$(mktemp --suffix .json)
dist/bin/index.js "examples/$1" >"$tempfile" && dist/bin/Compiler.exe "$tempfile"
rm "$tempfile"
