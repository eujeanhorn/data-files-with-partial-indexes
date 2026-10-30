namespace ECommerceDatabase.Menus.Options;

public sealed class MenuOption
{
    public int Accessor { get; }
    public string Description { get; }
    public Func<MenuOptionExecutionDetails>? Action { get; }

    public MenuOption(int accessor, string description, Func<MenuOptionExecutionDetails>? action)
    {
        Accessor = accessor;
        Description = description;
        Action = action;
    }
}
