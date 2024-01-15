using System.Text.Json;

namespace NovelAIBot
{
	public class ConfigModel
	{
		public ConnectionStringSettings ConnectionStrings { get; set; }
		public DiscordSettings Discord { get; set; } = new DiscordSettings();
		public string NovelAIToken { get; set; }

		public class ConnectionStringSettings
		{
			public string DefaultConnection { get; set; } = "";
		}

		public class DiscordSettings
		{
			public string Token { get; set; } = "";
		}


		public string ToJson()
			=> JsonSerializer.Serialize(this);


	}
}
