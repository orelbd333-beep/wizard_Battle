# Wizard Battle

A C# console game in progress featuring Dumbledore and Riddle.

The introduction displays a countdown. Press **Escape** during the countdown to skip to Dumbledore's turn. Puzzle gameplay is still in development; both power values currently start at zero.

## Run

Install the .NET 10 SDK, then run this command from the project folder:

```bash
dotnet run --project Wizards.csproj
```

Use a terminal that accepts keyboard input, such as VS Code's integrated terminal.

## Files

- `wizards_hackclub.cs`: the main program and winner comparisons.
- `Helpers/Introduction.cs`: the introduction, countdown, and Escape handling.
- `Wizards.csproj`: compiles the program and helper files together.
