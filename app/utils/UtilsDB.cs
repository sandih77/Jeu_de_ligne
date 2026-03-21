namespace app.utils;

using Npgsql;

public class UtilsDB
{
    private static string connString = "Host:localhost;Username=postgres;Password=postgres;Database=jeu_de_ligne";

    public static NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(connString);
    }  
}