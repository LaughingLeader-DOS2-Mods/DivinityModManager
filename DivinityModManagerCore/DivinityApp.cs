using DivinityModManager.Models;
using DivinityModManager.Util;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager
{
	public static class DivinityApp
	{
		public const string DIR_DATA = "Data\\";
		public const string URL_REPO = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager";
		public const string URL_CHANGELOG = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/blob/master/CHANGELOG.md";
		public const string URL_CHANGELOG_RAW = @"https://raw.githubusercontent.com/LaughingLeader-DOS2-Mods/DivinityModManager/master/CHANGELOG.md";
		public const string URL_UPDATE = @"https://raw.githubusercontent.com/LaughingLeader-DOS2-Mods/DivinityModManager/master/Update.xml";
		public const string URL_AUTHOR = @"https://github.com/LaughingLeader";
		public const string URL_ISSUES = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/issues";
		public const string URL_LICENSE = @"https://github.com/LaughingLeader-DOS2-Mods/DivinityModManager/blob/master/LICENSE";
		public const string URL_DONATION = @"https://ko-fi.com/laughingleader";

		public const string XML_MOD_ORDER_MODULE = @"<node id=""Module""><attribute id=""UUID"" value=""{0}"" type=""22""/></node>";
		public const string XML_MODULE_SHORT_DESC = @"<node id=""ModuleShortDesc""><attribute id=""Folder"" value=""{0}"" type=""30""/><attribute id=""MD5"" value=""{1}"" type=""23""/><attribute id=""Name"" value=""{2}"" type=""22""/><attribute id=""UUID"" value=""{3}"" type=""22"" /><attribute id=""Version"" value=""{4}"" type=""4""/></node>";
		public const string XML_MOD_SETTINGS_TEMPLATE = @"<?xml version=""1.0"" encoding=""UTF-8""?><save><header version=""2""/><version major=""3"" minor=""6"" revision=""4"" build=""0""/><region id=""ModuleSettings""><node id=""root""><children><node id=""ModOrder""><children>{0}</children></node><node id=""Mods""><children>{1}</children></node></children></node></region></save>";

		public static DivinityModData MOD_Origins { get; private set; } = new DivinityModData(true) { Name = "Divinity: Original Sin 2", UUID = "1301db3d-1f54-4e98-9be5-5094030916e4", Folder = "DivinityOrigins_1301db3d-1f54-4e98-9be5-5094030916e4", Version = DivinityModVersion.FromInt(372645092), Type = "Adventure", Targets = "Story", Author = "Larian Studios", Description = "The main campaign of Divinity: Original Sin 2.", MD5 = "", IsLarianMod = true, HasDescription=true};
		public static DivinityModData MOD_Shared { get; private set; } = new DivinityModData(true) { Name = "Shared", UUID = "2bd9bdbe-22ae-4aa2-9c93-205880fc6564", Folder = "Shared", Version = DivinityModVersion.FromInt(389028377), Type = "Adventure", Targets = "Story", Author = "Larian Studios", Description = "", MD5 = "", IsLarianMod = true, IsHidden = true };
		public static DivinityModData MOD_Shared_DOS { get; private set; } = new DivinityModData(true) { Name = "Shared_DOS", UUID = "eedf7638-36ff-4f26-a50a-076b87d53ba0", Folder = "Shared_DOS", Version = DivinityModVersion.FromInt(268435456), Type = "Adventure", Targets = "Story", Author = "Larian Studios", Description = "", MD5 = "", IsLarianMod = true, IsHidden = true };
		public static DivinityModData MOD_Character_Creation_Pack { get; private set; } = new DivinityModData(true) { Name = "Character_Creation_Pack", UUID = "b40e443e-badd-4727-82b3-f88a170c4db7", Folder = "Character_Creation_Pack", Version = DivinityModVersion.FromInt(268435456), Type = "Add-on", Targets = "Story", Author = "Larian Studios", Description = "", MD5 = "" , IsLarianMod = true};
		public static DivinityModData MOD_GameMaster { get; private set; } = new DivinityModData(true) { Name = "Game Master", UUID = "00550ab2-ac92-410c-8d94-742f7629de0e", Folder = "GameMaster", Version = DivinityModVersion.FromInt(271587865), Type = "Adventure", Targets = "Story", Author = "Larian Studios", Description = "GM mode is for when you want a highly curated adventure, much like a tradition tabletop game. Multiplayer required.", MD5 = "a81ffa30bfb55ccddbdc37256bc6f7f4" , IsLarianMod = true, HasDescription = true };
		public static DivinityModData MOD_Arena { get; private set; } = new DivinityModData(true) { Name = "Arena", UUID = "a99afe76-e1b0-43a1-98c2-0fd1448c223b", Folder = "DOS2_Arena", Version = DivinityModVersion.FromInt(271981796), Type = "Adventure", Targets = "Story", Author = "Larian Studios", Description = "A PvP mode designed for quick battles.", MD5 = "ba12b04eb34b2bcac60bb3edcceb7c5e" , IsLarianMod = true, HasDescription = true };

		public static List<DivinityModData> MODS_Base { get; private set; } = new List<DivinityModData>()
		{
			MOD_Origins,
			MOD_Shared,
			MOD_Shared_DOS,
			MOD_Character_Creation_Pack,
		};

		public static List<DivinityModData> MODS_GameModes { get; private set; } = new List<DivinityModData>()
		{
			MOD_GameMaster,
			MOD_Arena
		};

		public static List<DivinityModData> MODS_GiftBag { get; private set; } = new List<DivinityModData>()
		{
			new DivinityModData(true){ Name = "Nine Lives", UUID = "015de505-6e7f-460c-844c-395de6c2ce34", Folder="AS_BlackCatPlus", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Transforms the Black Cat into a follower. Once rescued, a whistle will appear in your inventory. You can use this if your cat gets lost, or if you want to change who it follows.<br><br>Note that this is a persistent mod that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Herb Gardens", UUID = "38608c30-1658-4f6a-8adf-e826a5295808", Folder="AS_GrowYourHerbs", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Plant your own herb garden! Take any herb and combine it with a bucket to create a seedling. Then, just place your seedling in the ground and watch it grow.<br><br>Note that this is a persistent modification that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Source Meditation", UUID = "1273be96-6a1b-4da9-b377-249b98dc4b7e", Folder="AS_RestRestorePoints", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Restores Source Points whenever a player character rests.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "From the Ashes", UUID = "af4b3f9c-c5cb-438d-91ae-08c5804c1983", Folder="AS_Resturrect", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Never underestimate the power of a good sleep! Simply use the bedroll at any non-combat time to resurrect your dead allies.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Endless Runner", UUID = "ec27251d-acc0-4ab8-920e-dbc851e79bb4", Folder="AS_ToggleSpeedAddon", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Find a new icon in your Hotbar which you can use to toggle sprint on and off. Sprint increases your movement speed and the movement speed of your followers.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "8 Action Points", UUID = "9b45f7e5-d4e2-4fc2-8ef7-3b8e90a5256c", Folder="CMP_8AP_Kamil", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Increases the base maximum Action Points of hero characters to 8.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Hagglers", UUID = "f33ded5d-23ab-4f0c-b71e-1aff68eee2cd", Folder="CMP_BarterTweaks", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Let the reputation and skill of your whole party help when you want to haggle for the best prices!", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Crafter's Kit", UUID = "68a99fef-d125-4ed0-893f-bb6751e52c5e", Folder="CMP_CraftingOverhaul", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Plenty of new recipes and unique items to craft!<br><br>Note that this is a persistent modification that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Divine Talents", UUID = "ca32a698-d63e-4d20-92a7-dd83cba7bc56", Folder="CMP_DivineTalents_Kamil", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="New talents! Have you ever wanted to resurrect your allies as zombies? Do you hate missing the one attack that really matters? This mod contains remedies to those problems and many more!<br><br>Note that this is a persistent mod that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Combat Randomiser", UUID = "f30953bb-10d3-4ba4-958c-0f38d4906195", Folder="CMP_EnemyRandomizer_Kamil", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="When entering combat, one or more random enemies will receive one of the new special statuses created for this mod. This will change everything you thought you knew about combat!<br><br>Note that this is a persistent mod that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Animal Empathy", UUID = "423fae51-61e3-469a-9c1f-8ad3fd349f02", Folder="CMP_Free_PetPalTag_Kamil", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Allows all player characters to talk to animals without having to spend a talent point. Also changes Pet Pal talent to grant maximum positive attitude in all conversations with animals.<br><br>Note that this is a persistent modification that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Fort Joy Magic Mirror", UUID = "2d42113c-681a-47b6-96a1-d90b3b1b07d3", Folder="CMP_FTJRespec_Kamil", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Manifests a Magic Mirror in the Arena of Fort Joy, along with a new Character Creation level. This allows you to respec before moving on to the next act of the game.<br><br>Note that this is a persistent modification that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Enhanced Spirit Vision", UUID = "8fe1719c-ef8f-4cb7-84bd-5a474ff7b6c1", Folder="CMP_InfiniteSpiritVision", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Increases the active radius of Spirit Vision and makes it last indefinitely.<br><br>Note that this is a persistent mod that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Sourcerous Sundries", UUID = "a945eefa-530c-4bca-a29c-a51450f8e181", Folder="CMP_LevelUpEquipment", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="In each major hub, you can now find a mysterious vendor selling exotic and potent artefacts. These artefacts can upgrade a character's own gear with immense power, bringing them up to the player's current level.<br><br>Note that this is a persistent modification that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Improved Organisation", UUID = "f243c84f-9322-43ac-96b7-7504f990a8f0", Folder="CMP_OrganizedContainers_Marek", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Find a collection of special bags that allow you to better (and automatically) organise your inventory.<br><br>Note that this is a persistent mod that cannot be turned off once activated.", MD5="", IsLarianMod = true},
			new DivinityModData(true){ Name = "Pet Power", UUID = "d2507d43-efce-48b8-ba5e-5dd136c715a7", Folder="CMP_SummoningImproved_Kamil", Version=DivinityModVersion.FromInt(268435456), Type="Add-on", Targets="Story", Author="Larian", Description="Pet Power enhances the summoning class and its infusion spells immensely. With this mod, you can cast infusion spells on all available summons, not just your own Incarnate! Each summon receives different skills depending on the base elemental infusion type.<br><br>Note that this is a persistent modification that cannot be turned off once activated.", MD5="", IsLarianMod = true},
		};

		public static List<DivinityModData> MODS_Larian_All { get; private set; } = new List<DivinityModData>()
		{
			MOD_Origins,
			MOD_Shared,
			MOD_Shared_DOS,
			MOD_Character_Creation_Pack,
			MOD_GameMaster,
			MOD_Arena
		}.Concat(MODS_GiftBag).ToList();

		public static List<DivinityModData> MODS_Larian_IgnoreDependencies { get; private set; } = new List<DivinityModData>()
		{
			MOD_Origins,
			MOD_Shared,
			MOD_Shared_DOS,
			MOD_Character_Creation_Pack,
			MOD_GameMaster,
			MOD_Arena
		};

		public static List<DivinityModData> GetIgnoredMods(bool all = false)
		{
			var mods = new List<DivinityModData>();
			if (all)
			{
				mods.AddRange(MODS_Larian_All);
			}
			else
			{
				mods.AddRange(MODS_Base);
				mods.AddRange(MODS_GameModes);
			}
			return mods;
		}

		public static List<DivinityModData> IgnoredEditorMods { get; set; } = MODS_Larian_All.ToList();

		// Hide Larian mods for now, since we can't add them to the active order without the game automatically removing them
		public static List<DivinityModData> IgnoredMods { get; set; } = MODS_Larian_All.ToList();

		public static DivinityGlobalCommands Commands { get; private set; } = new DivinityGlobalCommands();
		public static DivinityGlobalEvents Events { get; private set; } = new DivinityGlobalEvents();

		public static event PropertyChangedEventHandler StaticPropertyChanged;

		private static void NotifyStaticPropertyChanged([CallerMemberName] string name = null)
		{
			StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
		}

		private static bool developerModeEnabled = false;

		public static bool DeveloperModeEnabled
		{
			get => developerModeEnabled;
			set 
			{ 
				developerModeEnabled = value;
				NotifyStaticPropertyChanged();
			}
		}

		public static IObservable<Func<DivinityModDependencyData, bool>> DependencyFilter { get; set; }
	}
}
