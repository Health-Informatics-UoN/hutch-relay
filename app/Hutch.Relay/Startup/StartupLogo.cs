using Spectre.Console;
using UoN.VersionInformation;

namespace Hutch.Relay.Startup;

public static class StartupLogo
{
  public static void Display(IAnsiConsole? console = null)
  {
    console ??= AnsiConsole
      .Create(new() { Out = new AnsiConsoleOutput(Console.Error) });

    console.Write(new Rule().RuleStyle(Color.Blue));

    console.Write(new FigletText("Hutch Relay")
        .LeftJustified()
        .Color(Color.Blue));

    // write out version
    var version = new VersionInformationService().EntryAssembly(); // we're pre-DI here...
    console
      .WriteLine(
        Emoji.Known.Rabbit +
        " Hutch Relay v" +
        version);

    // Close out the logo section
    console.Write(new Rule().RuleStyle(Color.Blue));
  }
}
