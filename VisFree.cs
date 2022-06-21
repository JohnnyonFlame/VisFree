using System;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;

namespace Compiler
{
    public class Program
    {
        static IEnumerable<string> EnumerateAllSources(string[] args, int firstRef)
        {
            for (int i = 1; i < firstRef; i++)
                yield return args[i];
        }

        static IEnumerable<SyntaxTree> ParseAllSources(string[] args, int firstRef)
        {
            foreach (var source in EnumerateAllSources(args, firstRef))
            {
                string code = File.ReadAllText(source);
                yield return CSharpSyntaxTree.ParseText(code, encoding: Encoding.UTF8).WithFilePath(source);
            }
        } 

        static IEnumerable<EmbeddedText> GetAllEmbeddedTexts(string[] args, int firstRef)
        {
            foreach (var source in EnumerateAllSources(args, firstRef))
            {
                string code = File.ReadAllText(source);
                var buffer = Encoding.UTF8.GetBytes(code);
                var sourceText = SourceText.From(buffer, buffer.Length, Encoding.UTF8, canBeEmbedded: true);
                yield return EmbeddedText.FromSource(source, sourceText);
            }
        } 

        public static string PathToAssembly(string p)
        {
            try {
                return Assembly.Load(new AssemblyName(p)).Location;
            } catch (Exception) {}
            return p;
        }

        public static int Main(string[] args)
        {
            if (args.Count() < 2)
            {
                Console.WriteLine("Usage: <output.dll/exe> <source1.cs> [source#n.cs...] [-- ref#n.dll...]");
                return 1;
            }

            int firstRef;
            for (firstRef = 2; firstRef < args.Length; firstRef++)
            {
                if (args[firstRef] == "--")
                    break;
            }

            // Load all the referenced assemblies
            List<MetadataReference> metadataReferences = new List<MetadataReference>();
            metadataReferences.AddRange(
                args.Skip(firstRef+1).Select(
                    x => MetadataReference.CreateFromFile(PathToAssembly(x))
                )
            );

            // Are we saving a library or application?
            bool isApplication = Path.GetExtension(args[0]).ToLower() == ".exe";
            var outputKind = isApplication ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;

            // Compilation options
            var compilationOptions = new CSharpCompilationOptions(outputKind)
                .WithMetadataImportOptions(MetadataImportOptions.All)
                .WithAllowUnsafe(true) // Necessary to use protected methods and fields
                .WithOptimizationLevel(OptimizationLevel.Release);

            var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions)
                .GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            topLevelBinderFlagsProperty.SetValue(compilationOptions, (uint)1 << 22);

            // Create compiler
            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(args[0]),
                ParseAllSources(args, firstRef),
                metadataReferences,
                compilationOptions);

            // Start compilation
            string finalAssemblyFilePath = args[0];
            string finalAssemblyPdbFilePath = Path.ChangeExtension(args[0], ".pdb");
            Console.WriteLine($"Saving assembly to {finalAssemblyFilePath}...");

            try
            {
                MemoryStream asmStream = new MemoryStream();
                MemoryStream pdbStream = new MemoryStream();
                var emitOptions = new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.PortablePdb,
                    pdbFilePath: finalAssemblyPdbFilePath);

                var embeddedTexts = GetAllEmbeddedTexts(args, firstRef).ToList();
                var compileResults = compilation.Emit(                                                                                        
                    peStream: asmStream,
                    pdbStream: pdbStream,
                    xmlDocumentationStream: null,
                    embeddedTexts: embeddedTexts,
                    options: emitOptions);
                foreach (var diag in compileResults.Diagnostics)
                {
                    if (diag.ToString().Contains("Assuming assembly reference 'mscorlib"))
                        continue;
                    Console.WriteLine(diag.ToString());
                }

                if (!compileResults.Success)
                {
                    throw new Exception("Failed to compile assembly.");
                }
                else
                {
                    using (FileStream asmStreamOut = File.Open(finalAssemblyFilePath, FileMode.Create))
                    using (FileStream pdbStreamOut = File.Open(finalAssemblyPdbFilePath, FileMode.Create))
                    {
                        asmStream.CopyTo(asmStreamOut);
                        pdbStream.CopyTo(pdbStreamOut);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.ToString());
            }

            return 0;
        }
    }
}
