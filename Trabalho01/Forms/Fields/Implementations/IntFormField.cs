using CSharpFunctionalExtensions;
using System.Linq.Expressions;

namespace ECommerceDatabase.Forms.Fields.Implementations;

internal class IntFormField<TFormData> : FormField<TFormData, int>
{
    private readonly int? _minValue;

    public IntFormField(string message, Expression<Func<TFormData, int>> property, int? minValue = null, bool optional = false) : base(message, optional, property)
    {
        _minValue = minValue;
    }

    protected override Result<int> ValidateInput(string? input)
    {
        if (string.IsNullOrEmpty(input) || !int.TryParse(input, out var result))
            return Result.Failure<int>("Entrada inválida.");

        if (_minValue != null && result < _minValue)
            return Result.Failure<int>($"O número deve ser maior que {_minValue}.");

        return Result.Success(result);
    }
}
