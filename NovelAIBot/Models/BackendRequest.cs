using Discord.Interactions;
using System.Text.Json.Serialization;


namespace NovelAIBot.Models
{
	internal class BackendRequest : INaiRequest
	{
		public Guid Id { get; set; }
		public string Prompt { get; private set; }
		public string NegativePrompt { get; private set; }
		public string AuthKey { get; private set; }

		[JsonIgnore]
		public SocketInteractionContext Context { get; set; }

		public int Height { get; set; }

		public int Width { get; set; }

		public int Seed { get; set; }
		public BackendRequest(string prompt, string negativePrompt, string authKey, int height, int width, int seed)
		{
			Id = Guid.Empty;
			Prompt = prompt;
			NegativePrompt = negativePrompt;
			AuthKey = authKey;
			Height = height;
			Width = width;
			Seed = seed;
		}

		public BackendRequest(string prompt, string negativePrompt, string authKey, SocketInteractionContext context,
			int height, int width, int seed)
		{
			Id = Guid.Empty;
			Prompt = prompt;
			NegativePrompt = negativePrompt;
			AuthKey = authKey;
			Context = context;
			Height = height;
			Width = width;
		}

		public void AppendToPrompt(string input)
		{
			if (!string.IsNullOrEmpty(input))
				Prompt += ", " + input;
		}
		public void AppendToNegativePrompt(string input)
		{
			if (!string.IsNullOrEmpty(input))
			{
				if (string.IsNullOrEmpty(NegativePrompt))
					NegativePrompt = input;
				else NegativePrompt += ", " + input;
			}	
		}
	}
}
