using MyBGList.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyBGList.DTO
{
	public class RequestDTO<T> : IValidatableObject
	{
		[DefaultValue(0)]
		public int PageIndex { get; set; } = 0;
		[Range(0, 100)]
		[DefaultValue(0)]
		public int PageSize { get; set; } = 0;

		[DefaultValue("ID")]
		public string? SortColumn { get; set; } = "ID";
		[SortOrderValidator]
		[DefaultValue("ASC")]
		public string? SortOrder { get; set; } = "ASC";
		[DefaultValue(null)]
		public string? FilterQuery { get; set; } = null;

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			var validator = new SortColumnValidatorAttribute(typeof(T));

			var result = validator.GetValidationResult(SortColumn, validationContext);

			return (result != null) ? new[] { result } : new ValidationResult[0];
		}
	}
}
