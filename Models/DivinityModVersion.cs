using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
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

		public string Version
		{
			get { return version; }
			set
			{
				this.RaiseAndSetIfChanged(ref version, value);
			}
		}

		private int versionInt;

		public int VersionInt
		{
			get { return versionInt; }
			set
			{
				this.RaiseAndSetIfChanged(ref versionInt, value);
			}
		}

		private void UpdateVersion()
		{
			Version = String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);
		}

		public int ToInt()
		{
			return (Major << 28) + (Minor << 24) + (Revision << 16) + (Build);
		}

		public override string ToString()
		{
			return String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);
		}

		public void ParseInt(int vInt)
		{
			VersionInt = vInt;
			major = (VersionInt >> 28);
			minor = (VersionInt >> 24) & 0x0F;
			revision = (VersionInt >> 16) & 0xFF;
			build = (VersionInt & 0xFFFF);
			UpdateVersion();
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
	}
}
