using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace wizards
{
    // LAN discovery finds a lobby; TCP carries the actual game messages.
    static class LanGame
    {
        const int DiscoveryPort = 47777;
        const int GamePort = 47778;
        const string Protocol = "wizard-battle-v1";
        const double TimeLimit = 5;

        public static bool ShowMenu()
        {
            Console.WriteLine("Wizard Battle");
            Console.WriteLine("1. Play locally (take turns on this computer)");
            Console.WriteLine("2. Search the network for a game / create one");
            Console.WriteLine("3. Join a host by IP address");

            string choice = ReadChoice("Choose 1, 2, or 3: ", "1", "2", "3");
            if (choice == "1") return false;
            if (choice == null) return true;

            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Console.WriteLine("Use an interactive terminal to play the timed LAN game.");
                return true;
            }

            try
            {
                if (choice == "3")
                {
                    Console.Write("Host's IPv4 address: ");
                    if (!IPAddress.TryParse(Console.ReadLine(), out IPAddress address)
                        || address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        Console.WriteLine("That is not a valid IPv4 address.");
                        return true;
                    }
                    JoinAsync(address).GetAwaiter().GetResult();
                }
                else
                {
                    SearchOrHostAsync().GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Game cancelled, or the other player did not respond in time.");
            }
            catch (Exception error) when (error is SocketException || error is IOException
                || error is JsonException || error is InvalidDataException)
            {
                Console.WriteLine($"LAN game ended: {error.Message}");
                Console.WriteLine("Both computers must be on the same LAN. You can also try joining by IP.");
            }

            return true;
        }

        static string ReadChoice(string prompt, params string[] choices)
        {
            while (true)
            {
                Console.Write(prompt);
                string answer = Console.ReadLine()?.Trim();
                if (answer == null || choices.Contains(answer)) return answer;
                Console.WriteLine("Please choose one of the listed options.");
            }
        }

        static async Task SearchOrHostAsync()
        {
            Console.WriteLine("Searching for open games...");
            List<Lobby> games = await DiscoverAsync();
            if (games.Count == 0)
            {
                Console.WriteLine("No open games found. Creating a game...");
                await HostAsync();
                return;
            }

            for (int i = 0; i < games.Count; i++)
                Console.WriteLine($"{i + 1}. {games[i].Name} ({games[i].Address})");

            int selected = 0;
            if (games.Count > 1)
            {
                string choice = ReadChoice("Choose a game: ",
                    Enumerable.Range(1, games.Count).Select(i => i.ToString()).ToArray());
                if (choice == null) return;
                selected = int.Parse(choice) - 1;
            }
            await JoinAsync(games[selected].Address);
        }

        static IEnumerable<IPAddress> BroadcastAddresses()
        {
            // Loopback also allows two terminals on one computer for testing.
            var addresses = new HashSet<IPAddress> { IPAddress.Broadcast, IPAddress.Loopback };
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                foreach (UnicastIPAddressInformation entry in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (entry.Address.AddressFamily != AddressFamily.InterNetwork
                        || IPAddress.IsLoopback(entry.Address)) continue;
                    byte[] ip = entry.Address.GetAddressBytes();
                    byte[] mask = entry.IPv4Mask.GetAddressBytes();
                    addresses.Add(new IPAddress(ip.Zip(mask, (a, b) => (byte)(a | ~b)).ToArray()));
                }
            }
            return addresses;
        }

        static async Task<List<Lobby>> DiscoverAsync()
        {
            using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            udp.EnableBroadcast = true;
            string requestId = Guid.NewGuid().ToString("N");
            byte[] probe = Encoding.UTF8.GetBytes(Protocol + ":" + requestId);
            var found = new Dictionary<string, Lobby>();

            // Retry probes in case a host starts while the search is in progress.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                foreach (IPAddress target in BroadcastAddresses())
                {
                    try
                    {
                        await udp.SendAsync(probe, probe.Length, new IPEndPoint(target, DiscoveryPort));
                    }
                    catch (SocketException) { /* An adapter may not support broadcasts. */ }
                }

                using var timeout = new CancellationTokenSource(700);
                try
                {
                    while (true)
                    {
                        UdpReceiveResult reply = await udp.ReceiveAsync(timeout.Token);
                        try
                        {
                            Message message = JsonSerializer.Deserialize<Message>(reply.Buffer);
                            if (message == null || message.Type != Protocol || message.Answer != requestId
                                || string.IsNullOrEmpty(message.GameId) || message.Port != GamePort) continue;
                            found.TryAdd(message.GameId,
                                new Lobby(message.Name, reply.RemoteEndPoint.Address));
                        }
                        catch (JsonException) { /* Ignore unrelated LAN traffic. */ }
                    }
                }
                catch (OperationCanceledException) { }
            }
            return found.Values.ToList();
        }

        static async Task AdvertiseAsync(UdpClient udp, string gameId, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    UdpReceiveResult probe = await udp.ReceiveAsync(token);
                    string text = Encoding.UTF8.GetString(probe.Buffer);
                    if (!text.StartsWith(Protocol + ":", StringComparison.Ordinal)) continue;
                    byte[] reply = JsonSerializer.SerializeToUtf8Bytes(new Message
                    {
                        Type = Protocol, GameId = gameId, Name = Environment.MachineName,
                        Port = GamePort, Answer = text.Substring(Protocol.Length + 1)
                    });
                    await udp.SendAsync(reply, reply.Length, probe.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        static async Task HostAsync()
        {
            var listener = new TcpListener(IPAddress.Any, GamePort);
            try
            {
                listener.Start(1);
                using var discovery = new UdpClient(DiscoveryPort);
                using var advertising = new CancellationTokenSource();
                Task advertiseTask = AdvertiseAsync(discovery, Guid.NewGuid().ToString("N"), advertising.Token);
                Console.WriteLine("Game created. Waiting for a second player... (Escape to cancel)");
                foreach (IPAddress ip in NetworkInterface.GetAllNetworkInterfaces()
                    .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                    .Select(entry => entry.Address)
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    .Distinct())
                    Console.WriteLine($"Your host address: {ip}");

                using var waiting = new CancellationTokenSource();
                Task<TcpClient> accept = listener.AcceptTcpClientAsync(waiting.Token).AsTask();
                try
                {
                    while (!accept.IsCompleted)
                    {
                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                            waiting.Cancel();
                        await Task.Delay(30);
                    }
                    using TcpClient client = await accept;
                    advertising.Cancel();
                    await advertiseTask;
                    listener.Stop();
                    using var connection = new Connection(client);
                    await RunHostMatchAsync(connection);
                }
                finally
                {
                    advertising.Cancel();
                    await advertiseTask;
                }
            }
            finally { listener.Stop(); }
        }

        static async Task RunHostMatchAsync(Connection connection)
        {
            await connection.ExpectAsync("join");
            Console.WriteLine("A player joined your game!");
            string choice = ReadChoice("Choose your character: 1. Dumbledore  2. Riddle: ", "1", "2");
            if (choice == null) throw new OperationCanceledException();
            string hostRole = choice == "1" ? "Dumbledore" : "Riddle";
            string guestRole = choice == "1" ? "Riddle" : "Dumbledore";
            await connection.SendAsync(new Message { Type = "role", Name = guestRole });
            Console.WriteLine($"You are {hostRole}. The other player is {guestRole}.");
            WaitForReady();
            Console.WriteLine("Waiting for the other player to be ready...");
            await connection.ExpectAsync("ready");

            // Estimate the peer's clock offset so the shared start time works
            // even when the computers' system clocks differ.
            long bestRoundTrip = long.MaxValue;
            long peerOffset = 0;
            for (int i = 0; i < 3; i++)
            {
                long before = DateTime.UtcNow.Ticks;
                await connection.SendAsync(new Message { Type = "ping" });
                Message pong = await connection.ExpectAsync("pong");
                long after = DateTime.UtcNow.Ticks;
                if (after - before < bestRoundTrip)
                {
                    bestRoundTrip = after - before;
                    peerOffset = pong.StartTicks - (before + (after - before) / 2);
                }
            }

            string puzzle = Random.Shared.Next(1000, 10000).ToString();
            long startTicks = DateTime.UtcNow.AddSeconds(5).Ticks;
            await connection.SendAsync(new Message
            {
                Type = "start", Puzzle = puzzle, StartTicks = startTicks + peerOffset
            });

            // Receive in parallel while the host plays their own timed puzzle.
            Task<Message> guestResultTask = connection.ExpectAsync("answer", 20);
            WaitForStart(startTicks);
            Message hostResult = PlayPuzzle(hostRole, puzzle);
            Console.WriteLine("Waiting for the other player's result...");
            Message guestResult = await guestResultTask;

            int hostPower = CalculatePower(puzzle, hostResult);
            int guestPower = CalculatePower(puzzle, guestResult);
            int dumbledore = hostRole == "Dumbledore" ? hostPower : guestPower;
            int riddle = hostRole == "Riddle" ? hostPower : guestPower;
            // Only the host runs the battle; the guest displays its result.
            string winner = dumbledore == riddle ? "tie!!!"
                : dumbledore > riddle ? "Dumbledore wins!!!" : "Riddle wins!!!";
            var battle = new Message
            {
                Type = "battle", DumbledorePower = dumbledore, RiddlePower = riddle, Name = winner
            };
            await connection.SendAsync(battle);
            PrintBattle(battle);
        }

        static async Task JoinAsync(IPAddress address)
        {
            Console.WriteLine($"Joining {address}...");
            using var client = new TcpClient(AddressFamily.InterNetwork);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await client.ConnectAsync(address, GamePort, timeout.Token);
            using var connection = new Connection(client);
            await connection.SendAsync(new Message { Type = "join" });
            Message role = await connection.ExpectAsync("role");
            if (role.Name != "Dumbledore" && role.Name != "Riddle")
                throw new InvalidDataException("The host sent an invalid character.");
            Console.WriteLine($"You are {role.Name}. The host chose the other character.");
            WaitForReady();
            await connection.SendAsync(new Message { Type = "ready" });
            Console.WriteLine("Waiting for the host to start...");

            Message start;
            while (true)
            {
                start = await connection.ReadAsync();
                if (start.Type == "start") break;
                if (start.Type != "ping") throw new InvalidDataException("Unexpected game message.");
                await connection.SendAsync(new Message { Type = "pong", StartTicks = DateTime.UtcNow.Ticks });
            }
            if (start.Puzzle == null || start.Puzzle.Length != 4
                || start.Puzzle.Any(digit => digit < '0' || digit > '9'))
                throw new InvalidDataException("The host sent an invalid puzzle.");
            WaitForStart(start.StartTicks);
            Message answer = PlayPuzzle(role.Name, start.Puzzle);
            await connection.SendAsync(answer);
            Console.WriteLine("Waiting for the host's battle result...");
            Message battle = await connection.ExpectAsync("battle", 15);
            PrintBattle(battle);
        }

        static void WaitForReady()
        {
            Console.WriteLine("Sort all four digits from lowest to highest within 5 seconds.");
            Console.WriteLine("Press Enter when you are ready.");
            if (Console.ReadLine() == null) throw new OperationCanceledException();
        }

        static void WaitForStart(long startTicks)
        {
            double seconds = (startTicks - DateTime.UtcNow.Ticks) / (double)TimeSpan.TicksPerSecond;
            if (seconds < -1 || seconds > 10)
                throw new InvalidDataException("The shared start time is no longer valid.");
            Stopwatch countdown = Stopwatch.StartNew();
            int last = -1;
            while (countdown.Elapsed.TotalSeconds < seconds)
            {
                int remaining = (int)Math.Ceiling(seconds - countdown.Elapsed.TotalSeconds);
                if (remaining != last)
                {
                    Console.WriteLine($"Both turns start in {remaining}...");
                    last = remaining;
                }
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    throw new OperationCanceledException();
                Thread.Sleep(10);
            }
            // Discard typing during the countdown so it cannot answer the puzzle.
            while (Console.KeyAvailable) Console.ReadKey(true);
        }

        static Message PlayPuzzle(string role, string puzzle)
        {
            Console.WriteLine($"{role}'s turn: {puzzle}");
            string answer = "";
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.Elapsed.TotalSeconds < TimeLimit)
            {
                Console.Write($"\rTime: {Math.Max(0, TimeLimit - timer.Elapsed.TotalSeconds):0.00}s | Answer: {answer,-4}");
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (timer.Elapsed.TotalSeconds >= TimeLimit) break;
                    if (key.KeyChar >= '0' && key.KeyChar <= '9') answer += key.KeyChar;
                    else if (key.Key == ConsoleKey.Backspace && answer.Length > 0)
                        answer = answer.Remove(answer.Length - 1);
                    if (answer.Length == puzzle.Length) break;
                }
                Thread.Sleep(10);
            }
            timer.Stop();
            var result = new Message
            {
                Type = "answer", Answer = answer, ElapsedSeconds = Math.Min(TimeLimit, timer.Elapsed.TotalSeconds)
            };
            Console.WriteLine($"\rTime: {Math.Max(0, TimeLimit - result.ElapsedSeconds):0.00}s | Answer: {answer,-4}");
            int power = CalculatePower(puzzle, result);
            bool solved = answer == string.Concat(puzzle.OrderBy(digit => digit))
                && result.ElapsedSeconds < TimeLimit;
            Console.WriteLine(solved ? $"Correct! You gained {power} power."
                : $"Failed! The correct answer was {string.Concat(puzzle.OrderBy(digit => digit))}.");
            return result;
        }

        static int CalculatePower(string puzzle, Message result)
        {
            if (!double.IsFinite(result.ElapsedSeconds) || result.ElapsedSeconds < 0
                || result.ElapsedSeconds >= TimeLimit
                || result.Answer != string.Concat(puzzle.OrderBy(digit => digit))) return 0;
            // Preserve the user's existing scoring formula, including the int cast.
            return (int)(result.ElapsedSeconds / 5 * 100);
        }

        static void PrintBattle(Message battle)
        {
            Console.WriteLine("Battle result (decided by the host):");
            Console.WriteLine($"Dumbledore's power: {battle.DumbledorePower}");
            Console.WriteLine($"Riddle's power: {battle.RiddlePower}");
            Console.WriteLine(battle.Name);
        }

        record Lobby(string Name, IPAddress Address);

        sealed class Message
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string GameId { get; set; }
            public int Port { get; set; }
            public string Puzzle { get; set; }
            public string Answer { get; set; }
            public long StartTicks { get; set; }
            public double ElapsedSeconds { get; set; }
            public int DumbledorePower { get; set; }
            public int RiddlePower { get; set; }
        }

        sealed class Connection : IDisposable
        {
            readonly StreamReader reader;
            readonly StreamWriter writer;

            public Connection(TcpClient client)
            {
                client.NoDelay = true;
                reader = new StreamReader(client.GetStream(), Encoding.UTF8, false, 1024, true);
                writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false), 1024, true)
                { AutoFlush = true };
            }

            public Task SendAsync(Message message) => writer.WriteLineAsync(JsonSerializer.Serialize(message));

            public async Task<Message> ReadAsync(int seconds = 120)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
                string line = await reader.ReadLineAsync(timeout.Token);
                if (line == null) throw new IOException("The other player disconnected.");
                return JsonSerializer.Deserialize<Message>(line)
                    ?? throw new InvalidDataException("The other player sent an empty message.");
            }

            public async Task<Message> ExpectAsync(string type, int seconds = 120)
            {
                Message message = await ReadAsync(seconds);
                if (message.Type != type) throw new InvalidDataException("Unexpected game message.");
                return message;
            }

            public void Dispose()
            {
                writer.Dispose();
                reader.Dispose();
            }
        }
    }
}
