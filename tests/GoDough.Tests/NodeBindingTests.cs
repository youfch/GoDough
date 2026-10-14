using System;
using System.Collections.Generic;
using System.Linq;

using Godot;

using GoDough.Visuals.Attributes;
using GoDough.Visuals.Components;

namespace GoDough.Tests;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class MyUniqueNodeAttribute : UniqueNode { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class MyFromNodeAttribute : FromNode { }

internal sealed class NodeBindingTarget
{
    // --- [UniqueNode]: unique-named (%) resolution ---
    [UniqueNode] public object PublicProperty { get; set; } = null!;
    [UniqueNode] private object PrivateProperty { get; set; } = null!;
    [UniqueNode] public object PublicField = null!;
    [UniqueNode] private object PrivateField = null!;
    [UniqueNode("Explicit")] public object Named { get; set; } = null!;
    [UniqueNode] private object _underscored = null!;
    [UniqueNode] private object _PrivatePropUnderscore { get; set; } = null!;
    [UniqueNode("")] public object EmptyUniqueName { get; set; } = null!;
    [MyUniqueNode] public object Derived { get; set; } = null!;
    [UniqueNode] public object GetOnly => null!;
    [UniqueNode] public readonly object ReadOnlyField = null!;
    [UniqueNode(ignoreInexistent: true)] public object Ignorable { get; set; } = null!;
    [UniqueNode] public object this[int index] => null!;

    // --- [FromNode]: verbatim NodePath resolution ---
    [FromNode] public object Child { get; set; } = null!;
    [FromNode("Hud/Score")] public object Relative { get; set; } = null!;
    [FromNode("/root/Main/Hud")] public object Absolute { get; set; } = null!;
    [FromNode("%Score")] public object ExplicitUnique { get; set; } = null!;
    [FromNode("..")] public object Parent { get; set; } = null!;
    [FromNode("")] public object EmptyPath { get; set; } = null!;
    [FromNode] private object _derivedFromField = null!;
    [FromNode] private object __doubleUnderscore = null!;
    [MyFromNode] public object DerivedFromNode { get; set; } = null!;
    [FromNode(ignoreInexistent: true)] public object OptionalChild { get; set; } = null!;
    [FromNode] public object FromNodeGetOnly => null!;

    public object ReadPrivateProperty() => PrivateProperty;
    public object ReadPrivateField() => PrivateField;
    public object ReadUnderscored() => _underscored;
    public object ReadPrivatePropUnderscore() => _PrivatePropUnderscore;
    public object ReadDerivedFromField() => _derivedFromField;
    public object ReadDoubleUnderscore() => __doubleUnderscore;
}

public class NodeBindingTests
{
    private static IReadOnlyList<NodeBinding> Plan() => NodeBindingPlan.Create(typeof(NodeBindingTarget));

    private static NodeBinding For(string memberName) =>
        Plan().Single(binding => binding.Member.Name == memberName);

    [Fact]
    public void Includes_public_and_private_properties_and_fields()
    {
        var names = Plan().Select(binding => binding.Member.Name).ToList();

        Assert.Contains(nameof(NodeBindingTarget.PublicProperty), names);
        Assert.Contains("PrivateProperty", names);
        Assert.Contains(nameof(NodeBindingTarget.PublicField), names);
        Assert.Contains("PrivateField", names);
    }

    [Fact]
    public void Excludes_get_only_properties_indexers_and_readonly_fields()
    {
        var names = Plan().Select(binding => binding.Member.Name).ToList();

        Assert.DoesNotContain("GetOnly", names);
        Assert.DoesNotContain("FromNodeGetOnly", names);
        Assert.DoesNotContain("Item", names); // indexer
        Assert.DoesNotContain("ReadOnlyField", names);
    }

    [Fact]
    public void Unique_node_binding_uses_a_unique_name_expression()
    {
        Assert.Equal("%PublicProperty", For("PublicProperty").PathExpression);
        Assert.Equal("%Explicit", For("Named").PathExpression);
        Assert.Equal("%PublicField", For("PublicField").PathExpression);
    }

    [Fact]
    public void From_node_binding_uses_the_path_verbatim()
    {
        Assert.Equal("Child", For("Child").PathExpression);
        Assert.Equal("Hud/Score", For("Relative").PathExpression);
        Assert.Equal("/root/Main/Hud", For("Absolute").PathExpression);
        Assert.Equal("%Score", For("ExplicitUnique").PathExpression);
        Assert.Equal("..", For("Parent").PathExpression);
    }

    [Fact]
    public void Member_name_fallback_strips_a_leading_underscore()
    {
        Assert.Equal("%underscored", For("_underscored").PathExpression);
        Assert.Equal("%PrivatePropUnderscore", For("_PrivatePropUnderscore").PathExpression);
        Assert.Equal("derivedFromField", For("_derivedFromField").PathExpression);
        Assert.Equal("doubleUnderscore", For("__doubleUnderscore").PathExpression);
    }

    [Fact]
    public void Empty_or_missing_name_falls_back_to_the_member_name()
    {
        Assert.Equal("%EmptyUniqueName", For("EmptyUniqueName").PathExpression);
        Assert.Equal("EmptyPath", For("EmptyPath").PathExpression);
    }

    [Fact]
    public void Matches_derived_attributes()
    {
        Assert.Equal("%Derived", For("Derived").PathExpression);
        Assert.Equal("DerivedFromNode", For("DerivedFromNode").PathExpression);
    }

    [Fact]
    public void Propagates_ignore_inexistent()
    {
        Assert.True(For("Ignorable").IgnoreInexistent);
        Assert.True(For("OptionalChild").IgnoreInexistent);
        Assert.False(For("PublicProperty").IgnoreInexistent);
    }

    [Fact]
    public void Captures_the_declared_member_type()
    {
        Assert.Equal(typeof(object), For("PublicField").ExpectedType);
    }

    [Fact]
    public void Type_without_annotations_yields_no_bindings()
    {
        Assert.Empty(NodeBindingPlan.Create(typeof(object)));
    }

    [Fact]
    public void Null_type_throws()
    {
        Assert.Throws<ArgumentNullException>(() => NodeBindingPlan.Create(null!));
    }

    [Fact]
    public void Node_binding_exception_reports_member_expected_and_actual()
    {
        var exception = new NodeBindingException("Score", typeof(Label), typeof(Button), typeof(NodeBindingTarget));

        Assert.Contains("Score", exception.Message);
        Assert.Contains(nameof(Label), exception.Message);
        Assert.Contains(nameof(Button), exception.Message);
        Assert.Equal("Score", exception.MemberName);
        Assert.Equal(typeof(Label), exception.ExpectedType);
        Assert.Equal(typeof(Button), exception.ActualType);
        Assert.Equal(typeof(NodeBindingTarget), exception.OwnerType);
    }

    [Fact]
    public void Node_not_found_exception_carries_the_path()
    {
        var exception = new NodeNotFoundException("Node 'Info/Title' could not be found.", "Info/Title");

        Assert.Equal("Info/Title", exception.NodeName);
    }
}