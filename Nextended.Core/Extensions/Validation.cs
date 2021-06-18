using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Nextended.Core.Extensions
{
	/// <summary>
	/// Hilfsklasse für Validierung
	/// </summary>
	public static class Validation
	{
        public static IEnumerable<ValidationResult> Validate(this object obj)
		{
			var context = new ValidationContext(obj, null, null);
			var result = new List<ValidationResult>();
			Validator.TryValidateObject(obj, context, result, true);
			return result;
		}

		public static IEnumerable<ValidationResult> ValidateProperty(this object obj, string propertyName)
		{
            return Validate(obj).Where(result => result.MemberNames.Contains(propertyName));
		}

        //public static IRuleBuilderOptions<T, object> MustBeValidByDataAnnotations<T>(
        //    this IRuleBuilder<T, object> ruleBuilder)
        //{
        //    return ruleBuilder.NotNull().Must(o => !o.Validate().Any());
        //}

	}
}