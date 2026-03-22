namespace app.utils;

using Npgsql;

public class UtilsDB
{
    private static string connString = "Host=localhost;Username=postgres;Password=postgres;Database=jeu_de_ligne";

    public static NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(connString);
    }

    public static async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task InitializeDatabaseAsync()
    {
        // Le schema SQL se trouve dans script/schema.sql
        // Executez-le manuellement avec: psql -U postgres -d jeu_de_ligne -f script/schema.sql
        // Ou utilisez ExecuteSqlFileAsync() pour l'executer depuis l'application

        using var conn = GetConnection();
        await conn.OpenAsync();

        // Verification basique que les tables existent
        var checkSql = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'game_saves')";
        using var cmd = new NpgsqlCommand(checkSql, conn);
        var exists = (bool)(await cmd.ExecuteScalarAsync() ?? false);

        if (!exists)
        {
            throw new Exception("Les tables de la base de donnees n'existent pas. Executez script/schema.sql d'abord.");
        }
    }

    public static async Task ExecuteSqlFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Fichier SQL introuvable: {filePath}");
        }

        var sql = await File.ReadAllTextAsync(filePath);
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}