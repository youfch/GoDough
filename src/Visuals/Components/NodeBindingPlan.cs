using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GoDough.Visuals.Attributes;

namespace GoDough.Visuals.Components {
  /// <summary>
  /// A single node-binding instruction: which member to write to, which unique-named node to look
  /// up, the member's declared type and whether a missing node is tolerated.
  /// </summary>
  internal sealed class NodeBinding {
    public NodeBinding(MemberInfo member, string nodeName, Type expectedType, bool ignoreInexistent) {
      this.Member = member;
      this.NodeName = nodeName;
      this.ExpectedType = expectedType;
      this.IgnoreInexistent = ignoreInexistent;
    }

    public MemberInfo Member { get; }
    public string NodeName { get; }
    public Type ExpectedType { get; }
    public bool IgnoreInexistent { get; }
  }

  /// <summary>
  /// Builds the node bindings for a type using reflection only (no Godot types), so the member
  /// selection and name resolution rules can be unit tested without the engine.
  /// </summary>
  internal static class NodeBindingPlan {
    private const BindingFlags MemberFlags =
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IReadOnlyList<NodeBinding> Create(Type type) {
      if (type == null) {
        throw new ArgumentNullException(nameof(type));
      }

      var bindings = new List<NodeBinding>();

      foreach (var property in type.GetProperties(MemberFlags)) {
        if (property.GetIndexParameters().Length > 0 || property.SetMethod == null) {
          continue;
        }

        var attribute = GetUniqueNodeAttribute(property);
        if (attribute == null) {
          continue;
        }

        bindings.Add(new NodeBinding(
          property,
          ResolveNodeName(attribute, property.Name),
          property.PropertyType,
          attribute.IgnoreInexistent));
      }

      foreach (var field in type.GetFields(MemberFlags)) {
        if (field.IsInitOnly || field.IsLiteral) {
          continue;
        }

        var attribute = GetUniqueNodeAttribute(field);
        if (attribute == null) {
          continue;
        }

        bindings.Add(new NodeBinding(
          field,
          ResolveNodeName(attribute, field.Name),
          field.FieldType,
          attribute.IgnoreInexistent));
      }

      return bindings;
    }

    private static string ResolveNodeName(UniqueNode attribute, string memberName) =>
      String.IsNullOrEmpty(attribute.SpecificName) ? memberName : attribute.SpecificName;

    private static UniqueNode GetUniqueNodeAttribute(MemberInfo member) =>
      member.GetCustomAttributes()
        .FirstOrDefault(attribute => typeof(UniqueNode).IsAssignableFrom(attribute.GetType()))
        as UniqueNode;
  }
}