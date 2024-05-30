using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelAIBot.Models
{
	internal class NaiRequest : INaiRequest
	{
		public string Prompt { get; private set; }
		public string NegativePrompt { get; private set; }
		public int Height { get; private set; }
		public int Width { get; private set; }
		public SocketInteractionContext Context { get; private set; }

		public NaiRequest(string prompt, string negativePrompt, int height, int width, SocketInteractionContext context)
		{
			Prompt = prompt;
			NegativePrompt = negativePrompt;
			Height = height;
			Width = width;
			Context = context;
		}
	}
}