namespace ECommerceDatabase.Consoles;

public static class ConsoleUtils
{
    public static ConsoleKey WaitForKeys(params ConsoleKey[] keys)
    {
        ConsoleKey pressedKey;

        if (keys.Length == 0)
            throw new ArgumentException("Console keys mus be informed.", nameof(keys));

        do
        {
            pressedKey = WaitForAnyKey();
        } while (!keys.Contains(pressedKey));

        return pressedKey;
    }

    public static ConsoleKey WaitForAnyKey()
        => Console.ReadKey(intercept: true).Key;

    public static bool Confirm(string message)
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("{0} [S/N]", message);

        return WaitForKeys(ConsoleKey.S, ConsoleKey.N) == ConsoleKey.S;
    }

}
