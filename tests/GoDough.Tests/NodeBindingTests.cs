using System;
using System.Collections.Generic;
using System.Linq;

using GoDough.Visuals.Attributes;
using GoDough.Visuals.Components;

namespace GoDough.Tests;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class MyUniqueNodeAttribute : UniqueNode { }

internal sealed class NodeBindingTarget
{
    [UniqueNode] public object PublicProperty { get; set; } = null!;
    [UniqueNode] private object PrivateProperty { get; set; } = null!;
    [UniqueNode] public object PublicField = null!;
    [UniqueNode] private object PrivateField = null!;
    [UniqueNode("Explicit")] public object Named { get; set; } = null!;
    [MyUniqueNode] public object Derived { get; set; } = null!;
    [UniqueNode] public object GetOnly => null!;
    [UniqueNode] public readonly object ReadOnlyField = null!;
    [UniqueNode(ignoreInexistent: true)] public object Ignorable { get; set; } = null!;
    [UniqueNode] public object this[int index] => null!;

    public object ReadPrivateProperty() => PrivateProperty;
    public object ReadPrivateField() => PrivateField;
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
        Assert.DoesNotContain("Item", names); // indexer
        Assert.DoesNotContain("ReadOnlyField", names);
    }

    [Fact]
    public void Resolves_node_name_from_specific_name_or_member_name()
    {
        Assert.Equal("Explicit", For("Named").NodeName);
        Assert.Equal(nameof(NodeBindingTarget.PublicProperty), For("PublicProperty").NodeName);
        Assert.Equal("PrivateField", For("PrivateField").NodeName);
    }

    [Fact]
    public void Matches_derived_attributes()
    {
        Assert.Equal("Derived", For("Derived").NodeName);
    }

    [Fact]
    public void Propagates_ignore_inexistent()
    {
        Assert.True(For("Ignorable").IgnoreInexistent);
        Assert.False(For("PublicProperty").IgnoreInexistent);
    }

    [Fact]
    public void Captures_the_declared_member_type()
    {
        Assert.Equal(typeof(object), For("PublicField").ExpectedType);
    }

    [Fact]
    public void Null_type_throws()
    {
        Assert.Throws<ArgumentNullException>(() => NodeBindingPlan.Create(null!));
    }
}