using System;
using System.Collections.Generic;
using System.Linq;

namespace Epoch.Core.Domain
{
    /// <summary>Core Spec sec.6.1 validation gates G1-G5.</summary>
    public enum ValidationError
    {
        NONE = 0,
        ERR_INVALID_INDEX,
        ERR_AGE_LOCKED,
        ERR_UNAFFORDABLE,
        ERR_BAD_TARGET,
        ERR_LANE_FULL,
        ERR_DUPLICATE_PERK,
        ERR_KEYSTONE_ALREADY_TAKEN,
        ERR_PASS_NOT_FORCED,
    }

    /// <summary>
    /// The outcome of validating a command. A failed validation mutates nothing
    /// (Invariant SM-2). Reasons are surfaced to the UI, which keeps illegal cards
    /// visible with a concise reason (Technical Plan sec.7).
    /// </summary>
    public sealed record ValidationResult(IReadOnlyList<ValidationError> Errors)
    {
        private static readonly ValidationError[] NoErrors = Array.Empty<ValidationError>();

        public static ValidationResult Valid { get; } = new ValidationResult(NoErrors);

        public static ValidationResult Invalid(params ValidationError[] errors) =>
            new ValidationResult(errors ?? NoErrors);

        public bool IsValid => Errors.Count == 0;

        public bool Contains(ValidationError error) => Errors.Contains(error);
    }
}
