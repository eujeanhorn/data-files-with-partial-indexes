using CSharpFunctionalExtensions;
using ECommerceDatabase.Menus.Options;

namespace ECommerceDatabase.Menus;

public class Menu
{
    private readonly string? _title;
    private readonly char _titleSeparator;
    private readonly List<MenuOption> _options;

    public Menu(string? title = null, char titleSeparator = '=')
    {
        _title = title?.ToUpper();
        _titleSeparator = titleSeparator;
        _options = [];
    }

    public Menu AddOption(MenuOption option)
    {
        _options.Add(option);
        return this;
    }

    public Menu AddOption(int accessor, string description, Func<MenuOptionExecutionDetails>? action)
        => AddOption(new MenuOption(accessor, description, action));

    public void Run()
    {
        if (_options.Count == 0)
            throw new InvalidOperationException("Cannot run an empty menu.");

        string? lastMessage = null;

        while(true)
        {
            Console.Clear();
            ShowTitle();
            ShowOptions();
            Console.WriteLine();

            var requestOptionResult = RequestOption(lastMessage);
            if (requestOptionResult.IsFailure)
            {
                lastMessage = requestOptionResult.Error;
                continue;
            }

            lastMessage = null;

            var selectedOption = requestOptionResult.Value;
            if (selectedOption.Action == null)
                break;

            var executionDetails = selectedOption.Action();
            if (executionDetails.Message != null)
                lastMessage = executionDetails.Message;
        } 
    }

    private void ShowTitle()
    {
        if (string.IsNullOrEmpty(_title))
            return;

        var separator = new string(_titleSeparator, _title.Length + 4);
        Console.WriteLine(separator);
        Console.WriteLine("  {0}  ", _title);
        Console.WriteLine(separator);
        Console.WriteLine();
    }

    private void ShowOptions()
    {
        foreach (var option in _options)
            Console.WriteLine("{0} - {1}", option.Accessor, option.Description);
    }

    private Result<MenuOption> RequestOption(string? lastMessage)
    {
        if (lastMessage != null)
            Console.WriteLine(lastMessage);

        Console.Write("Selecione a opção desejada: ");
        var input = Console.ReadLine();

        if (!int.TryParse(input, out var selectedOption))
            return Result.Failure<MenuOption>("Entrada inválida.");

        var option = _options.FirstOrDefault(o => o.Accessor == selectedOption);
        if (option == null)
            return Result.Failure<MenuOption>("Opção inexistente.");

        return Result.Success(option);
    }
}
