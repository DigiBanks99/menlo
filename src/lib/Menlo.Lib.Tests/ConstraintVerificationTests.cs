using System.Reflection;
using Shouldly;

namespace Menlo.Lib.Tests;

/// <summary>
/// Tests to verify that domain abstractions have no prohibited dependencies.
/// TC-07: No ClaimsPrincipal in Domain
/// </summary>
public sealed class ConstraintVerificationTests
{
    [Fact]
    public void GivenMenloLibAssembly_WhenSearchingForClaimsPrincipal()
    {
        // Arrange
        Assembly menloLibAssembly = Assembly.Load("Menlo.Lib");

        // Act
        Type[] allTypes = menloLibAssembly.GetTypes();
        List<string> violatingTypes = new();

        foreach (Type type in allTypes)
        {
            // Check if type inherits from or references ClaimsPrincipal
            if (type.FullName?.Contains("ClaimsPrincipal") == true)
            {
                violatingTypes.Add(type.FullName);
            }

            // Check properties
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.PropertyType.FullName?.Contains("ClaimsPrincipal") == true)
                {
                    violatingTypes.Add($"{type.FullName}.{property.Name}");
                }
            }

            // Check method parameters and return types
            foreach (MethodInfo method in type.GetMethods())
            {
                if (method.ReturnType.FullName?.Contains("ClaimsPrincipal") == true)
                {
                    violatingTypes.Add($"{type.FullName}.{method.Name} (return type)");
                }

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    if (parameter.ParameterType.FullName?.Contains("ClaimsPrincipal") == true)
                    {
                        violatingTypes.Add($"{type.FullName}.{method.Name} (parameter {parameter.Name})");
                    }
                }
            }
        }

        // Assert
        ItShouldNotContainClaimsPrincipalReferences(violatingTypes);
    }

    private static void ItShouldNotContainClaimsPrincipalReferences(List<string> violatingTypes)
    {
        violatingTypes.ShouldBeEmpty($"Found ClaimsPrincipal references: {string.Join(", ", violatingTypes)}");
    }

    [Fact]
    public void GivenMenloLibAssembly_WhenSearchingForSecurityClaimsNamespace()
    {
        // Arrange
        Assembly menloLibAssembly = Assembly.Load("Menlo.Lib");
        AssemblyName[] referencedAssemblies = menloLibAssembly.GetReferencedAssemblies();

        // Act
        bool hasSecurityClaimsReference = referencedAssemblies
            .Any(asm => asm.Name?.Contains("System.Security.Claims") == true);

        // Assert
        ItShouldNotReferenceSecurityClaims(hasSecurityClaimsReference);
    }

    private static void ItShouldNotReferenceSecurityClaims(bool hasSecurityClaimsReference)
    {
        hasSecurityClaimsReference.ShouldBeFalse("Menlo.Lib should not reference System.Security.Claims");
    }

    [Fact]
    public void GivenMenloLibAssembly_WhenSearchingForAspNetCore()
    {
        // Arrange
        Assembly menloLibAssembly = Assembly.Load("Menlo.Lib");
        AssemblyName[] referencedAssemblies = menloLibAssembly.GetReferencedAssemblies();

        // Act
        bool hasAspNetCoreReference = referencedAssemblies
            .Any(asm => asm.Name?.Contains("Microsoft.AspNetCore") == true);

        // Assert
        ItShouldNotReferenceAspNetCore(hasAspNetCoreReference);
    }

    private static void ItShouldNotReferenceAspNetCore(bool hasAspNetCoreReference)
    {
        hasAspNetCoreReference.ShouldBeFalse("Menlo.Lib should not reference ASP.NET Core");
    }

    [Fact]
    public void GivenMenloLibAssembly_WhenSearchingForEntityFramework()
    {
        // Arrange
        Assembly menloLibAssembly = Assembly.Load("Menlo.Lib");
        AssemblyName[] referencedAssemblies = menloLibAssembly.GetReferencedAssemblies();

        // Act
        bool hasEfCoreReference = referencedAssemblies
            .Any(asm => asm.Name?.Contains("EntityFrameworkCore") == true);

        // Assert
        ItShouldNotReferenceEntityFramework(hasEfCoreReference);
    }

    private static void ItShouldNotReferenceEntityFramework(bool hasEfCoreReference)
    {
        hasEfCoreReference.ShouldBeFalse("Menlo.Lib should not reference Entity Framework Core");
    }
}
