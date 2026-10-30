using CSharpFunctionalExtensions;
using System.Globalization;
using System.Linq.Expressions;

namespace ECommerceDatabase.Forms.Fields.Implementations;

public sealed class DateTimeFormField<TFormData> : FormField<TFormData, DateTime>
{
    private readonly DateTime? _minValue;
    protected override string? FieldFormat => DefaultFormat;
    private const string DefaultFormat = "dd/MM/yyyy HH:mm:ss";

    public DateTimeFormField(string message, Expression<Func<TFormData, DateTime>> property, DateTime? minValue = null, bool optional = false) : base(message, optional, property)
    {
        _minValue = minValue;
    }

    protected override Result<DateTime> ValidateInput(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return Result.Failure<DateTime>("Entrada inválida.");

        if (!DateTime.TryParseExact(input, DefaultFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return Result.Failure<DateTime>("Entrada inválida.");

        if (_minValue != null && result < _minValue)
            return Result.Failure<DateTime>($"A data e hora deve ser maior que {_minValue.Value.ToString(DefaultFormat)}.");

        return Result.Success(result);
    }
}
