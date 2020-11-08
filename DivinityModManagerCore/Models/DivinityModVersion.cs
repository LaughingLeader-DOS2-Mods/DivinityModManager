using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	[JsonObject(MemberSerialization.OptIn)]
	public class DivinityModVersion : ReactiveObject
	{
		private int major = 0;

		public int Major
		{
			get { return major; }
			set
			{
				this.RaiseAndSetIfChanged(ref major, value);
				UpdateVersion();
			}
		}

		private int minor = 0;

		public int Minor
		{
			get { return minor; }
			set
			{
				this.RaiseAndSetIfChanged(ref minor, value);
				UpdateVersion();
			}
		}

		private int revision = 0;

		public int Revision
		{
			get { return revision; }
			set
			{
				this.RaiseAndSetIfChanged(ref revision, value);
				UpdateVersion();
			}
		}

		private int build = 0;

		public int Build
		{
			get { return build; }
			set
			{
				this.RaiseAndSetIfChanged(ref build, value);
				UpdateVersion();
			}
		}

		private string version;

		[JsonProperty]
		public string Version
		{
			get { return version; }
			set
			{
				this.RaiseAndSetIfChanged(ref version, value);
			}
		}

		private int versionInt = 0;

		[JsonProperty]
		public int VersionInt
		{
			get { return versionInt; }
			set
			{
				value = Math.Max(Int32.MinValue, Math.Min(value, Int32.MaxValue));
				if(versionInt != value)
				{
					ParseInt(versionInt);
					this.RaisePropertyChanged("VersionInt");
				}
			}
		}

		private void UpdateVersion()
		{
			Version = String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);
			var nextVersion = ToInt();
			if(nextVersion != versionInt)
			{
				versionInt = ToInt();
				this.RaisePropertyChanged("VersionInt");
			}
		}

		public int ToInt()
		{
			return (Major << 28) + (Minor << 24) + (Revision << 16) + (Build);
		}

		public override string ToString()
		{
			return String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);
		}

		public void ParseInt(int vInt, bool update=true)
		{
			if(versionInt != vInt)
			{
				versionInt = vInt;
				this.RaisePropertyChanged("VersionInt");
			}
			major = (versionInt >> 28);
			minor = (versionInt >> 24) & 0x0F;
			revision = (versionInt >> 16) & 0xFF;
			build = (versionInt & 0xFFFF);
			if (update)
			{
				UpdateVersion();
			}
			this.RaisePropertyChanged("Major");
			this.RaisePropertyChanged("Minor");
			this.RaisePropertyChanged("Revision");
			this.RaisePropertyChanged("Build");
		}

		public static DivinityModVersion FromInt(int vInt)
		{
			return new DivinityModVersion(vInt);
		}

		public DivinityModVersion() { }

		public DivinityModVersion(int vInt)
		{
			ParseInt(vInt);
		}

		public DivinityModVersion(int headerMajor, int headerMinor, int headerRevision, int headerBuild)
		{
			Major = headerMajor;
			Minor = headerMinor;
			Revision = headerRevision;
			Build = headerBuild;
		}
	}
}
