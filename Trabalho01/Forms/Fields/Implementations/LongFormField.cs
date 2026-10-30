using CSharpFunctionalExtensions;
using System.Linq.Expressions;

namespace ECommerceDatabase.Forms.Fields.Implementations;

internal class LongFormField<TFormData> : FormField<TFormData, long>
{
    private readonly long? _minValue;

    public LongFormField(string message, Expression<Func<TFormData, long>> property, long? minValue = null, bool optional = false) : base(message, optional, property)
    {
        _minValue = minValue;
    }

    protected override Result<long> ValidateInput(string? input)
    {
        if (string.IsNullOrEmpty(input) || !long.TryParse(input, out var result))
            return Result.Failure<long>("Entrada inválida.");

        if (_minValue != null && result < _minValue)
            return Result.Failure<long>($"O número deve ser maior que {_minValue}.");

        return Result.Success(result);
    }
}
