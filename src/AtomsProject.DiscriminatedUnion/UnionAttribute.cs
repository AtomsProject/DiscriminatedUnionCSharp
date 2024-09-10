namespace AtomsProject.DiscriminatedUnion
{
    /// <summary>
    /// Marks an interface as a union type, indicating that the interface can represent one of several specific types.
    /// </summary>
    /// <remarks>
    /// This attribute is used to define a union type by specifying a list of possible types that the interface
    /// can represent. When applied to an interface, the analyzer ensures that any switch statement or expression
    /// handling the interface checks for all possible types in the union. 
    ///
    /// This implementation mimics the behavior proposed in Microsoft's C# Type Unions Proposal.
    /// If union types are natively added to C# in the future, this attribute provides an easy path to transition.
    ///
    /// Example usage:
    /// <code>
    /// [Union(typeof(A), typeof(B), typeof(C))]
    /// public interface IExampleUnion { }
    /// </code>
    /// </remarks>
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
}