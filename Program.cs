using Eto.Forms;

namespace Glosslate;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        new Application().Run(new MainForm());
    }
}
