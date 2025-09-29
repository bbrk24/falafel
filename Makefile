CXXFLAGS := -O3 -DNDEBUG $(CXXFLAGS) -std=c++20 -Wall -Wextra -Wno-sign-compare

.PHONY: all clean

all: dist/obj/libruntime.so dist/include/ dist/bin/Compiler.exe dist/bin/index.js

clean:
	rm -Rf dist/

cpp_files = $(wildcard runtime-lib/src/*.cpp)
cpp_src_headers = $(wildcard runtime-lib/src/*.hh)

dist/obj/libruntime.so: dist/obj/ $(cpp_files) $(cpp_src_headers)
	$(CXX) $(CFLAGS) $(CPPFLAGS) $(CXXFLAGS) -shared -o dist/obj/libruntime.so -fPIC $(cpp_files)

dist/include/: dist/ $(cpp_src_headers) $(wildcard runtime-lib/include/*.hh)
	cp -RL runtime-lib/include/ dist/

compiler_output_dir := compiler/Compiler/bin/Release/net8.0

dist/bin/Compiler.exe: dist/bin/
	cd compiler; dotnet build -c Release
	cp $(compiler_output_dir)/Compiler.exe $(wildcard $(compiler_output_dir)/*.dll) \
		$(compiler_output_dir)/Compiler.runtimeconfig.json dist/bin/
	chmod +x dist/bin/Compiler.exe

dist/bin/index.js: dist/bin/ parser/package-lock.json parser/build.js $(wildcard parser/src/*)
	cd parser; node build
	cp parser/dist/index.js dist/bin/
	chmod +x dist/bin/index.js

parser/package-lock.json: parser/package.json
	cd parser; npm i

dist/%/: dist/
	-mkdir $@

dist/:
	mkdir dist/
