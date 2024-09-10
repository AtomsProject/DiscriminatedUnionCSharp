# Discriminated Union for CSharp
A C# Roslyn Analyzer to ensure exhaustiveness with Union Types in Switch Statements and Expressions

This analyzer ensures exhaustive pattern matching for C# `switch` statements and expressions when using union-like types defined with a custom `[Union]` attribute. The analyzer checks that all types specified in the union are properly handled in `switch` constructs, preventing missing cases that could lead to incomplete logic.

## Why this approach:
I chose this route to closely align with the union type proposal from Microsoft outlined in [Type Unions Proposal](https://github.com/dotnet/csharplang/blob/main/proposals/TypeUnions.md). This ensures that when/if native union types are added to C#, the switch to using built-in functionality will be seamless and require minimal code changes.

This implementation inspects both `SwitchStatementSyntax` and `SwitchExpressionSyntax`, ensuring that every arm or case of the switch properly handles a union type, promoting code reliability and exhaustiveness in pattern matching.

## Example Usage

``` CSharp

 [Union(typeof(A), typeof(B), typeof(C))]
 public interface IExampleUnion { }

 public class A : IExampleUnion { }
 public class B : IExampleUnion { public bool Flagged { get; set; } }
 public class C : IExampleUnion { }

 public class TestClass
 {
     public void TestSwitchExpression(IExampleUnion exampleUnion)
     {
         var result = exampleUnion switch // Will flag that C is missing
         {
             A _ => "TypeA",
             B { Flagged: true } => "TypeB Flagged",
             _ => "Unknown"
         };
     }
     
     public void TestSwitchStatement(IExampleUnion exampleUnion)
     {
         switch (exampleUnion)
         {
             case A a:
                 break;
             case B b:
                 break;
             case C c:
                 break;
         }
     }
 }
```
