DEPS := \
	Microsoft.CodeAnalysis.CSharp.dll \
	Microsoft.CodeAnalysis.dll \
	System.Collections.Immutable.dll \
	Facades/netstandard.dll

REFS := $(DEPS:%=-r:%)

COMFIGURATION?=Release
MONO_PREFIX?=
ifneq ($(MONO_PREFIX),)
MCS := $(MONO_PREFIX)/bin/mcs
else
MCS := mcs
endif

release: VisFree.cs
	@mkdir -p bin/Release
	$(MCS) VisFree.cs -out:bin/Release/VisFree.exe -optimize $(REFS)

debug: Compiler.cs
	@mkdir -p bin/Release
	$(MCS) VisFree.cs -out:bin/Release/VisFree.exe -debug $(REFS)

all: release