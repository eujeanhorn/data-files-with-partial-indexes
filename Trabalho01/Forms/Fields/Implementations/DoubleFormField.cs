using CSharpFunctionalExtensions;
using System.Linq.Expressions;

namespace ECommerceDatabase.Forms.Fields.Implementations;

internal class DoubleFormField<TFormData> : FormField<TFormData, double>
{
    private readonly double? _minValue;

    public DoubleFormField(string message, Expression<Func<TFormData, double>> property, double? minValue = null, bool optional = false) : base(message, optional, property)
    {
        _minValue = minValue;
    }

    protected override Result<double> ValidateInput(string? input)
    {
        if (string.IsNullOrEmpty(input) || !double.TryParse(input, out var result))
            return Result.Failure<double>("Entrada inválida.");

        if (_minValue != null && result < _minValue)
            return Result.Failure<double>($"O número deve ser maior que {_minValue}.");

        return Result.Success(result);
    }
}
