using System;
using System.Linq;
using System.Reflection;

using Godot;
using GoDough.Composition.Attributes;
using GoDough.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace GoDough.Composition.Extensions {
  public static class NodeCompositionExtensions {
    private const BindingFlags MemberFlags =
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Injects all properties and fields marked with <see cref="InjectAttribute"/> (or a derived
    /// attribute) using the service provider of the current <see cref="AppHost"/>.
    /// </summary>
    public static T WireNode<T>(this T node)
      where T : Node {
      if (node == null) {
        throw new ArgumentNullException(nameof(node));
      }

      try {
        WireMembers(node, AppHost.Instance.Application.Services);
        return node;
      } catch (Exception ex) {
        GD.PrintErr(ex.ToString());
        throw;
      }
    }

    /// <summary>
    /// Injects every property and field marked with <see cref="InjectAttribute"/> (or a derived
    /// attribute) using the supplied service provider. Public and non-public members are scanned.
    /// Members that cannot be written (indexers, get-only properties, readonly fields) are skipped.
    /// </summary>
    public static void WireMembers(object target, IServiceProvider services) {
      if (target == null) {
        throw new ArgumentNullException(nameof(target));
      }

      if (services == null) {
        throw new ArgumentNullException(nameof(services));
      }

      var type = target.GetType();

      foreach (var property in type.GetProperties(MemberFlags)) {
        if (property.GetIndexParameters().Length > 0 || property.SetMethod == null) {
          continue;
        }

        var attribute = GetInjectAttribute(property);
        if (attribute == null) {
          continue;
        }

        if (TryResolve(services, property.PropertyType, attribute, type, property.Name, out var value)) {
          property.SetValue(target, value);
        }
      }

      foreach (var field in type.GetFields(MemberFlags)) {
        if (field.IsInitOnly || field.IsLiteral) {
          continue;
        }

        var attribute = GetInjectAttribute(field);
        if (attribute == null) {
          continue;
        }

        if (TryResolve(services, field.FieldType, attribute, type, field.Name, out var value)) {
          field.SetValue(target, value);
        }
      }
    }

    private static InjectAttribute GetInjectAttribute(MemberInfo member) =>
      member.GetCustomAttributes()
        .FirstOrDefault(attribute => typeof(InjectAttribute).IsAssignableFrom(attribute.GetType()))
        as InjectAttribute;

    private static bool TryResolve(
      IServiceProvider services,
      Type serviceType,
      InjectAttribute attribute,
      Type ownerType,
      string memberName,
      out object value) {

      try {
        if (attribute.Key != null) {
          value = attribute.Required
            ? services.GetRequiredKeyedService(serviceType, attribute.Key)
            : ResolveOptionalKeyed(services, serviceType, attribute.Key);
        } else {
          value = attribute.Required
            ? services.GetRequiredService(serviceType)
            : services.GetService(serviceType);
        }
      } catch (Exception ex) {
        throw new InvalidOperationException(
          String.Format(
            "Unable to resolve required service '{0}' for member '{1}' on '{2}'.",
            serviceType,
            memberName,
            ownerType),
          ex);
      }

      return value != null;
    }

    private static object ResolveOptionalKeyed(IServiceProvider services, Type serviceType, object key) {
      try {
        return services.GetRequiredKeyedService(serviceType, key);
      } catch (InvalidOperationException) {
        return null;
      }
    }
  }
}