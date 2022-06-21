### VisFree
-------------
VisFree is a simple utility to compile C# assemblies that completely ignore access modifiers, example usage cases would be:
- Reusing internal code from different assemblies, such as texture loading from FNA.
- Building game mods with MonoMod, Harmony and other frameworks.
- Building test suites on top of production-ready deployments. 

### Usage:
-------------
```bash
VisFree.exe <output.dll/exe> <source1.cs> [source#n.cs...] [-- ref#n.dll...]
VisFree.exe GamePatch.dll Patch01.cs Patch02.cs -- Game.exe Engine.dll
VisFree.exe MyTool.exe Main.cs Misc.cs -- Refs.dll
```

### Building (Mono):
-------------
- If you want to compile with system mono:
```bash
make
```

You can optionally specify the build type, such as `release` or `debug`, as make targets, and use the `MONO_PREFIX` variable to change where your mono install is located:

```bash
make debug MONO_PREFIX=~/mono-6.12.0.122/built
```

### Building (dotnet)
-------------
```bash
dotnet restore
dotnet build
```

### LICENSE:
-------------

The files in this repository are free software licensed under MIT, read the [License File](LICENSE) for reference.

### Special Thanks:
-------------
Filip W. for [No InternalsVisibleTo, no problem – bypassing C# visibility rules with Roslyn](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/).