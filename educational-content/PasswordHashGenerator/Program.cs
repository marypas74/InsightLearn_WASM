using Microsoft.AspNetCore.Identity;

Console.WriteLine("==============================================");
Console.WriteLine("ASP.NET Identity Password Hash Generator");
Console.WriteLine("InsightLearn Test Users Password: Pa$$W0rd");
Console.WriteLine("==============================================\n");

var hasher = new PasswordHasher<object>();
var password = "Pa$$W0rd";
var hash = hasher.HashPassword(null, password);

Console.WriteLine($"Plain Password: {password}");
Console.WriteLine($"\nHashed Password (for SQL INSERT):");
Console.WriteLine(hash);
Console.WriteLine("\n==============================================");
Console.WriteLine("Copy this hash into seed-database.sql");
Console.WriteLine("==============================================");
