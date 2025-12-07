using System;

namespace Maude.TestHarness;

/// <summary>
/// Shared store for configuring the custom shake gesture predicate in the harness.
/// </summary>
internal static class ShakePredicateCoordinator
{
    private static string codeOne = string.Empty;
    private static string codeTwo = string.Empty;

    public static string CodeOne => codeOne;

    public static string CodeTwo => codeTwo;

    public static void UpdateCodeOne(string? value) => codeOne = value ?? string.Empty;

    public static void UpdateCodeTwo(string? value) => codeTwo = value ?? string.Empty;

    public static bool ShouldAllowShake
        => !string.IsNullOrWhiteSpace(codeOne)
           && string.Equals(codeOne, codeTwo, StringComparison.Ordinal);
}
