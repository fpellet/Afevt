using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NFluent;
using Xunit;

namespace Afevt.Test
{
    public class AfevtAnalyzerShould
    {
        [Fact]
        public async Task ReturnErrorIfCreateValueTypeWithDefaultConstructor()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        public struct ValueTypeA {
            public int Value { get; }

            public ValueTypeA(int value) {
                Value = value;
            }
        }

        class TypeName
        {   
            public void MethodA(){
                var a = new ValueTypeA();
            }
        }
    }";

            var result = await Analyze(test);

            Check.That(result).HasSize(1);
            var error = result.First();
            Check.That(error.Id).IsEqualTo("Afevt");
            Check.That(error.GetMessage()).IsEqualTo("Default constructor is prohibited, because ValueTypeA has others constructors");
            Check.That(error.Severity).IsEqualTo(DiagnosticSeverity.Error);
            Check.That(error.Location.SourceSpan.Start).IsEqualTo(323);
            Check.That(error.Location.SourceSpan.End).IsEqualTo(339);
        }

        [Fact]
        public async Task ReturnNoErrorIfCreateValueTypeWithOtherConstructor()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        public struct ValueTypeA {
            public int Value { get; }

            public ValueTypeA(int value) {
                Value = value;
            }
        }

        class TypeName
        {   
            public void MethodA(){
                var a = new ValueTypeA(4);
            }
        }
    }";

            var result = await Analyze(test);

            Check.That(result).IsEmpty();
        }

        [Fact]
        public async Task ReturnNoErrorIfCreateValueTypeWithDefautConstructorButNotOtherConstructorExists()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        public struct ValueTypeA {
        }

        class TypeName
        {   
            public void MethodA(){
                var a = new ValueTypeA();
            }
        }
    }";

            var result = await Analyze(test);

            Check.That(result).IsEmpty();
        }

        [Fact]
        public async Task ReturnNoErrorIfCreateClassWithDefautConstructor()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        public class ValueTypeA {
            public int Value { get; }

            public ValueTypeA() {
            }

            public ValueTypeA(int value) {
                Value = value;
            }
        }

        class TypeName
        {   
            public void MethodA(){
                var a = new ValueTypeA();
            }
        }
    }";

            var result = await Analyze(test);

            Check.That(result).IsEmpty();
        }

        [Fact]
        public async Task ReturnNoErrorIfCreateWithDefaultConstructorButValueTypeIsInSystemNamespace()
        {
            var test = @"
    namespace System.Joe.Indien 
    {
        public struct ValueTypeA {
            public int Value { get; set; }

            public ValueTypeA(int value) {
                Value = value;
            }
        }
    }

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public void MethodA(){
                var a = new System.Joe.Indien.ValueTypeA();
            }
        }
    }";

            var result = await Analyze(test);

            Check.That(result).IsEmpty();
        }

        [Fact]
        public async Task ReturnNoErrorIfCreateWithDefaultConstructorButValueTypeIsInMicrosoftNamespace()
        {
            var test = @"
    namespace Microsoft.Joe.Indien 
    {
        public struct ValueTypeA {
            public int Value { get; set; }

            public ValueTypeA(int value) {
                Value = value;
            }
        }
    }

    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public void MethodA(){
                var a = new Microsoft.Joe.Indien.ValueTypeA();
            }
        }
    }";

            var result = await Analyze(test);

            Check.That(result).IsEmpty();
        }

        [Fact]
        public async Task NotThrowIfNotUseConstructor()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        public struct ValueTypeA {
            public int Value { get; set; }

            public ValueTypeA(int value) {
                Value = value;
            }
        }

        class TypeName
        {   
            public void MethodA(){
                var a = new ValueTypeA { Value = 5 };
            }
        }
    }";

            var result = await Analyze(test);

            Check.That(result).IsEmpty();
        }

        [Fact]
        public async Task ReturnErrorIfUseDefaultValue()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        public struct ValueTypeA {
            public int Value { get; set; }

            public ValueTypeA(int value) {
                Value = value;
            }
        }

        class TypeName
        {   
            public void MethodA(){
                var a = default(ValueTypeA);
            }
        }
    }";

            var result = await Analyze(test);

            Check.That(result.Select(r => r.Id)).ContainsExactly("Afevt");
        }

        private async Task<ICollection<Diagnostic>> Analyze(string source)
        {
            var project = CreateProject(source);
            var compilation = await project.GetCompilationAsync();
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new AfevtAnalyzer()));
            var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();

            return diagnostics.Where(d => !d.Id.StartsWith("CS")).ToArray();
        }

        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);

        private static Project CreateProject(string source, string language = LanguageNames.CSharp)
        {
            var projectName = "TestProject";
            var projectId = ProjectId.CreateNewId(projectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, projectName, projectName, language)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference);

            var fileName = "Test.cs";
            var documentId = DocumentId.CreateNewId(projectId, fileName);
            solution = solution.AddDocument(documentId, fileName, SourceText.From(source));
            return solution.GetProject(projectId);
        }
    }
}