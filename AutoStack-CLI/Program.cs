if (args.Length > 0)
{
    var uri = args[0];
    Console.WriteLine($"AutoStack launched with: {uri}");

    // Parse the URI (e.g., autostack://action?param=value)
    if (uri.StartsWith("autostack://"))
    {
        var path = uri.Substring("autostack://".Length);
        Console.WriteLine($"Handling: {path}");
        // Add your logic here
    }
}
else
{
    Console.WriteLine("AutoStack CLI - No arguments provided");
}

Console.ReadKey();
