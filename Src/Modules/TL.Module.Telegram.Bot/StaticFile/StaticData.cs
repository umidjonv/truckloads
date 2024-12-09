using System;
using Microsoft.Extensions.Configuration;

public static class StaticData
{
	private  static readonly IConfiguration _configuration; 
	
	static StaticData()
	{
		
		_configuration = new ConfigurationBuilder()
			.SetBasePath(AppDomain.CurrentDomain.BaseDirectory) 
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) 
			.Build();


		botToken = _configuration["BotSettings:Token"] ?? "DefaultToken";
		BotBase_Url = _configuration["BotSettings:BotBase_Url"] ??
		              $"https://api.telegram.org/bot{botToken}/sendMessage";
	}
	
	public static string botToken { get; private set; }

	public static string BotBase_Url { get; private set; }
}
