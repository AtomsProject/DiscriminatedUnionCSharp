// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace AtomsProject.DiscriminatedUnion.Analyzer.Sample;

// If you don't see warnings, build the Analyzers Project.

public class Examples
{
    [Union(typeof(A), typeof(B), typeof(C))]
    public interface IExampleUnion
    {
    }

    public class A : IExampleUnion
    {
    }

    public class B : IExampleUnion
    {
        public bool Flagged { get; set; }
    }

    public class C : IExampleUnion
    {
    }

    public class TestClass
    {
        public void TestSwitchExpression_WithPropertyPatternClause(IExampleUnion exampleUnion)
        {
            var result = exampleUnion switch // Will flag that C is missing
            {
                A a => "TypeA",
                B { Flagged: true } b => "TypeB Flagged",
                _ => "Unknown",
            };
        }

        public void TestSwitchExpression_WithDeclarationPattern(IExampleUnion exampleUnion)
        {
            var result = exampleUnion switch // Will flag that C is missing
            {
                A a => "TypeA",
                B b => "TypeB Flagged",
                _ => "Unknown",
            };
        }
       public void TestSwitchExpression_WithConstraintPattern(IExampleUnion exampleUnion)
        {
            var result = exampleUnion switch // Will flag that C is missing
            {
                A => "TypeA",
                B => "TypeB Flagged",
                _ => "Unknown",
            };
        }

        public void TestSwitchStatement_CasePatternSwitchLabel(IExampleUnion exampleUnion)
        {
            switch (exampleUnion)
            {
                case A a:
                    break;
                case B b:
                    break;
            }
        }
        
        public void TestSwitchStatement_CaseSwitchLabel(IExampleUnion exampleUnion)
        {
            switch (exampleUnion)
            {
                case A:
                    break;
                case B:
                    break;
            }
        } 
        public void TestSwitchStatement_RecursivePattern(IExampleUnion exampleUnion)
        {
            switch (exampleUnion)
            {
                case A:
                    break;
                case B {Flagged: true}:
                    break;
            }
        }
    }
}