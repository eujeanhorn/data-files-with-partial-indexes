using ECommerceDatabase.Forms.Fields;

namespace ECommerceDatabase.Forms;

public sealed class Form<TFormData>
{
    private readonly Func<TFormData> _formDataFactory;
    private readonly List<IFormField<TFormData>> _fields;

    internal Form(Func<TFormData> formDataFactory)
    {
        _formDataFactory = formDataFactory;
        _fields = [];
    }

    public Form<TFormData> AddField(IFormField<TFormData> field)
    {
        _fields.Add(field);
        return this;
    }

    public TFormData Run()
    {
        var formData = _formDataFactory();

        foreach (var field in _fields)
            field.Run(formData);

        return formData;
    }
}

public static class Form
{
    public static Form<TData> Create<TData>() where TData : new()
        => new(() => new TData());
}