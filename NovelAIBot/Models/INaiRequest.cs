using Discord.Interactions;
using System;
using System.Linq;

namespace NovelAIBot.Models
{
	internal interface INaiRequest
	{
		SocketInteractionContext Context { get; }
		int Height { get; }
		string NegativePrompt { get; }
		string Prompt { get; }
		int Width { get; }
		int Seed { get; }
	}
}