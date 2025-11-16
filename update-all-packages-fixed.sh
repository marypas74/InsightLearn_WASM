#!/bin/bash
set -e

echo "📦 [2/7] Aggiornamento InsightLearn.Core..."
dotnet add src/InsightLearn.Core/InsightLearn.Core.csproj package AutoMapper --version 13.0.1
dotnet add src/InsightLearn.Core/InsightLearn.Core.csproj package Microsoft.Extensions.Identity.Stores --version 8.0.11
dotnet add src/InsightLearn.Core/InsightLearn.Core.csproj package System.IdentityModel.Tokens.Jwt --version 8.2.1
echo "✅ Core updated"

echo "📦 [3/7] Aggiornamento InsightLearn.Infrastructure..."
dotnet add src/InsightLearn.Infrastructure/InsightLearn.Infrastructure.csproj package AutoMapper --version 13.0.1
dotnet add src/InsightLearn.Infrastructure/InsightLearn.Infrastructure.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection --version 13.0.1
dotnet add src/InsightLearn.Infrastructure/InsightLearn.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add src/InsightLearn.Infrastructure/InsightLearn.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools --version 8.0.11
dotnet add src/InsightLearn.Infrastructure/InsightLearn.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.10
echo "✅ Infrastructure updated"

echo "📦 [4/7] Aggiornamento InsightLearn.Application..."
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package AspNetCore.HealthChecks.Elasticsearch --version 8.2.1
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package AspNetCore.HealthChecks.MongoDb --version 9.0.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package AspNetCore.HealthChecks.Redis --version 9.0.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package AspNetCore.HealthChecks.SqlServer --version 9.0.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package AspNetCore.HealthChecks.UI.Client --version 9.0.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package AspNetCore.HealthChecks.Uris --version 9.0.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package AutoMapper --version 13.0.1
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package AutoMapper.Extensions.Microsoft.DependencyInjection --version 13.0.1
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package ClosedXML --version 0.104.2
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package FluentValidation --version 11.11.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package FluentValidation.DependencyInjectionExtensions --version 11.11.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package itext7 --version 9.0.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package itext7.bouncy-castle-adapter --version 9.0.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package MediatR --version 12.4.2
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package Microsoft.AspNetCore.DataProtection --version 8.0.11
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package Microsoft.Extensions.Diagnostics.HealthChecks --version 8.0.11
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package Microsoft.Extensions.Hosting --version 8.0.1
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package MongoDB.Driver --version 3.2.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package MongoDB.Driver.GridFS --version 3.2.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package QuestPDF --version 2025.1.3
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package StackExchange.Redis --version 2.8.22
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package Stripe.net --version 47.5.0
dotnet add src/InsightLearn.Application/InsightLearn.Application.csproj package Swashbuckle.AspNetCore --version 7.2.0
echo "✅ Application updated"

echo "📦 [5/7] Aggiornamento InsightLearn.Tests..."
dotnet add tests/InsightLearn.Tests.csproj package Bogus --version 36.0.0
dotnet add tests/InsightLearn.Tests.csproj package FluentAssertions --version 8.8.0
dotnet add tests/InsightLearn.Tests.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add tests/InsightLearn.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 8.0.11
dotnet add tests/InsightLearn.Tests.csproj package Microsoft.AspNetCore.TestHost --version 8.0.11
dotnet add tests/InsightLearn.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11
dotnet add tests/InsightLearn.Tests.csproj package Microsoft.Extensions.Configuration.Json --version 8.0.1
dotnet add tests/InsightLearn.Tests.csproj package Microsoft.NET.Test.Sdk --version 18.0.1
dotnet add tests/InsightLearn.Tests.csproj package Moq --version 4.20.72
dotnet add tests/InsightLearn.Tests.csproj package xunit --version 2.9.3
dotnet add tests/InsightLearn.Tests.csproj package xunit.runner.visualstudio --version 3.1.5
echo "✅ Tests updated"

echo "✅ TUTTI I PACCHETTI AGGIORNATI"
