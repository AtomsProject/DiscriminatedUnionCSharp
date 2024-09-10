# Discriminated Union for C#

A C# Roslyn Analyzer to ensure exhaustiveness with Union Types in `switch` Statements and Expressions.

This analyzer guarantees exhaustive pattern matching for C# `switch` statements and expressions when using union-like types defined with a custom `[Union]` attribute. It checks that all types specified in the union are properly handled in `switch` constructs, preventing missing cases that could lead to incomplete logic or runtime errors.

## Getting Started

> **NOTE**: This project is currently in preview.

### The Analyzer

There are two ways to install the analyzer:

1. **Add it directly to each project:**

   ```
   dotnet add package AtomsProject.DiscriminatedUnion.Analyzer --version 0.1.0-rc1
   ```

2. **Add it globally via a `Directory.Build.Props` file:**

   This method allows the analyzer to be used globally across your entire solution. However, if you're publishing libraries for other teams, they will need to include this as well to benefit from the analyzer.

   To do this, create a `Directory.Build.Props` file in the same directory as your `.sln` (if you don’t already have one) and add the `AtomsProject.DiscriminatedUnion.Analyzer` package.

   Example:
   ```xml
   <Project>
     <ItemGroup>
       <PackageReference Include="AtomsProject.DiscriminatedUnion.Analyzer" Version="0.1.0-rc1" />
     </ItemGroup>
   </Project>
   ```

### Union Attribute

The analyzer will look for an attribute named `UnionAttribute` that takes an array of `Type[]` as its only argument.

There are two ways to define and use this attribute:

1. Use our NuGet package:
        
   Add the package to the project where the union types are defined:
   ```
   dotnet add package AtomsProject.DiscriminatedUnion.Attribute --version 0.1.0-rc1
   ```

2. Define your own attribute:
        
   If you prefer not to take an external dependency on our NuGet package, you can create your own `UnionAttribute`, as long as it follows the naming convention.

   Example:
   ```csharp
   [System.AttributeUsage(System.AttributeTargets.Interface)]
   public class UnionAttribute : System.Attribute
   {
       public System.Type[] Types { get; }
       public UnionAttribute(params System.Type[] types)
       {
           Types = types;
       }
   }
   ```

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
