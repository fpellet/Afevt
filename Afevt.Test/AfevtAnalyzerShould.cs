using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Afevt;

namespace Afevt.Test
{
    [TestClass]
    public class AfevtAnalyzerShould : CodeFixVerifier
    {
        [TestMethod]
        public void ReturnErrorIfCreateValueTypeWithDefaultConstructor()
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
            var expected = new DiagnosticResult
            {
                Id = "Afevt",
                Message = "Default constructor is prohibited, because ValueTypeA has others constructors",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 15, 25)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void ReturnNoErrorIfCreateValueTypeWithOtherConstructor()
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

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReturnNoErrorIfCreateValueTypeWithDefautConstructorButNotOtherConstructorExists()
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

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReturnNoErrorIfCreateClassWithDefautConstructor()
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

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReturnNoErrorIfCreateWithDefaultConstructorButValueTypeIsInSystemNamespace()
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

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReturnNoErrorIfCreateWithDefaultConstructorButValueTypeIsInMicrosoftNamespace()
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

            VerifyCSharpDiagnostic(test);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AfevtAnalyzer();
        }
    }
}