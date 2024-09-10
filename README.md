# Discriminated Union for C#

A C# Roslyn Analyzer to ensure exhaustiveness with Union Types in `switch` Statements and Expressions.

This analyzer guarantees exhaustive pattern matching for C# `switch` statements and expressions when using union-like types defined with a custom `[Union]` attribute. It checks that all types specified in the union are properly handled in `switch` constructs, preventing missing cases that could lead to incomplete logic or runtime errors.

## Why this approach?

I chose this approach to closely align with Microsoft's union type proposal outlined in [Type Unions Proposal](https://github.com/dotnet/csharplang/blob/main/proposals/TypeUnions.md). This ensures that if/when native union types are added to C#, migrating to the built-in functionality will be seamless and require minimal code changes.

This implementation inspects both `SwitchStatementSyntax` and `SwitchExpressionSyntax`, ensuring that every arm or case of the `switch` properly handles a union type, promoting code reliability and exhaustive pattern matching.

## Example Usage

```csharp
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
