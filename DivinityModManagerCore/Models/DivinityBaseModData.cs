using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Util;

using DynamicData;
using DynamicData.Binding;

using LSLib.LS;

using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace DivinityModManager.Models
{
	public interface IDivinityModData
	{
		string UUID { get; set; }
		string Name { get; set; }
		string Folder { get; set; }
		string MD5 { get; set; }
		DivinityModVersion Version { get; set; }
	}

	[DataContract]
	public class DivinityBaseModData : ReactiveObject, IDivinityModData
	{
		[Reactive] public string FilePath { get; set; }

		readonly ObservableAsPropertyHelper<string> fileName;
		public string FileName => fileName.Value;
		[Reactive] [DataMember] public string UUID { get; set; }
		[Reactive] [DataMember] public string Folder { get; set; }
		[Reactive] [DataMember] public string Name { get; set; }

		readonly ObservableAsPropertyHelper<string> displayName;
		public string DisplayName => displayName.Value;
		[Reactive] [DataMember] public string Description { get; set; }
		[Reactive] [DataMember] public string Author { get; set; }
		[Reactive] public string MD5 { get; set; }
		[Reactive] [DataMember] public DivinityModVersion Version { get; set; }
		[Reactive] public DivinityModVersion HeaderVersion { get; set; }
		[Reactive] public DivinityModVersion PublishVersion { get; set; }
		[Reactive] public DateTime? LastModified { get; set; }

		[Reactive] public bool DisplayFileForName { get; set; } = false;
		[Reactive] public bool IsHidden { get; set; } = false;
		[Reactive] public bool IsLarianMod { get; set; } = false;
		[Reactive] public bool IsClassicMod { get; set; } = false;
		[Reactive] public string HelpText { get; set; } = "";

		public List<string> Tags { get; set; } = new List<string>();

		[Reactive] public Visibility Visibility { get; set; } = Visibility.Visible;

		readonly ObservableAsPropertyHelper<Visibility> descriptionVisibility;
		public Visibility DescriptionVisibility => descriptionVisibility.Value;

		readonly ObservableAsPropertyHelper<Visibility> authorVisibility;
		public Visibility AuthorVisibility => authorVisibility.Value;

		public virtual string GetDisplayName()
		{
			return !DisplayFileForName ? Name : FileName;
		}

		public virtual string GetHelpText()
		{
			return "";
		}

		public void AddTag(string tag)
		{
			if (!String.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
			{
				Tags.Add(tag);
				Tags.Sort((x, y) => string.Compare(x, y, true));
			}
		}

		public void AddTags(IEnumerable<string> tags)
		{
			if (tags == null)
			{
				return;
			}
			bool addedTags = false;
			foreach (var tag in tags)
			{
				if (!String.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
				{
					Tags.Add(tag);
					addedTags = true;
				}
			}
			Tags.Sort((x, y) => string.Compare(x, y, true));
			if (addedTags)
			{
				this.RaisePropertyChanged("Tags");
			}
		}

		public DivinityBaseModData()
		{
			fileName = this.WhenAnyValue(x => x.FilePath).Select(f => Path.GetFileName(f)).ToProperty(this, nameof(FileName));
			displayName = this.WhenAnyValue(x => x.Name, x => x.FilePath, x => x.DisplayFileForName).Select(x => this.GetDisplayName()).ToProperty(this, nameof(DisplayName));
			descriptionVisibility = this.WhenAnyValue(x => x.Description).Select(x => !String.IsNullOrWhiteSpace(x) ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Visible).ToProperty(this, nameof(DescriptionVisibility));
			authorVisibility = this.WhenAnyValue(x => x.Author).Select(x => !String.IsNullOrWhiteSpace(x) ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Visible).ToProperty(this, nameof(AuthorVisibility));
		}
	}
}
