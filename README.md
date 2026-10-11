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

## LAN multiplayer

Run the game in an interactive terminal on each computer and choose **2**.
The game searches the LAN for about two seconds. If no open game is found,
it creates a two-player lobby. Start the first computer before searching on
the second. The host chooses Dumbledore or Riddle after the second player
joins; the guest receives the other character automatically.

Both players press Enter when ready, then share a five-second countdown and
play the same four-digit sorting puzzle at the same time. The host checks
both answers, calculates power using `(int)(elapsedSeconds / 5 * 100)`, and
applies the existing winner comparison. Both screens display the host's result.
Wrong answers and timeouts score zero. This formula currently awards more
power for a longer successful attempt.

Choose **1** for the existing local, sequential game. Choose **3** and enter
the host's IPv4 address if broadcast discovery is unavailable. Hosts display
their addresses while waiting. Escape cancels a waiting lobby or the shared
countdown. The lobby stops advertising once another player joins.

Discovery uses UDP port **47777**; games use TCP port **47778**. Both computers
must be on the same LAN, and the host's firewall must allow those inbound
ports. Guest Wi-Fi/client isolation can prevent LAN connections. No additional
NuGet packages or internet service are required.
