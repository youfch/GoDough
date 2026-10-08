using System;
using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GoDough.Tests;

internal interface IWidget { }

internal interface IUnregistered { }

internal sealed class Widget : IWidget { }

internal sealed class OtherWidget : IWidget { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class MyInjectAttribute : InjectAttribute { }

internal sealed class InjectionTarget
{
    [Inject] public IWidget PublicProperty { get; set; } = null!;
    [Inject] private IWidget PrivateProperty { get; set; } = null!;
    [Inject] public IWidget PublicField = null!;
    [Inject] private IWidget PrivateField = null!;
    [MyInject] public IWidget DerivedProperty { get; set; } = null!;
    [Inject(Required = false)] public IUnregistered OptionalMissing { get; set; } = null!;
    [Inject("special")] public IWidget Keyed { get; set; } = null!;
    [Inject] public IWidget GetOnly => null!;

    public IWidget ReadPrivateProperty() => PrivateProperty;
    public IWidget ReadPrivateField() => PrivateField;
}

public class NodeInjectionTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWidget, Widget>();
        services.AddKeyedSingleton<IWidget>("special", (_, _) => new OtherWidget());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Injects_public_and_private_properties_and_fields()
    {
        var target = new InjectionTarget();

        NodeCompositionExtensions.WireMembers(target, BuildProvider());

        Assert.IsType<Widget>(target.PublicProperty);
        Assert.IsType<Widget>(target.PublicField);
        Assert.IsType<Widget>(target.ReadPrivateProperty());
        Assert.IsType<Widget>(target.ReadPrivateField());
    }

    [Fact]
    public void Injects_members_marked_with_derived_attribute()
    {
        var target = new InjectionTarget();

        NodeCompositionExtensions.WireMembers(target, BuildProvider());

        Assert.IsType<Widget>(target.DerivedProperty);
    }

    [Fact]
    public void Injects_keyed_service_by_key()
    {
        var target = new InjectionTarget();

        NodeCompositionExtensions.WireMembers(target, BuildProvider());

        Assert.IsType<OtherWidget>(target.Keyed);
    }

    [Fact]
    public void Optional_missing_service_is_skipped()
    {
        var target = new InjectionTarget();

        NodeCompositionExtensions.WireMembers(target, BuildProvider());

        Assert.Null(target.OptionalMissing);
    }

    [Fact]
    public void Required_missing_service_throws_with_member_and_type_context()
    {
        var target = new InjectionTarget();
        using var emptyProvider = new ServiceCollection().BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => NodeCompositionExtensions.WireMembers(target, emptyProvider));

        Assert.Contains(nameof(IWidget), ex.Message);
        Assert.Contains(nameof(InjectionTarget), ex.Message);
    }

    [Fact]
    public void Get_only_property_does_not_throw()
    {
        var target = new InjectionTarget();
        using var provider = BuildProvider();

        var exception = Record.Exception(
            () => NodeCompositionExtensions.WireMembers(target, provider));

        Assert.Null(exception);
    }
}
