namespace MoleculeLookup.Core.Enums;

/// <summary>
/// Defines the subscription tier for the application.
/// Standard version has basic search functionality.
/// Premium version includes similarity search with Tanimoto coefficient.
/// </summary>
public enum SubscriptionTier
{
    Standard,
    Premium
}
