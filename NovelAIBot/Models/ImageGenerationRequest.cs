using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelAIBot.JsonModels
{
	internal class ImageGenerationRequest
	{
		[JsonProperty("input")]
		public string Input { get; set; }

		[JsonProperty("model")]
		public string Model { get; set; }

		[JsonProperty("action")]
		public string Action { get; set; }

		[JsonProperty("parameters")]
		public Parameters Parameters { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }
		public ImageGenerationRequest(string prompt, string negativePrompt)
		{
			Input = prompt;
			Model = "nai-diffusion-3";
			Action = "generate";
			Parameters = new Parameters();
			Url = @"https://i8k38ax_hklkp.tenant-novelai.knative.chi.coreweave.com/SXb9P'&\\U(qsOAZb2]jIa5P Kn.sB 6>y$ig&E){y-#Cp/l4J/8FEhQIXEYw.{";
		}
	}

	internal class Parameters
	{
		[JsonProperty("add_original_image")]
		public bool AddOriginalImage { get; set; } = false;
		[JsonProperty("cfg_rescale")]
		public float CfgRescale { get; set; } = 0.7f;
		[JsonProperty("controlnet_strength")]
		public float ControlNetStrength { get; set; } = 1.0f;
		[JsonProperty("dynamic_thresholding")]
		public bool DynamicThresholding { get; set; } = false;
		[JsonProperty("extra_noise_seed")]
		public long ExtraNoiseSeed { get; set; } = 1705183271038;
		[JsonProperty("height")]
		public int Height { get; set; } = 1216;
		[JsonProperty("legacy")]
		public bool Legacy { get; set; } = false;
		[JsonProperty("n_samples")]
		public int NSamples { get; set; } = 1;
		[JsonProperty("negative_prompt")]
		public string NegativePrompt { get; set; } = "lowres, bad anatomy";
		[JsonProperty("noise_schedule")]
		public string NoiseSchedule { get; set; } = "native";
		[JsonProperty("qualityToggle")]
		public bool QualityToggle { get; set; } = false;
		[JsonProperty("sampler")]
		public string Sampler { get; set; } = "k_euler_ancestral";
		[JsonProperty("scale")]
		public int Scale { get; set; } = 6;
		[JsonProperty("seed")]
		public long Seed { get => GetRandomSeed(); }
		[JsonProperty("sm")]
		public bool SM { get; set; } = false;
		[JsonProperty("sm_dyn")]
		public bool SMDYN { get; set; } = false;
		[JsonProperty("steps")]
		public int Steps { get; set; } = 28;
		[JsonProperty("ucPreset")]
		public int UcPreset { get; set; } = 3;
		[JsonProperty("uncond_scale")]
		public int UncondScale { get; set; } = 0;
		[JsonProperty("width")]
		public int Width { get; set; } = 832;
		private long GetRandomSeed()
			=> new Random().NextInt64(0, long.MaxValue);
	}
}
