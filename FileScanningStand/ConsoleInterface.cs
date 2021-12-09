using System;
using System.Collections.Generic;

namespace FileScanningStand
{
    public class ConsoleInterface
    {
        public static void Main()
        {
            var stand = new Stand();

            while (true)
            {
                Console.WriteLine(
                    @"Enter the command: ""Scan"", ""GetBasesDate"", ""Test"", ""RevertToSnapshot"" or ""Exit""");

                switch (Console.ReadLine())
                {
                    case "Scan":

                        Console.WriteLine("Enter the path to the object");

                        var path = Console.ReadLine();

                        var res = new List<string>();

                        stand.Scan(path, ref res);

                        foreach (var str in res)
                        {
                            if (str == "KAVReport:" || str == "DWReport:")
                            {
                                Console.WriteLine("\n" + str);

                                continue;
                            }

                            if (str.Substring(str.Length - 8) == " - clean")
                            {
                                Console.ForegroundColor = ConsoleColor.Green;

                                Console.WriteLine("\n" + str + "\n");

                                Console.ResetColor();

                                continue;
                            }

                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.WriteLine("\n" + str + "\n");

                            Console.ResetColor();
                        }

                        break;

                    case "GetBasesDate":

                        Console.WriteLine(stand.GetBasesDate());

                        break;

                    case "Test":

                        Console.WriteLine(stand.Test());

                        break;

                    case "RevertToSnapshot":

                        stand.RevertToSnapshot();

                        break;

                    case "Exit":

                        stand.Exit();

                        return;

                    default:

                        Console.WriteLine("Error: Unknown command!");

                        break;
                }
            }
        }
    }
}