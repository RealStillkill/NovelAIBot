using System.Net.WebSockets;
using System.Text;

namespace NovelAIBot.Extensions
{
	public static class WebSocketExtensions
	{
		public static async Task SendTextMessageAsync(this WebSocket client, string text, Encoding encoding)
		{
			byte[] buffer = encoding.GetBytes(text);
			await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
		}

		public static async Task<string> ReceiveTextMessageAsync(this WebSocket client, int bufferSize, Encoding encoding)
		{
			byte[] buffer = new byte[bufferSize];
			var result = await client.ReceiveAsync(buffer, CancellationToken.None);
			Array.Resize(ref buffer, result.Count);
			return encoding.GetString(buffer, 0, buffer.Length) ?? string.Empty;
		}
	}
}