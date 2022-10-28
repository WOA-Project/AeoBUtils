using CommandLine;
using System.Reflection;

namespace AeoBUtils
{
    [Verb("aeob2aeobsl", HelpText = "Convert AeoB to AeoBsl")]
    public class AeoB2AeoBslOptions
    {
        [Option('p', "path", HelpText = "Path to AeoB", Required = true)]
        public string? path { get; set; }

        [Option('o', "output", HelpText = "Path to output AeoBsl", Required = true)]
        public string? output { get; set; }
    }

    [Verb("aeobsl2aeob", HelpText = "Convert AeoBslto AeoB")]
    public class AeoBsl2AeoBOptions
    {
        [Option('p', "path", HelpText = "Path to AeoBsl", Required = true)]
        public string? path { get; set; }

        [Option('o', "output", HelpText = "Path to output AeoB", Required = true)]
        public string? output { get; set; }
    }

    [Verb("axb2axbsl", HelpText = "Convert AxB to AxBsl")]
    public class AxB2AxBslOptions
    {
        [Option('p', "path", HelpText = "Path to AxB", Required = true)]
        public string? path { get; set; }

        [Option('o', "output", HelpText = "Path to output AxBsl", Required = true)]
        public string? output { get; set; }
    }

    [Verb("axbsl2axb", HelpText = "Convert AxBsl AxB")]
    public class AxBsl2AxBOptions
    {
        [Option('p', "path", HelpText = "Path to AxBsl", Required = true)]
        public string? path { get; set; }

        [Option('o', "output", HelpText = "Path to output AxB", Required = true)]
        public string? output { get; set; }
    }

    internal class Program
    {
        private static void PrintLogo()
        {
            Console.WriteLine($"AeoB/AeoBsl Converter {Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine("Copyright (c) Gustave Monce and Contributors");
            Console.WriteLine("https://github.com/WOA-Project/AeoBUtils");
            Console.WriteLine();
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY.");
            Console.WriteLine("This is free software, and you are welcome to redistribute it under certain conditions.");
            Console.WriteLine();
        }

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<AeoB2AeoBslOptions, AeoBsl2AeoBOptions, AxB2AxBslOptions, AxBsl2AxBOptions>(args).MapResult(
              (AeoB2AeoBslOptions arg) =>
              {
                  PrintLogo();

                  if (File.Exists(arg.path))
                  {
                      using FileStream file = File.OpenRead(arg.path);
                      File.WriteAllText(arg.output, AeoBParser.ParseAeoBFile(file));
                  }

                  return 0;
              },
              (AeoBsl2AeoBOptions arg) =>
              {
                  PrintLogo();

                  if (File.Exists(arg.path))
                  {
                      using FileStream file = File.OpenWrite(arg.output);
                      AeoBBuilder.BuildAeoBFile(File.ReadAllText(arg.path), file);
                  }

                  return 0;
              },
              (AxB2AxBslOptions arg) =>
              {
                  PrintLogo();

                  if (File.Exists(arg.path))
                  {
                      using FileStream file = File.OpenRead(arg.path);
                      File.WriteAllText(arg.output, AdrenoParser.ParseAdrenoFile(file));
                  }

                  return 0;
              },
              (AxBsl2AxBOptions arg) =>
              {
                  PrintLogo();

                  if (File.Exists(arg.path))
                  {
                      using FileStream file = File.OpenWrite(arg.output);
                      AdrenoBuilder.BuildAdrenoFile(File.ReadAllText(arg.path), file);
                  }

                  return 0;
              },
              errs => 1);
        }
    }
}