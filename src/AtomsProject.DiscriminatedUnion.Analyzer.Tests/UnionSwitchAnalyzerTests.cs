using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using AnalyzerFix = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    AtomsProject.DiscriminatedUnion.Analyzer.UnionSwitchAnalyzer,
    AtomsProject.DiscriminatedUnion.Analyzer.UnionSwitchFixer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace AtomsProject.DiscriminatedUnion.Analyzer.Tests;

public class UnionSwitchAnalyzerTests
{
    private const string UnionAttributeCode = """
                                              [System.AttributeUsage(System.AttributeTargets.Interface)]
                                               public class UnionAttribute : System.Attribute
                                               {
                                                   /// <summary>
                                                   /// Gets the types that this union interface can represent.
                                                   /// </summary>
                                                   public System.Type[] Types { get; }
                                              
                                                   /// <summary>
                                                   /// Initializes a new instance of the <see cref="UnionAttribute"/> class with the specified types.
                                                   /// </summary>
                                                   /// <param name="types">The specific types that the union interface can represent.</param>
                                                   public UnionAttribute(params System.Type[] types)
                                                   {
                                                       Types = types;
                                                   }
                                               }
                                              """;
    [Test]
    public async Task SwitchStatement_AddMissingType_WithDeclaration()
    {
        const string testCode = """
                                [Union(typeof(A), typeof(B), typeof(C))]
                                public interface IExampleUnion { }
                                
                                public class A : IExampleUnion { }
                                public class B : IExampleUnion { public bool Flagged { get; set; } }
                                public class C : IExampleUnion { }
                                
                                public class TestClass
                                {
                                    public void TestSwitchExpression(IExampleUnion exampleUnion)
                                    {
                                       switch (exampleUnion)
                                        {
                                            case A a:
                                                break;
                                            case B b:
                                                break;
                                        }
                                    }
                                }
                                """;

        const string fixedCode = """
                                 [Union(typeof(A), typeof(B), typeof(C))]
                                 public interface IExampleUnion { }

                                 public class A : IExampleUnion { }
                                 public class B : IExampleUnion { public bool Flagged { get; set; } }
                                 public class C : IExampleUnion { }

                                 public class TestClass
                                 {
                                     public void TestSwitchExpression(IExampleUnion exampleUnion)
                                     {
                                        switch (exampleUnion)
                                         {
                                             case A a:
                                                 break;
                                             case B b:
                                                 break;
                                             case C:
                                                 break;
                                         }
                                     }
                                 }
                                 """;
        
        var test = new CSharpCodeFixTest<UnionSwitchAnalyzer, UnionSwitchFixer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { UnionAttributeCode, testCode },
                ExpectedDiagnostics =
                {
                    AnalyzerFix.Diagnostic(UnionSwitchAnalyzer.DiagnosticId)
                        .WithSpan("/0/Test1.cs", 12, 8, 12, 14)
                        .WithArguments("C")
                },
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0", 
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref", "8.0.0"), 
                    Path.Combine("ref", "net8.0"))
            }, FixedState =
            {
                Sources = { UnionAttributeCode, fixedCode }
            }
        };
        
        await test.RunAsync();
    } 
    
    [Test]
    public async Task SwitchStatement_AddMissingType()
    {
        const string testCode = """
                                [Union(typeof(A), typeof(B), typeof(C))]
                                public interface IExampleUnion { }
                                
                                public class A : IExampleUnion { }
                                public class B : IExampleUnion { public bool Flagged { get; set; } }
                                public class C : IExampleUnion { }
                                
                                public class TestClass
                                {
                                    public void TestSwitchExpression(IExampleUnion exampleUnion)
                                    {
                                       switch (exampleUnion)
                                        {
                                            case A:
                                                break;
                                            case B:
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                                """;

        const string fixedCode = """
                                 [Union(typeof(A), typeof(B), typeof(C))]
                                 public interface IExampleUnion { }

                                 public class A : IExampleUnion { }
                                 public class B : IExampleUnion { public bool Flagged { get; set; } }
                                 public class C : IExampleUnion { }

                                 public class TestClass
                                 {
                                     public void TestSwitchExpression(IExampleUnion exampleUnion)
                                     {
                                        switch (exampleUnion)
                                         {
                                             case A:
                                                 break;
                                             case B:
                                                 break;
                                             case C:
                                                 break;
                                             default:
                                                 break;
                                         }
                                     }
                                 }
                                 """;
        
        var test = new CSharpCodeFixTest<UnionSwitchAnalyzer, UnionSwitchFixer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { UnionAttributeCode, testCode },
                ExpectedDiagnostics =
                {
                    AnalyzerFix.Diagnostic(UnionSwitchAnalyzer.DiagnosticId)
                        .WithSpan("/0/Test1.cs", 12, 8, 12, 14)
                        .WithArguments("C")
                },
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0", 
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref", "8.0.0"), 
                    Path.Combine("ref", "net8.0"))
            }, FixedState =
            {
                Sources = { UnionAttributeCode, fixedCode }
            }
        };
        
        await test.RunAsync();
    }
    
    [Test]
    public async Task SwitchExpression_AddMissingType_WithFlag()
    {
        const string testCode = """
                                [Union(typeof(A), typeof(B), typeof(C))]
                                public interface IExampleUnion { }
                                
                                public class A : IExampleUnion { }
                                public class B : IExampleUnion { public bool Flagged { get; set; } }
                                public class C : IExampleUnion { }
                                
                                public class TestClass
                                {
                                    public string TestSwitchExpression(IExampleUnion exampleUnion)
                                    {
                                       return exampleUnion switch
                                       {
                                           A => "TypeA",
                                           B { Flagged: true } => "TypeB Flagged",
                                       };
                                    }
                                }
                                """;

        const string fixedCode = """
                                 [Union(typeof(A), typeof(B), typeof(C))]
                                 public interface IExampleUnion { }

                                 public class A : IExampleUnion { }
                                 public class B : IExampleUnion { public bool Flagged { get; set; } }
                                 public class C : IExampleUnion { }

                                 public class TestClass
                                 {
                                     public string TestSwitchExpression(IExampleUnion exampleUnion)
                                     {
                                        return exampleUnion switch
                                        {
                                            A => "TypeA",
                                            B { Flagged: true } => "TypeB Flagged",
                                            C => default(string)
                                        };
                                     }
                                 }
                                 """;
        
        var test = new CSharpCodeFixTest<UnionSwitchAnalyzer, UnionSwitchFixer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { UnionAttributeCode, testCode },
                ExpectedDiagnostics =
                {
                    AnalyzerFix.Diagnostic(UnionSwitchAnalyzer.DiagnosticId)
                        .WithSpan("/0/Test1.cs", 12, 28, 12, 34)
                        .WithArguments("C")
                },
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0", 
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref", "8.0.0"), 
                    Path.Combine("ref", "net8.0"))
            }, FixedState =
            {
                Sources = { UnionAttributeCode, fixedCode }
            }
        };
        
        await test.RunAsync();
    }
    
    [Test]
    public async Task SwitchExpression_AddMissingType()
    {
        const string testCode = """
                                [Union(typeof(A), typeof(B), typeof(C))]
                                public interface IExampleUnion { }
                                
                                public class A : IExampleUnion { }
                                public class B : IExampleUnion { public bool Flagged { get; set; } }
                                public class C : IExampleUnion { }
                                
                                public class TestClass
                                {
                                    public string TestSwitchExpression(IExampleUnion exampleUnion)
                                    {
                                       return exampleUnion switch
                                       {
                                           A => "TypeA",
                                           B => "TypeB",
                                           _ => "Unknown"
                                       };
                                    }
                                }
                                """;

        const string fixedCode = """
                                 [Union(typeof(A), typeof(B), typeof(C))]
                                 public interface IExampleUnion { }

                                 public class A : IExampleUnion { }
                                 public class B : IExampleUnion { public bool Flagged { get; set; } }
                                 public class C : IExampleUnion { }

                                 public class TestClass
                                 {
                                     public string TestSwitchExpression(IExampleUnion exampleUnion)
                                     {
                                        return exampleUnion switch
                                        {
                                            A => "TypeA",
                                            B => "TypeB",
                                            C => default(string),
                                            _ => "Unknown"
                                        };
                                     }
                                 }
                                 """;
        
        var test = new CSharpCodeFixTest<UnionSwitchAnalyzer, UnionSwitchFixer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { UnionAttributeCode, testCode },
                ExpectedDiagnostics =
                {
                    AnalyzerFix.Diagnostic(UnionSwitchAnalyzer.DiagnosticId)
                        .WithSpan("/0/Test1.cs", 12, 28, 12, 34)
                        .WithArguments("C")
                },
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0", 
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref", "8.0.0"), 
                    Path.Combine("ref", "net8.0"))
            }, FixedState =
            {
                Sources = { UnionAttributeCode, fixedCode }
            }
        };
        
        await test.RunAsync();
    }
    
    [Test]
    public async Task SwitchExpression_AddMissingType_WithDeclarationPattern()
    {
        const string testCode = """
                                [Union(typeof(A), typeof(B), typeof(C))]
                                public interface IExampleUnion { }
                                
                                public class A : IExampleUnion { }
                                public class B : IExampleUnion { public bool Flagged { get; set; } }
                                public class C : IExampleUnion { }
                                
                                public class TestClass
                                {
                                    public string TestSwitchExpression(IExampleUnion exampleUnion)
                                    {
                                       return exampleUnion switch
                                       {
                                           A a => "TypeA",
                                           B b => "TypeB Flagged",
                                       };
                                    }
                                }
                                """;

        const string fixedCode = """
                                 [Union(typeof(A), typeof(B), typeof(C))]
                                 public interface IExampleUnion { }

                                 public class A : IExampleUnion { }
                                 public class B : IExampleUnion { public bool Flagged { get; set; } }
                                 public class C : IExampleUnion { }

                                 public class TestClass
                                 {
                                     public string TestSwitchExpression(IExampleUnion exampleUnion)
                                     {
                                        return exampleUnion switch
                                        {
                                            A a => "TypeA",
                                            B b => "TypeB Flagged",
                                            C => default(string)
                                        };
                                     }
                                 }
                                 """;
        
        var test = new CSharpCodeFixTest<UnionSwitchAnalyzer, UnionSwitchFixer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { UnionAttributeCode, testCode },
                ExpectedDiagnostics =
                {
                    AnalyzerFix.Diagnostic(UnionSwitchAnalyzer.DiagnosticId)
                        .WithSpan("/0/Test1.cs", 12, 28, 12, 34)
                        .WithArguments("C")
                },
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net8.0", 
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref", "8.0.0"), 
                    Path.Combine("ref", "net8.0"))
            }, FixedState =
            {
                Sources = { UnionAttributeCode, fixedCode }
            }
        };
        
        await test.RunAsync();
    }
}