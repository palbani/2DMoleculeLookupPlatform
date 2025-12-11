namespace MoleculeLookup.Core.Enums;

/// <summary>
/// Reasons why a molecule validation might fail.
/// Used for error logging and user feedback.
/// </summary>
public enum ValidationFailureReason
{
    None,
    ChargeImbalance,
    InvalidBondingRules,
    InvalidStereochemistry,
    EmptyStructure,
    InvalidAtom,
    UnknownError
}
