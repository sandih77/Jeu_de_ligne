using app.model;
using Point = app.model.Point;

namespace Projet_Jeu_De_Ligne;

public class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    // [STAThread]
    public static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // ApplicationConfiguration.Initialize();
        // Application.Run(new Form1());
        Point p = new Point(10, 10);
        Point p1 = new Point(15, 15);
        Point p2 = new Point(20, 20);
        Point p3 = new Point(13, 13);
        List<Point> list = new List<Point>();
        list.Add(p);
        list.Add(p1);
        list.Add(p2);
        list.Add(p3);
        Board board = new Board();
        board.points = list;
        foreach (Point item in board.points)
        {
            Console.WriteLine("X " + item.X);
            Console.WriteLine("Y " + item.Y);
        }
    }
}