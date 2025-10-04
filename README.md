Dependencies:

- Node 22.12 or newer
- .NET 8
- Clang 17 or newer or GCC 11 or newer
  - Must be either in the path as `g++`, or if in a different location, specified by the environment
    variable `CXX` (e.g. `CXX=/usr/bin/clang++`).
- lld or mold for the linker
  - If it's not the default for your compiler (e.g. you're using GCC and `/usr/bin/ld` is bfd), it
    must be pointed to by the environment variable `LDFLAGS`, e.g. `LDFLAGS="-fuse-ld=lld"`.
- make
- Any POSIX-compatible shell, with common utilities such as `grep` and `mktemp`

Compilation:

- Debug build: `make build-debug -j $(nproc)`
- Release build: `make -j $(nproc)`
- Run tests: `make test`
- Clean: `make clean`
