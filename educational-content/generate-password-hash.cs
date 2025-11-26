// Script per generare hash password ASP.NET Identity per Pa$$W0rd
// Compile: dotnet script generate-password-hash.cs

#r "nuget: Microsoft.AspNetCore.Identity, 8.0.0"

using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var password = "Pa$$W0rd";
var hash = hasher.HashPassword(null, password);

Console.WriteLine("==============================================");
Console.WriteLine("ASP.NET Identity Password Hash Generator");
Console.WriteLine("==============================================");
Console.WriteLine($"Plain Password: {password}");
Console.WriteLine($"Hashed Password:\n{hash}");
Console.WriteLine("==============================================");
Console.WriteLine("Use this hash in SQL INSERT statements for AspNetUsers table");
Console.WriteLine("==============================================");
