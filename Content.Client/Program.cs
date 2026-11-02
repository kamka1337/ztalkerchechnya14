using Robust.Client;

namespace Content.Client
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ContentStart.StartLibrary(args, new GameControllerOptions
            {
                DefaultWindowTitle = "chechnya14"
            });
        }
    }
}
