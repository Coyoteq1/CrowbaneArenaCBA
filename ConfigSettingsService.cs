using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Stunlock.Core;

namespace CrowbanePackPlugin.Services;
internal class ConfigSettingsService
{
	static bool _logged;
	private static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
	private static readonly string SETTINGS_PATH = Path.Combine(CONFIG_PATH, "settings.json");

	public bool RevealMapToAll {
		get {
			return config.RevealMapToAll;

		}
		set { 
			config.RevealMapToAll = value; 
			SaveConfig();
		}
	}

	public bool HeadgearBloodbound
	{
		get
		{
			return config.HeadgearBloodbound;
		}
		set
		{
			config.HeadgearBloodbound = value;
			SaveConfig();
		}
	}

	public IReadOnlyDictionary<string, bool> BloodBound => config.BloodBound;
	public IReadOnlyDictionary<int, PrisonerFeed> PrisonerFeeds => config.PrisonerFeeds;

	public bool SoulshardsFlightRestricted
	{
		get
		{
			return config.SoulshardsRestricted;
		}
		set
		{
			config.SoulshardsRestricted = value;
			SaveConfig();
		}
	}

	public int ItemDropLifetime
	{
		get
		{
			return config.ItemDropLifetime;
		}
		set
		{
			config.ItemDropLifetime = value;
			SaveConfig();
		}
	}

	public int ItemDropLifetimeWhenDisabled
	{
		get
		{
			return config.ItemDropLifetimeWhenDisabled;
		}
		set
		{
			config.ItemDropLifetimeWhenDisabled = value;
			SaveConfig();
		}
	}

	public int ShardDropLifetime
	{
		get
		{
			return config.ShardDropLifetime;
		}
		set
		{
			config.ShardDropLifetime = value;
			SaveConfig();
		}
	}

	public int? ShardDurabilityTime
	{
		get
		{
			return config.ShardDurabilityTime;
		}
		set
		{
			config.ShardDurabilityTime = value;
			SaveConfig();
		}
	}

	public bool ShardDropManagementEnabled
	{
		get
		{
			return config.ShardDropManagementEnabled;
		}
		set
		{
			config.ShardDropManagementEnabled = value;
			SaveConfig();
		}
	}

	public int ShardDraculaDropLimit
	{
		get
		{
			return config.ShardDraculaDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardDraculaDropLimit = value;
			SaveConfig();
		}
	}

	public int ShardWingedHorrorDropLimit
	{
		get
		{
			return config.ShardWingedHorrorDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardWingedHorrorDropLimit = value;
			SaveConfig();
		}
	}

	public int ShardMonsterDropLimit
	{
		get
		{
			return config.ShardMonsterDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardMonsterDropLimit = value;
			SaveConfig();
		}
	}

	public int ShardMorganaDropLimit
	{
		get
		{
			return config.ShardMorganaDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardMorganaDropLimit = value;
			SaveConfig();
		}
	}

	public int ShardSolarusDropLimit
	{
		get
		{
			return config.ShardSolarusDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardSolarusDropLimit = value;
			SaveConfig();
		}
	}

	public bool EveryoneDaywalker
	{
		get
		{
			return config.EveryoneDaywalker ?? false;
		}
		set
		{
			config.EveryoneDaywalker = value;
			SaveConfig();
		}
	}

	public float GruelMutantChance
	{
		get
		{
			return config.GruelMutantChance ?? 0.35f;
		}
		set
		{
			config.GruelMutantChance = value;
			SaveConfig();
		}
	}

	public float GruelBloodMin
	{
		get
		{
			return config.GruelBloodMin ?? 0.01f;
		}
		set
		{
			config.GruelBloodMin = value;
			SaveConfig();
		}
	}
	public float GruelBloodMax
	{
		get
		{
			return config.GruelBloodMax ?? 0.02f;
		}
		set
		{
			config.GruelBloodMax = value;
			SaveConfig();
		}
	} 
	public PrefabGUID GruelTransform
	{
		get
		{
			return config.GruelTransformPrefabInt.HasValue ? new PrefabGUID(config.GruelTransformPrefabInt.Value) 
				                                  : new PrefabGUID(-1025552087);
		}
		set
		{
			config.GruelTransformPrefabInt = value.GuidHash;
			SaveConfig();
		}
	}

	public bool BatVision
	{
		get
		{
			return config.BatVision;
		}
		set
		{
			config.BatVision = value;
			SaveConfig();
		}
	}

	public void SetBloodBound(string key, bool value)
	{
		config.BloodBound[key] = value;
		SaveConfig();
	}

	public void ClearBloodBound(IEnumerable<string> keys)
	{
		foreach (var key in keys)
		{
			config.BloodBound.Remove(key);
		}

		SaveConfig();
	}

	public void SetPrisonerFeed(int prefabGuid, PrisonerFeed value)
	{
		config.PrisonerFeeds[prefabGuid] = value;
		SaveConfig();
	}

	public void ClearPrisonerFeed(int prefabGuid)
	{
		config.PrisonerFeeds.Remove(prefabGuid);
		SaveConfig();
	}

	public struct PrisonerFeed
	{
		public float HealthChangeMin { get; set; }
		public float HealthChangeMax { get; set; }
		public float MiseryChangeMin { get; set; }
		public float MiseryChangeMax { get; set; }
		public float BloodQualityChangeMin { get; set; }
		public float BloodQualityChangeMax { get; set; }
	}

	struct Config
	{
		public Config()
		{
			BloodBound = new Dictionary<string, bool>();
			SoulshardsRestricted = true;
			ItemDropLifetimeWhenDisabled = 300;
			ShardDropLimit = 1;
			ShardDropManagementEnabled = true;
		}

		public bool RevealMapToAll { get; set; }

				public Dictionary<string, bool> BloodBound { get; set; }
		public bool HeadgearBloodbound { get; set; }
		public bool SoulshardsRestricted { get; set; }
		public int ItemDropLifetime { get; set; }
		public int ItemDropLifetimeWhenDisabled { get; set; }
		public int ShardDropLifetime { get; set; }
		public bool ShardDropManagementEnabled { get; set; }
		public int? ShardDropLimit { get; set; }
		public int? ShardDurabilityTime { get; set; }
		public int? ShardDraculaDropLimit { get; set; }
		public int? ShardWingedHorrorDropLimit { get; set; }
		public int? ShardMonsterDropLimit { get; set; }
		public int? ShardMorganaDropLimit { get; set; }
		public int? ShardSolarusDropLimit { get; set; }
		public bool? EveryoneDaywalker { get; set; }
		public float? GruelMutantChance { get; set; }
		public float? GruelBloodMin { get; set; }
		public float? GruelBloodMax { get; set; }
		public int? GruelTransformPrefabInt { get; set; }
		public bool BatVision { get; set; }
		public Dictionary<int, PrisonerFeed> PrisonerFeeds { get; set; } = new Dictionary<int, PrisonerFeed>();
	}

	Config config;

    public ConfigSettingsService()
    {
        LoadConfig();
        // Suppress verbose startup logging entirely (set once)
        _logged = true;
    }

	void LoadConfig()
	{
		try
		{
			if (!File.Exists(SETTINGS_PATH))
			{
				config = new Config();
				SaveConfig();
				return;
			}

			var json = File.ReadAllText(SETTINGS_PATH);
			var options = new JsonSerializerOptions
			{
				AllowTrailingCommas = true,
				ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
				PropertyNameCaseInsensitive = true
			};
			config = JsonSerializer.Deserialize<Config>(json, options);
			if (config.Equals(default(Config)))
			{
				// Defensive: ensure config isn't left default-initialized
				config = new Config();
			}
		}
		catch (System.Exception e)
		{
			try
			{
				Core.Log.LogWarning($"Failed to load settings.json: {e.Message}");
				// Backup the bad file (best-effort)
				if (File.Exists(SETTINGS_PATH))
				{
					var bak = SETTINGS_PATH + ".bak";
					File.Copy(SETTINGS_PATH, bak, overwrite: true);
				}
			}
			catch { }

			// Fall back to defaults and rewrite a valid file
			config = new Config();
			try { SaveConfig(); } catch { }
		}
	}

	void SaveConfig()
	{
		if(!Directory.Exists(CONFIG_PATH))
			Directory.CreateDirectory(CONFIG_PATH);
		var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
		File.WriteAllText(SETTINGS_PATH, json);
	}
}





