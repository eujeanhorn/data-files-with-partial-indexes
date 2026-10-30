namespace ECommerceDatabase.Forms.Fields
{
    public interface IFormField<TFormData>
    {
        string Message { get; }
        void Run(TFormData formDataInstance);
    }
}