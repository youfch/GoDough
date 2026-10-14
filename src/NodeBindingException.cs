using System;

namespace GoDough {
  /// <summary>
  /// Thrown when a member annotated for node binding resolves to a node whose runtime type is not
  /// assignable to the member's declared type.
  /// </summary>
  public class NodeBindingException : Exception {
    public string MemberName { get; }
    public Type ExpectedType { get; }
    public Type ActualType { get; }
    public Type OwnerType { get; }

    public NodeBindingException(string memberName, Type expectedType, Type actualType, Type ownerType)
      : base(String.Format(
          "Cannot bind member '{0}' on '{1}': expected a node of type '{2}', but the resolved node is of type '{3}'.",
          memberName,
          ownerType,
          expectedType,
          actualType)) {
      this.MemberName = memberName;
      this.ExpectedType = expectedType;
      this.ActualType = actualType;
      this.OwnerType = ownerType;
    }
  }
}