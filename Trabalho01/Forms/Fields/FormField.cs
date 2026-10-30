using CSharpFunctionalExtensions;
using System.Linq.Expressions;
using System.Reflection;

namespace ECommerceDatabase.Forms.Fields;

public abstract class FormField<TFormData, TFieldValue> : IFormField<TFormData>
{
    public string Message { get; }
    public bool Optional { get; }
    protected virtual string? FieldFormat => null;
    protected readonly Expression<Func<TFormData, TFieldValue>> _property;

    protected FormField(string message, bool optional, Expression<Func<TFormData, TFieldValue>> property)
    {
        Message = message;
        Optional = optional;
        _property = property;
    }


    public virtual void Run(TFormData formDataInstance)
    {
        string? lastMessage = null;
        var message = BuildMessage();

        while (true)
        {
            Console.Clear();
            Console.WriteLine();

            if (lastMessage != null)
                Console.WriteLine(lastMessage);

            Console.Write(message);
            var input = Console.ReadLine();

            if (Optional && string.IsNullOrEmpty(input))
                break;

            var validateResult = ValidateInput(input);
            if (validateResult.IsFailure)
            {
                lastMessage = validateResult.Error;
                continue;
            }

            var member = (MemberExpression)_property.Body;
            var propertyInfo = (PropertyInfo)member.Member;

            propertyInfo.SetValue(formDataInstance, validateResult.Value);
            break;
        }
    }

    protected string BuildMessage()
    {
        if (FieldFormat == null)
            return Message + ": ";

        return string.Format("{0} ({1}): ", Message, FieldFormat);
    }

    protected abstract Result<TFieldValue> ValidateInput(string? input);
}