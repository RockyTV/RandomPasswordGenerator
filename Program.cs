using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DicewarePasswordGen
{
    public class Program
    {
        private static string throwError = string.Empty;
        private static string wordListUrl = "http://world.std.com/~reinhold/diceware.wordlist.asc";
        private static string appDir = Environment.CurrentDirectory;
        private static string wordListFile = Path.Combine(appDir, "wordlist.txt");
        private static int passphraseLength = 8;
        private static bool batchMode = false;
        private static Dictionary<int, string> wordList = new Dictionary<int, string>();

        public static bool DownloadWordList()
        {
            if (!File.Exists(wordListFile))
            {
                using (WebClient wc = new WebClient())
                {
                    try
                    {
                        wc.DownloadFile(wordListUrl, Path.Combine(appDir, "wordlist.txt"));
                    }
                    catch (Exception e)
                    {
                        throwError = e.Message;
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool ParseWordList()
        {
            string[] startingIndexes = new string[] { "1", "2", "3", "4", "5", "6" };
            if (!File.Exists(wordListFile))
            {
                return false;
            }

            try
            {
                using (FileStream fs = new FileStream(wordListFile, FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string currentLine = sr.ReadLine();
                            if (currentLine == null)
                            {
                                break;
                            }

                            string trimmedLine = currentLine.Trim();
                            if (string.IsNullOrEmpty(trimmedLine))
                            {
                                continue;
                            }

                            // Check if the current line is a valid entry
                            if (!(trimmedLine.StartsWith("1") || trimmedLine.StartsWith("2") || trimmedLine.StartsWith("3") ||
                                trimmedLine.StartsWith("4") || trimmedLine.StartsWith("5") || trimmedLine.StartsWith("6")))
                            {
                                continue;
                            }

                            int currentKey = int.Parse(trimmedLine.Substring(0, 5));
                            string currentValue = trimmedLine.Substring(6);

                            // Add the current line, if it is a valid entry, to the word list.
                            if (!wordList.ContainsKey(currentKey))
                            {
                                wordList.Add(currentKey, currentValue);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throwError = e.ToString();
                return false;
            }

            return true;
        }

        public static string GeneratePassphrase(int length = 6)
        {
            if (length < 6)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            List<int> generatedNumbers = new List<int>(length);

            string outPassphrase = string.Empty;

            // We want to generate x random 5 digit values
            for (int i = 0; i < length; i++)
            {
                using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
                {
                    sb = new StringBuilder();
                    byte[] randomSeed = new byte[4];
                    rngCsp.GetBytes(randomSeed);

                    Random random = new Random(BitConverter.ToInt32(randomSeed, 0));
                    // We want to generate 5 digit random numbers
                    int[] results = new int[5];
                    for (int j = 0; j < results.Length; j++)
                    {
                        results[j] = random.Next(1, 7);
                    }

                    foreach (int value in results)
                        sb.Append(value);

                    string outStr = sb.ToString();

                    int generatedVal = int.Parse(outStr);

                    if (!generatedNumbers.Contains(generatedVal))
                        generatedNumbers.Add(generatedVal);
                }
            }

            sb = new StringBuilder();
            foreach (int generatedNum in generatedNumbers)
            {
                if (generatedNumbers.IndexOf(generatedNum) != 5)
                    sb.AppendFormat("{0} ", wordList[generatedNum]);
                else
                    sb.AppendLine(wordList[generatedNum]);
            }

            outPassphrase = sb.ToString();
            outPassphrase.Replace(Environment.NewLine, " ");
            return outPassphrase;
        }

        [STAThread()]
        public static int Main(string[] args)
        {
            Console.Clear();
            Console.Title = "Random Password Generator";

            if (args.Length > 0)
            {
                List<string> args2 = new List<string>(args);
                if (args2.Contains("-b") || args2.Contains("-batch") || args2.Contains("--batch"))
                    batchMode = true;
            }

            Console.Write("Downloading word list... ");
            if (!DownloadWordList())
            {
                Console.WriteLine("failed!");
                Console.WriteLine("Exception: {0}", throwError);
                AskToExitIfInteractive();
                return 1;
            }
            else
            {
                Console.WriteLine("done!");
            }

            Console.Write("Parsing word list... ");
            if (!ParseWordList())
            {
                Console.WriteLine("failed!");
                Console.WriteLine(throwError);
                AskToExitIfInteractive();
                return 2;
            }
            else
            {
                Console.WriteLine("done!");
            }

            Console.Write("Generating {0} word random passphrase... ", passphraseLength);
            string generatedPassphrase = GeneratePassphrase(passphraseLength);
            if (generatedPassphrase != null)
            {
                Console.WriteLine("done!");
                Clipboard.SetText(generatedPassphrase);
                Console.WriteLine("Passphrase was copied to clipboard.");
            }
            else
            {
                AskToExitIfInteractive();
                return 3;
            }

            AskToExitIfInteractive();
            return 0;
        }

        public static void AskToExitIfInteractive()
        {
            if (!batchMode)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
