using System.ComponentModel.DataAnnotations;

namespace WorkTicketApp.Validation
{
    public class GreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public GreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var currentValue = (int)(value ?? 0);

            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                throw new ArgumentException($"Property with name {_comparisonProperty} not found");

            var comparisonValue = (int)(property.GetValue(validationContext.ObjectInstance) ?? 0);

            if (currentValue <= comparisonValue)
                return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName! });

            return ValidationResult.Success;
        }
    }
}