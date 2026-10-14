using System;
using System.Reflection;

using Godot;

namespace GoDough.Visuals.Components {
  public static class NodeExtensions {
    /// <summary>
    /// Binds every property and field annotated with <c>[UniqueNode]</c> (or a derived attribute) to
    /// the unique-named node it references.
    /// <para>
    /// This is a separate pipeline from service injection (<c>[Inject]</c> / <c>WireNode</c>): it
    /// only looks up unique-named (<c>%</c>) nodes in the scene tree and never touches the service
    /// container. The node name defaults to the member name and can be overridden on the attribute.
    /// </para>
    /// </summary>
    public static Node BindToViewModel(this Node node, object boundInstance = null) {
      if (node == null) {
        throw new ArgumentNullException(nameof(node));
      }

      if (boundInstance == null) {
        boundInstance = node;
      }

      try {
        foreach (var binding in NodeBindingPlan.Create(boundInstance.GetType())) {
          ApplyBinding(node, boundInstance, binding);
        }

        return node;
      } catch (Exception ex) {
        GD.PrintErr(ex.ToString());
        throw;
      }
    }

    private static void ApplyBinding(Node node, object boundInstance, NodeBinding binding) {
      var uniqueNode = node.GetNodeOrNull(new NodePath(String.Format("%{0}", binding.NodeName)));

      if (uniqueNode == null) {
        if (binding.IgnoreInexistent) {
          return;
        }

        throw new NodeNotFoundException(
          String.Format(
            "Node with name {0} could not be found in the current scene tree (member '{1}' on '{2}').",
            binding.NodeName,
            binding.Member.Name,
            boundInstance.GetType()),
          binding.NodeName);
      }

      if (!binding.ExpectedType.IsInstanceOfType(uniqueNode)) {
        throw new NodeBindingException(
          binding.Member.Name,
          binding.ExpectedType,
          uniqueNode.GetType(),
          boundInstance.GetType());
      }

      switch (binding.Member) {
        case PropertyInfo property:
          property.SetValue(boundInstance, uniqueNode);
          break;

        case FieldInfo field:
          field.SetValue(boundInstance, uniqueNode);
          break;
      }
    }
  }
}