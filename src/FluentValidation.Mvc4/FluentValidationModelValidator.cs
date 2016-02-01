namespace FluentValidation.Mvc {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Web.Mvc;
	using Internal;
	using Results;

	/// <summary>
	/// ModelValidator implementation that uses FluentValidation.
	/// </summary>
	internal class FluentValidationModelValidator : ModelValidator {
		readonly IValidator validator;
		readonly CustomizeValidatorAttribute customizations;

		public FluentValidationModelValidator(ModelMetadata metadata, ControllerContext controllerContext, IValidator validator)
			: base(metadata, controllerContext) {
			this.validator = validator;
			
			this.customizations = CustomizeValidatorAttribute.GetFromControllerContext(controllerContext) 
				?? new CustomizeValidatorAttribute();
		}

		public override IEnumerable<ModelValidationResult> Validate(object container) {
			if (Metadata.Model != null) {
				var selector = customizations.ToValidatorSelector();
				var interceptor = customizations.GetInterceptor() ?? (validator as IValidatorInterceptor);
				var context = new ValidationContext(Metadata.Model, new PropertyChain(), selector);

				if(interceptor != null) {
					// Allow the user to provide a customized context
					// However, if they return null then just use the original context.
					context = interceptor.BeforeMvcValidation(ControllerContext, context) ?? context;
				}

				var result = validator.Validate(context);

				if(interceptor != null) {
					// allow the user to provice a custom collection of failures, which could be empty.
					// However, if they return null then use the original collection of failures. 
					result = interceptor.AfterMvcValidation(ControllerContext, context, result) ?? result;
				}

				if (!result.IsValid) {
					return ConvertValidationResultToModelValidationResults(result);
				}
			}
			return Enumerable.Empty<CustomModelValidationResult>();
		}

		protected virtual IEnumerable<CustomModelValidationResult> ConvertValidationResultToModelValidationResults(ValidationResult result) {
			return result.Errors.Select(x => new CustomModelValidationResult
            {
				MemberName = x.PropertyName,
				Message = x.ErrorMessage,
                CustomState = x.CustomState
            });
		}
	}

    public class CustomModelValidationResult : ModelValidationResult {
        public object CustomState { get; set; }
    }
}