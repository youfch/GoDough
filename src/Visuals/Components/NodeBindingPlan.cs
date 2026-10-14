using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GoDough.Visuals.Attributes;

namespace GoDough.Visuals.Components {
  /// <summary>
  /// A single node-binding instruction: which member to write to, the NodePath expression to resolve,
  /// the member's declared type and whether a missing node is tolerated.
  /// </summary>
  internal sealed class NodeBinding {
    public NodeBinding(MemberInfo member, string pathExpression, Type expectedType, bool ignoreInexistent) {
      this.Member = member;
      this.PathExpression = pathExpression;
      this.ExpectedType = expectedType;
      this.IgnoreInexistent = ignoreInexistent;
    }

    public MemberInfo Member { get; }

    /// <summary>The NodePath expression handed to <c>GetNode</c>: "%Score", "Hud/Score", "/root/Main".</summary>
    public string PathExpression { get; }

    public Type ExpectedType { get; }
    public bool IgnoreInexistent { get; }
  }

  /// <summary>
  /// Builds the node bindings for a type using reflection only (no Godot types), so the member
  /// selection, path resolution and name derivation rules can be unit tested without the engine.
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

        var binding = CreateBinding(property, property.Name, property.PropertyType);
        if (binding != null) {
          bindings.Add(binding);
        }
      }

      foreach (var field in type.GetFields(MemberFlags)) {
        if (field.IsInitOnly || field.IsLiteral) {
          continue;
        }

        var binding = CreateBinding(field, field.Name, field.FieldType);
        if (binding != null) {
          bindings.Add(binding);
        }
      }

      return bindings;
    }

    private static NodeBinding CreateBinding(MemberInfo member, string memberName, Type memberType) {
      // [UniqueNode] resolves a unique-named node; [FromNode] takes a verbatim NodePath.
      var unique = GetAttribute<UniqueNode>(member);
      if (unique != null) {
        return new NodeBinding(
          member,
          "%" + NameOrFallback(unique.SpecificName, memberName),
          memberType,
          unique.IgnoreInexistent);
      }

      var fromNode = GetAttribute<FromNode>(member);
      if (fromNode != null) {
        return new NodeBinding(
          member,
          NameOrFallback(fromNode.Path, memberName),
          memberType,
          fromNode.IgnoreInexistent);
      }

      return null;
    }

    private static string NameOrFallback(string explicitName, string memberName) =>
      String.IsNullOrEmpty(explicitName) ? DeriveFromMemberName(memberName) : explicitName;

    /// <summary>
    /// Derives the node name/path from the member name, stripping a leading underscore (the common
    /// C# private-field convention) so that <c>_score</c> maps to <c>score</c>.
    /// </summary>
    private static string DeriveFromMemberName(string memberName) {
      var trimmed = memberName.TrimStart('_');
      return trimmed.Length == 0 ? memberName : trimmed;
    }

    private static T GetAttribute<T>(MemberInfo member) where T : Attribute =>
      member.GetCustomAttributes().OfType<T>().FirstOrDefault();
  }
}