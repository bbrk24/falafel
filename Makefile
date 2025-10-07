# MARK: Build
CXXFLAGS := $(CXXFLAGS) -std=c++20 -Wall -Wextra -Wformat-truncation=2 -Wno-sign-compare

.PHONY: build-release build-debug
common_outputs := dist/lib/libfalafel.so dist/include/ dist/bin/compiler dist/bin/parser dist/bin/falafel
build-release: $(common_outputs)
build-debug: $(common_outputs) dist/bin/parser.map dist/bin/falafel.map

build-release: dotnet_config := Release
build-release: CXXFLAGS := -O3 -g0 -DNDEBUG -flto $(CXXFLAGS)
build-debug: dotnet_config := Debug
build-debug: CXXFLAGS := -Og -g2 $(CXXFLAGS)
build-debug: FALAFEL_DEBUG := 1

cpp_files = $(wildcard runtime-lib/src/*.cpp)
cpp_src_headers = $(wildcard runtime-lib/src/*.hh)

dist/lib/libfalafel.so: dist/ $(cpp_files) $(cpp_src_headers)
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) -shared -o $@ -fPIC $(cpp_files) $(LDFLAGS)

dist/include/: dist/ $(cpp_src_headers) $(wildcard runtime-lib/include/*.hh)
	cp -RL runtime-lib/include/ dist/

dotnet_runtime := $(shell dotnet --info | grep '^ RID:' | tr -s ' ' | cut -d' ' -f3)
csharp_files = $(shell find compiler/Compiler/ -path compiler/Compiler/obj -prune -o -name '*.cs' -print)
dotnet_output_folder = compiler/Compiler/bin/$(dotnet_config)/net8.0/$(dotnet_runtime)/publish

dist/bin/compiler: dist/ compiler/Compiler.sln compiler/Compiler/Compiler.csproj $(csharp_files)
	cd compiler; dotnet publish Compiler/Compiler.csproj -c $(dotnet_config) -r $(dotnet_runtime)
	-mv $(dotnet_output_folder)/Compiler.exe $(dotnet_output_folder)/Compiler
	cp $(dotnet_output_folder)/Compiler $@
	chmod +x $@

# Technically this depends on parser/dist/index.js.map, but the same build process creates both
dist/bin/parser.map: parser/dist/index.js dist/
	cp $< $@

dist/bin/parser: parser/dist/index.js dist/
	cp $< $@
	chmod +x $@

parser/dist/index.js: parser/package-lock.json parser/build.js $(wildcard parser/src/*)
	cd parser; node build

parser/package-lock.json: parser/package.json
	cd parser; npm i

dist/bin/falafel: cli/dist/index.js dist/
	cp $< $@
	chmod +x $@

dist/bin/falafel.map: cli/dist/index.js dist/
	cp $< $@

cli/dist/index.js: cli/tsconfig.json cli/package-lock.json cli/build.civet $(wildcard cli/src/*)
	cd cli; FALAFEL_DEBUG=$(FALAFEL_DEBUG) npx civet build.civet

cli/package-lock.json: cli/package.json
	cd cli; npm i

dist/:
	mkdir -p dist/bin/ dist/lib/

# MARK: Clean
.PHONY: clean clean-outputs clean-compiler clean-parser clean-cli
clean: clean-outputs clean-compiler clean-parser clean-cli

clean-outputs:
	rm -Rf dist/

clean-compiler:
	rm -Rf $(wildcard compiler/*/bin/)

clean-parser:
	rm -Rf parser/dist/

clean-cli:
	rm -Rf cli/dist/

# MARK: Test
.PHONY: test test-runtime test-compiler test-parser test-cli
test: test-runtime test-compiler test-parser test-cli

test_csharp_files = $(shell find compiler/Compiler.Tests/ -path compiler/Compiler.Tests/obj -prune -o -name '*.cs' -print)
test-compiler: dotnet_config := Debug
test-compiler: compiler/Compiler.sln compiler/Compiler/Compiler.csproj $(csharp_files) compiler/Compiler.Tests/Compiler.Tests.csproj $(test_csharp_files)
	cd compiler; dotnet test

test-runtime: runtime-lib/test/test
	runtime-lib/test/test

test-parser: parser/package-lock.json
	cd parser; npx engine-check

test-cli: cli/package-lock.json
	cd cli; npx engine-check

runtime-lib/test/test: runtime-lib/test/test-framework.o runtime-lib/test/main.cpp $(wildcard runtime-lib/test/*.hh) $(cpp_files) $(cpp_src_headers)
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) -Og -g2 -Iruntime-lib/test/test-framework -o $@ \
		runtime-lib/test/test-framework.o runtime-lib/test/main.cpp $(cpp_files) $(LDFLAGS)

runtime-lib/test/test-framework.o: $(shell find runtime-lib/test/test-framework -name '*.cpp' -or -name '*.hh')
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) -Og -g2 -r -o $@ \
		$(shell find runtime-lib/test/test-framework -name '*.cpp') $(LDFLAGS)

# MARK: Install
.PHONY: install
install: build-release
	sudo cp -R $(wildcard dist/include/*) /usr/local/include/
	sudo cp $(wildcard dist/lib/*) /usr/local/lib/
	sudo ln -s $$(pwd)/dist/bin/falafel /usr/local/bin/falafel 
