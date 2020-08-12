namespace Renamer
{
    class Program
    {
        static void Main(string[] args)
        {
            string Get(int index)
                => index < args.Length ? args[index] : null;

            Renamer.RunInConsole(Get(0), Get(1), Get(2));
        }
    }
}
