using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelAIBot
{
	public class Directories
	{
		public static string AppData
		{
			get
			{
				switch (Environment.OSVersion.Platform)
				{
					case PlatformID.Win32NT:
						return $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}Kyaru{Path.DirectorySeparatorChar}";

					//TODO: Add config location flag
					case PlatformID.Unix:
						if (ConfigPathOverride == null)
							return $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}.config{Path.DirectorySeparatorChar}";
						else return $"{ConfigPathOverride}.config{Path.DirectorySeparatorChar}";
					default: throw new PlatformNotSupportedException("Operating system not supported");
				}
			}
		}
		public static string Config
		{
			get
			{
				return $"{AppData}config.json";
			}
		}

		public static string ConfigPathOverride
		{
			get => configPathOverride;
			set
			{
				if (!value.EndsWith(Path.DirectorySeparatorChar))
					value += Path.DirectorySeparatorChar;
				configPathOverride = value;
			}
		}
		static string configPathOverride = null;
	}
}
