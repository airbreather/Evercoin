using System;

namespace Evercoin
{
    public sealed class ValidationResult
    {
        public static readonly ValidationResult FailingResult = new ValidationResult(false);

        public static readonly ValidationResult PassingResult = new ValidationResult(true);

        private readonly bool passed;

        private readonly string reason;

        private ValidationResult(bool passed)
            : this(passed, String.Empty)
        {
        }

        private ValidationResult(bool passed, string reason)
        {
            this.passed = passed;
            this.reason = reason;
        }

        public static implicit operator bool(ValidationResult result)
        {
            return result.Passed;
        }

        public bool Passed { get { return this.passed; } }

        public string Reason { get { return this.reason; } }

        public static ValidationResult FailWithReason(string reason)
        {
            return new ValidationResult(false, reason ?? String.Empty);
        }
    }
}
