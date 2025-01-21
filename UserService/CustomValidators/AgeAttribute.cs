using System.ComponentModel.DataAnnotations;

namespace UserService.CustomValidators
{
    public class AgeAttribute : ValidationAttribute
    {
        public AgeAttribute()
        {

        }
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null || value is not DateTime date) return new ValidationResult("Property age is null");
            if ((DateTime.Now - date).TotalDays < 8 * 365.25) return new ValidationResult("Your age is too young");
            return ValidationResult.Success;
        }
    }
}