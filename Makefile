# MARK: Build
CXXFLAGS := $(CXXFLAGS) -std=c++20 -Wall -Wextra -Wformat-truncation=2 -Wno-sign-compare

.PHONY: build-release build-debug
common_outputs := dist/obj/libruntime.so dist/include/ dist/bin/Compiler dist/bin/index.js
build-release: $(common_outputs)
build-debug: $(common_outputs) dist/bin/index.js.map

build-release: dotnet_config := Release
build-release: CXXFLAGS := $(CXXFLAGS) -O3 -g0 -DNDEBUG
build-debug: dotnet_config := Debug
build-debug: CXXFLAGS := $(CXXFLAGS) -Og -g2

cpp_files = $(wildcard runtime-lib/src/*.cpp)
cpp_src_headers = $(wildcard runtime-lib/src/*.hh)

dist/obj/libruntime.so: dist/ $(cpp_files) $(cpp_src_headers)
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) -shared -o dist/obj/libruntime.so -fPIC $(cpp_files) $(LDFLAGS)

dist/include/: dist/ $(cpp_src_headers) $(wildcard runtime-lib/include/*.hh)
	cp -RL runtime-lib/include/ dist/

dotnet_runtime := $(shell dotnet --info | grep '^ RID:' | tr -s ' ' | cut -d' ' -f3)
csharp_files = $(shell find compiler/Compiler/ -path compiler/Compiler/obj -prune -o -name '*.cs' -print)
dotnet_output_folder = compiler/Compiler/bin/$(dotnet_config)/net8.0/$(dotnet_runtime)/publish

dist/bin/Compiler: dist/ compiler/Compiler.sln compiler/Compiler/Compiler.csproj $(csharp_files)
	cd compiler; dotnet publish Compiler/Compiler.csproj -c $(dotnet_config) -r $(dotnet_runtime)
	-mv $(dotnet_output_folder)/Compiler.exe $(dotnet_output_folder)/Compiler
	cp $(dotnet_output_folder)/Compiler dist/bin/
	chmod +x dist/bin/Compiler

# Technically this depends on parser/dist/index.js.map, but the same build process creates both
dist/bin/index.js.map: parser/dist/index.js
	cp parser/dist/index.js.map dist/bin/

dist/bin/index.js: parser/dist/index.js
	cp parser/dist/index.js dist/bin/
	chmod +x dist/bin/index.js

parser/dist/index.js: dist/ parser/package-lock.json parser/build.js $(wildcard parser/src/*)
	cd parser; node build

parser/package-lock.json: parser/package.json
	cd parser; npm i

dist/:
	mkdir -p dist/bin/ dist/obj/

# MARK: Clean
.PHONY: clean clean-outputs clean-compiler clean-parser
clean: clean-outputs clean-compiler clean-parser

clean-outputs:
	rm -Rf dist/

clean-compiler:
	rm -Rf $(wildcard compiler/*/bin/)

clean-parser:
	rm -Rf parser/dist/
