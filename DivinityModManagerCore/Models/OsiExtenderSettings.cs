using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models
{
	[DataContract]
	public class OsiExtenderSettings : ReactiveObject
	{
		private bool extenderIsAvailable = false;

		public bool ExtenderIsAvailable
		{
			get => extenderIsAvailable;
			set { this.RaiseAndSetIfChanged(ref extenderIsAvailable, value); }
		}

		private int extenderVersion = -1;

		public int ExtenderVersion
		{
			get => extenderVersion;
			set { this.RaiseAndSetIfChanged(ref extenderVersion, value); }
		}

		private bool enableExtensions = true;

		[DataMember]
		public bool EnableExtensions
		{
			get => enableExtensions;
			set { this.RaiseAndSetIfChanged(ref enableExtensions, value); }
		}

		private bool createConsole = false;

		[DataMember]
		public bool CreateConsole
		{
			get => createConsole;
			set { this.RaiseAndSetIfChanged(ref createConsole, value); }
		}

		private bool enableLogging = false;

		[DataMember]
		public bool EnableLogging
		{
			get => enableLogging;
			set { this.RaiseAndSetIfChanged(ref enableLogging, value); }
		}

		private bool logCompile;

		[DataMember]
		public bool LogCompile
		{
			get => logCompile;
			set { this.RaiseAndSetIfChanged(ref logCompile, value); }
		}

		private string logDirectory;

		[DataMember]
		public string LogDirectory
		{
			get => logDirectory;
			set { this.RaiseAndSetIfChanged(ref logDirectory, value); }
		}
	}
}
