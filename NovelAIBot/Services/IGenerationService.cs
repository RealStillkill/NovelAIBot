using NovelAIBot.Enums;
using NovelAIBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelAIBot.Services
{
	internal interface IGenerationService
	{
		public Task<byte[]> GetImageBytesAsync(INaiRequest request);
	}
}
