using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TL.Module.Telegram.Domain;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, false);

var configuration = builder.Build();

var connectionString = configuration.GetConnectionString("TelegramConnectionString");

var optionBuilder = new DbContextOptionsBuilder<TelegramDbContext>();
optionBuilder.UseNpgsql(connectionString);

using var context = new TelegramDbContext(optionBuilder.Options);

context.Database.EnsureCreated();

context.Database.Migrate();