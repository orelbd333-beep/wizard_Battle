# wizard_Battle
Wizard battle either over you're local internet connection or offline,
you gain power by completing puzzles in a short time under pressure!!

Winning rules:
Win by having greator power than youre oponent, tie by having a diffrence of less than 10. 

Have fun!

## Run the project

Install the .NET 10 SDK, then run this command from the repository folder:

```bash
dotnet run --project Wizards.csproj
```

Use an interactive terminal, such as VS Code's integrated terminal. Press **Escape** during the introduction countdown to skip to Dumbledore's turn.

## Project files

- `wizards_hackclub.cs`: main program and winner comparisons.
- `Helpers/Introduction.cs`: welcome message, countdown, and Escape handling.
- `Wizards.csproj`: compiles the main program and helper files together.

Puzzle gameplay and multiplayer are still in development. Both power values currently start at zero.
