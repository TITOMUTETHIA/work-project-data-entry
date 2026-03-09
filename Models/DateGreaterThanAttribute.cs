using System.ComponentModel.DataAnnotations;

namespace WorkTicketApp.Models.Validation
{
    /// <summary>
    /// Validates that the decorated date string property is on or after the date string property specified.
    /// </summary>
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var currentValue = value as string;

            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                throw new ArgumentException($"Property with name {_comparisonProperty} not found.");

            var comparisonValue = property.GetValue(validationContext.ObjectInstance) as string;

            // If either value is not a valid date, let other validators handle it.
            if (!DateTime.TryParse(currentValue, out var currentDateTime) || !DateTime.TryParse(comparisonValue, out var comparisonDateTime))
                return ValidationResult.Success;

            if (currentDateTime < comparisonDateTime)
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be on or after {_comparisonProperty}.");

            return ValidationResult.Success;
        }
    }
}