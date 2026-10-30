using ECommerceDatabase.Consoles;
using ECommerceDatabase.Forms;
using ECommerceDatabase.Forms.Fields.Implementations;
using ECommerceDatabase.Imports;
using ECommerceDatabase.Menus;
using ECommerceDatabase.TeableViewers;
using ECommerceDatabase.UserSessions.Domain;
using ECommerceDatabase.UserSessions.Models;
using ECommerceDatabase.UserSessions.TableReaders;

namespace ECommerceDatabase.UserSessions.Menus;

internal class UserSessionsMenu
{

    public static Menu Create(UserSessionsTableReader userSessionsTableReader)
    {
        var userSessionForm = BuildUserSessionForm();
        var userSessionSearchForm = BuildUserSessionSearchForm();
        var importForm = BuildImportForm();

        return new Menu("Sessões dos usuários", '-')
            .AddOption(1, "Inserir dados iniciais", () =>
            {
                var importData = importForm.Run();
                return new(CreateInitialData(importData.FilePath, userSessionsTableReader));
            })
            .AddOption(2, "Mostrar dados", () =>
            {
                ShowUserSessions(userSessionsTableReader);
                return new(null);
            })
            .AddOption(3, "Pesquisa direto no arquivo de dados", () =>
            {
                var searchData = userSessionSearchForm.Run();
                if (!string.IsNullOrEmpty(searchData.ID))
                    ShowSearchResult(userSessionsTableReader.TryGetRecord(searchData.ID));

                return new(null);
            })
            .AddOption(4, "Pesquisa usando índice parcial", () =>
            {
                var searchData = userSessionSearchForm.Run();
                if (!string.IsNullOrEmpty(searchData.ID))
                    ShowSearchResult(userSessionsTableReader.TryGetRecordByIndex(searchData.ID));

                return new(null);
            })
            .AddOption(5, "Inserir registro", () =>
            {
                var userSessionData = userSessionForm.Run();
                var createdUserSession = SaveNewUserSession(userSessionData, userSessionsTableReader);
                return new($"Sessão do usuário criada com ID {createdUserSession.ID}.");
            })
            .AddOption(6, "Remover registro", () =>
            {
                var searchData = userSessionSearchForm.Run();
                if (string.IsNullOrEmpty(searchData.ID))
                    return new(null);

                return userSessionsTableReader.TryDelete(searchData.ID)
                    ? new("Sessão do usuário removida.")
                    : new("Sessão do usuário não encontrada.");
            })
            .AddOption(0, "Voltar", null);
    }

    private static string? CreateInitialData(string csvFilePath, UserSessionsTableReader userSessionsTableReader)
    {
        if (string.IsNullOrEmpty(csvFilePath))
            return null;

        if (!File.Exists(csvFilePath))
            return "Arquivo não encontrado.";

        if (!ConsoleUtils.Confirm("Os dados atuais de sessões serão substituídos. Continuar?"))
            return null;

        Console.WriteLine("Importando...");

        // O CSV deve estar ordenado por user_session. Eventos sem sessão são ignorados.
        var userSessions = EventsCsvReader.ReadLines(csvFilePath)
            .Where(l => l.UserSession.Length == UserSession.IdLength)
            .Select(l => new UserSession(l.UserSession, l.UserID, l.ProductID, l.EventType, l.EventTime, 0, 0));

        try
        {
            var count = userSessionsTableReader.ReplaceAll(userSessions);
            return $"{count} sessões importadas.";
        }
        catch (InvalidDataException exception)
        {
            return $"Falha na importação: {exception.Message}";
        }
    }

    private static UserSession SaveNewUserSession(UserSessionFormData userSessionData, UserSessionsTableReader userSessionsTableReader)
    {
        var userSession = new UserSession(
            Guid.NewGuid().ToString(),
            userSessionData.UserID,
            userSessionData.ProductID,
            userSessionData.EventType,
            userSessionData.EventTime,
            0,
            0);

        userSessionsTableReader.Insert(userSession);
        return userSession;
    }

    private static void ShowSearchResult(UserSession? record)
    {
        Console.Clear();
        Console.WriteLine();

        if (record == null)
        {
            Console.WriteLine("Registro não encontrado.");
            Console.WriteLine("------------------------");
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para continuar.");
            ConsoleUtils.WaitForAnyKey();
            return;
        }

        Console.WriteLine("Registro encontrado:");
        Console.WriteLine("--------------------");
        Console.WriteLine("- ID: {0}", record.ID);
        Console.WriteLine("- Tipo: {0}", record.EventType);
        Console.WriteLine("- Produto: {0}", record.ProductID);
        Console.WriteLine("- Usuário: {0}", record.UserID);
        Console.WriteLine("- Data: {0}", record.EventTime);
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para continuar.");
        ConsoleUtils.WaitForAnyKey();
    }

    private static void ShowUserSessions(UserSessionsTableReader userSessionsTableReader)
    {
        const int recordsPerPage = 10;
        var pages = userSessionsTableReader.GetAllPaginated(recordsPerPage);

        new TableViewer<UserSession>()
            .AddColumn("ID", p => p.ID.ToString(), 36)
            .AddColumn("Tipo", p => p.EventType, 8)
            .AddColumn("Produto", p => p.ProductID.ToString(), 10)
            .AddColumn("Usuário", p => p.UserID.ToString(), 15)
            .AddColumn("Data", p => p.EventTime.ToString(), 20)
            .Run(pages, recordsPerPage);
    }


    private static Form<UserSessionFormData> BuildUserSessionForm()
        => Form.Create<UserSessionFormData>()
            .AddField(new IntFormField<UserSessionFormData>("Informe o ID do usuário", p => p.UserID, minValue: 1))
            .AddField(new IntFormField<UserSessionFormData>("Informe o ID do produto", p => p.ProductID, minValue: 1))
            .AddField(new StringFormField<UserSessionFormData>("Informe o tipo do evento", p => p.EventType, minLength: 1, maxLength: UserSession.EventTypeLength))
            .AddField(new DateTimeFormField<UserSessionFormData>("Informe a data e hora do evento", p => p.EventTime));

    private static Form<UserSessionSearchFormData> BuildUserSessionSearchForm()
        => Form.Create<UserSessionSearchFormData>()
            .AddField(new StringFormField<UserSessionSearchFormData>("Informe o ID da sessão", p => p.ID, minLength: UserSession.IdLength, maxLength: UserSession.IdLength, optional: true));

    private static Form<ImportFormData> BuildImportForm()
        => Form.Create<ImportFormData>()
            .AddField(new StringFormField<ImportFormData>("Informe o caminho do arquivo CSV", p => p.FilePath, optional: true));
}
