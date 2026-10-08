using System;

namespace GoDough.Composition.Attributes {
  /// <summary>
  /// Marks a property or field for service injection. Applies to public and non-public members
  /// and is matched for derived attributes as well.
  /// </summary>
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
  public class InjectAttribute : Attribute {
    /// <summary>
    /// Optional key used to resolve a keyed service. When null the unkeyed service is resolved.
    /// </summary>
    public object Key { get; }

    /// <summary>
    /// Whether the service is required. When true (the default) a missing service throws;
    /// when false the member is left untouched.
    /// </summary>
    public bool Required { get; set; } = true;

    public InjectAttribute() {
    }

    public InjectAttribute(object key) =>
      (this.Key) = (key);
  }
}