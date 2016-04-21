using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace DicewarePasswordGen
{
    public class Options
    {
        [Option('b', "batch", DefaultValue = true, HelpText = "Run the program as batch mode")]
        public bool BatchMode { get; set; }
        [Option('l', "length", DefaultValue = 6, HelpText = "Generate a passphrase containing the specified amount of words. The higher the number of words, the safer the passphrase is.")]
        public int PassphraseLength { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

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
            if (!File.Exists(wordListFile))
                return false;

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
							if (Regex.IsMatch(trimmedLine, @"^([1-6]{5})\t([\S]*)$"))
							{
								Match match = Regex.Match(trimmedLine, @"^([1-6]{5})\t([\S]*)$");
								if (match.Groups.Count == 3)
								{
									int key = int.Parse(match.Groups[1].Value);
									string value = match.Groups[2].Value;
									if (!wordList.ContainsKey(key))
										wordList.Add(key, value);
								}
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
                if (generatedNumbers.IndexOf(generatedNum) != length - 1)
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

            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                batchMode = options.BatchMode;
                passphraseLength = options.PassphraseLength;

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
                if (generatedPassphrase != null && generatedPassphrase != string.Empty)
                {
                    Console.WriteLine("done!");
                    Clipboard.SetText(generatedPassphrase);
                    Console.WriteLine("Passphrase was copied to clipboard.");
                }
                else
                {
                    Console.WriteLine("failed!");
                    Console.WriteLine("Please specify a passphrase length of 6 or more words.");
                    AskToExitIfInteractive();
                    return 3;
                }

                AskToExitIfInteractive();
                return 0;
            }
            return 4;
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
