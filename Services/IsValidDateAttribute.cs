using System.ComponentModel.DataAnnotations;

namespace WorkTicketApp.Models.Validation
{
    public class IsValidDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Let [Required] handle null/empty if it's not allowed. For a nullable string, null or empty is valid.
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return ValidationResult.Success;
            }

            if (DateTime.TryParse(value.ToString(), out _))
            {
                return ValidationResult.Success;
            }
            return new ValidationResult(ErrorMessage ?? $"The {validationContext.DisplayName} field must be a valid date/time string.");
        }
    }
}