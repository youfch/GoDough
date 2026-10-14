using System;

namespace GoDough.Visuals.Attributes {
  /// <summary>
  /// Binds a member to the node addressed by a Godot NodePath expression, resolved verbatim from the
  /// node the binding runs on: a plain child (<c>"Title"</c>), a relative path (<c>"Hud/Score"</c>),
  /// an absolute path (<c>"/root/Main/Hud"</c>), a unique name (<c>"%Score"</c>) or a parent step
  /// (<c>".."</c>). When no path is given the member name is used (a leading underscore is stripped).
  /// </summary>
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
  public class FromNode : Attribute {
    public FromNode(string? path = null, bool ignoreInexistent = false) {
      this.Path = path;
      this.IgnoreInexistent = ignoreInexistent;
    }

    /// <summary>The NodePath expression. When <c>null</c> the member name is used.</summary>
    public string? Path { get; }

    /// <summary>When true a missing node is skipped instead of throwing.</summary>
    public bool IgnoreInexistent { get; private set; }
  }
}