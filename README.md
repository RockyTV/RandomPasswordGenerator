## RandomPasswordGenerator
**RandomPasswordGenerator** is a tool that tries to implement the [Diceware](http://world.std.com/~reinhold/diceware.html) method for generating secure random passphrases.

It should be cryptographically secure using `System.Random`. What the tool does is use the `RNGCryptoServiceProvider` class to generate a random seed for `System.Random`.

If anyone has a better way of generating cryptographically secure random numbers, submit a Pull Request.

This tool utilizes gsscoder's [CommandLine library](https://github.com/gsscoder/commandline).

### Usage

Compile the project, and run `randompassgen.exe`. If you want to run the program in batch mode, use this:
`randompassgen.exe (-b or -batch or --batch)`

It will download the word list from Diceware, parse it, and then generate a random 8 word passphrase. The passphrase will be automatically copied to your clipboard.
