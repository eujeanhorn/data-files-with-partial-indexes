using CSharpFunctionalExtensions;
using System.Linq.Expressions;

namespace ECommerceDatabase.Forms.Fields.Implementations;

internal sealed class StringFormField<TFormData> : FormField<TFormData, string>
{
    private readonly int _minLength;
    private readonly int _maxLength;

    public StringFormField(string message, Expression<Func<TFormData, string>> property, int minLength = 0, int maxLength = int.MaxValue, bool optional = false) : base(message, optional, property)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    protected override Result<string> ValidateInput(string? input)
    {
        input ??= string.Empty;

        if (input.Length < _minLength)
            return Result.Failure<string>($"Deve ser informada uma string de no mínimo {_minLength} caracteres.");

        if (input.Length > _maxLength)
            return Result.Failure<string>($"A entrada excedeu o máximo de caracteres permitido ({_maxLength} caracteres).");

        return Result.Success(input);
    }
}
