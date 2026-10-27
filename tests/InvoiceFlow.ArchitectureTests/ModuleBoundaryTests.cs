using InvoiceFlow.SharedKernel.ValueObjects;
using NetArchTest.Rules;
using System.Reflection;

namespace InvoiceFlow.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly Dictionary<string, Assembly> Modules = new()
    {
        ["InvoiceFlow.Intake"] =
            typeof(InvoiceFlow.Intake.IntakeModule).Assembly,

        ["InvoiceFlow.Extraction"] =
            typeof(InvoiceFlow.Extraction.ExtractionModule).Assembly,

        ["InvoiceFlow.Validation"] =
            typeof(InvoiceFlow.Validation.ValidationModule).Assembly,

        ["InvoiceFlow.Approval"] =
            typeof(InvoiceFlow.Approval.ApprovalModule).Assembly,

        ["InvoiceFlow.Payments"] =
            typeof(InvoiceFlow.Payments.PaymentsModule).Assembly,

        ["InvoiceFlow.Reporting"] =
            typeof(InvoiceFlow.Reporting.ReportingModule).Assembly
    };

    private static readonly Assembly SharedKernel =
        typeof(Money).Assembly;

    public static TheoryData<string> ModuleNames => new(Modules.Keys);

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_DoesNotDependOnOtherModules(string module)
    {
        string[] others = Modules.Keys
            .Where(name => name != module)
            .ToArray();

        TestResult result = Types.InAssembly(Modules[module])
            .ShouldNot()
            .HaveDependencyOnAny(others)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{module} depends on another module: " +
            $"{string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_OnlyExposesItsModuleClass(string module)
    {
        TestResult result = Types.InAssembly(Modules[module])
            .That()
            .ArePublic()
            .Should()
            .HaveNameEndingWith("Module")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{module} exposes public types that do not end with 'Module': " +
            $"{string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void SharedKernel_DoesNotDependOnModulesOrAspNetCore()
    {
        string[] forbiddenDependencies = Modules.Keys
            .Append("Microsoft.AspNetCore")
            .ToArray();

        TestResult result = Types.InAssembly(SharedKernel)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenDependencies)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"SharedKernel has forbidden dependencies: " +
            $"{string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
