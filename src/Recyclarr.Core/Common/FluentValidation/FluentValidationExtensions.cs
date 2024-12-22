using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;

namespace Recyclarr.Common.FluentValidation;

public static class FluentValidationExtensions
{
    // From: https://github.com/FluentValidation/FluentValidation/issues/1648
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IRuleBuilderOptions<T, TProperty?> SetNonNullableValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty?> ruleBuilder,
        IValidator<TProperty> validator,
        params string[] ruleSets
    )
    {
        var adapter = new ChildValidatorAdaptor<T, TProperty?>(validator!, validator.GetType())
        {
            RuleSets = ruleSets,
        };

        return ruleBuilder.SetAsyncValidator(adapter);
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IRuleBuilderOptions<T, TProperty?> SetNonNullableValidator<
        T,
        TProperty,
        TValidator
    >(
        this IRuleBuilder<T, TProperty?> ruleBuilder,
        Func<T, TValidator> validatorProvider,
        params string[] ruleSets
    )
        where TValidator : IValidator<TProperty>
    {
        var adapter = new ChildValidatorAdaptor<T, TProperty?>(
            (context, _) => validatorProvider(context.InstanceToValidate),
            typeof(TValidator)
        )
        {
            RuleSets = ruleSets,
        };

        return ruleBuilder.SetAsyncValidator(adapter);
    }

    public static IEnumerable<TSource> IsValid<TSource, TValidator>(
        this IEnumerable<TSource> source,
        TValidator validator,
        Action<IReadOnlyCollection<ValidationFailure>, TSource>? handleInvalid = null
    )
        where TValidator : IValidator<TSource>
    {
        foreach (var s in source)
        {
            var result = validator.Validate(s);
            if (result is { IsValid: true })
            {
                yield return s;
            }
            else
            {
                handleInvalid?.Invoke(result.Errors, s);
            }
        }
    }
}
