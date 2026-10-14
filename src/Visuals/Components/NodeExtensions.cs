using System;
using System.Reflection;

using Godot;

namespace GoDough.Visuals.Components {
  public static class NodeExtensions {
    /// <summary>
    /// Binds every property and field annotated with <c>[UniqueNode]</c> / <c>[FromNode]</c> (or a
    /// derived attribute) to the node it references: <c>[UniqueNode]</c> resolves a unique-named
    /// (<c>%</c>) node, <c>[FromNode]</c> takes a verbatim NodePath (child, relative, absolute,
    /// unique or parent). When no name/path is given the member name is used, with a leading
    /// underscore stripped.
    /// <para>
    /// This is a separate pipeline from service injection (<c>[Inject]</c> / <c>WireNode</c>): it
    /// only resolves nodes in the scene tree and never touches the service container.
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
      var target = node.GetNodeOrNull(new NodePath(binding.PathExpression));

      if (target == null) {
        if (binding.IgnoreInexistent) {
          return;
        }

        throw new NodeNotFoundException(
          String.Format(
            "Node '{0}' could not be found in the current scene tree (member '{1}' on '{2}').",
            binding.PathExpression,
            binding.Member.Name,
            boundInstance.GetType()),
          binding.PathExpression);
      }

      if (!binding.ExpectedType.IsInstanceOfType(target)) {
        throw new NodeBindingException(
          binding.Member.Name,
          binding.ExpectedType,
          target.GetType(),
          boundInstance.GetType());
      }

      switch (binding.Member) {
        case PropertyInfo property:
          property.SetValue(boundInstance, target);
          break;

        case FieldInfo field:
          field.SetValue(boundInstance, target);
          break;
      }
    }
  }
}