﻿using System.ComponentModel.DataAnnotations;

namespace MyBGList.Attributes
{
	public class SortColumnValidatorAttribute : ValidationAttribute
	{
		public Type EntityType { get; set; }
		public SortColumnValidatorAttribute(Type entityType) : base("Value must match an existing column")
		{
			this.EntityType = entityType;
		}

		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			var strValue = value as string;
			if (!string.IsNullOrEmpty(strValue) && EntityType != null && EntityType.GetProperties().Any(p => p.Name == strValue))
			{
				return ValidationResult.Success;
			}
			return new ValidationResult(ErrorMessage);
		}
	}
}
