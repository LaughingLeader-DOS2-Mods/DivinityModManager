using Alphaleonis.Win32.Filesystem;

using DivinityModManager.Extensions;
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
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace DivinityModManager.Models
{
	[ScreenReaderHelper(Name = "DisplayName", HelpText = "HelpText")]
	public class DivinityGameMasterCampaign : DivinityBaseModData
	{
		public Resource MetaResource { get; set; }

		public List<DivinityModDependencyData> Dependencies = new List<DivinityModDependencyData>();

		public bool Export(IEnumerable<DivinityModData> order)
		{
			try
			{
				if (MetaResource.TryFindNode("Dependencies", out var dependenciesNode))
				{
					if (dependenciesNode.Children.TryGetValue("ModuleShortDesc", out var nodeList))
					{
						nodeList.Clear();
						foreach (var m in order)
						{
							var attributes = new Dictionary<string, NodeAttribute>()
							{
								{ "UUID", new NodeAttribute(NodeAttribute.DataType.DT_FixedString) {Value = m.UUID}},
								{ "Name", new NodeAttribute(NodeAttribute.DataType.DT_FixedString) {Value = m.Name}},
								{ "Version", new NodeAttribute(NodeAttribute.DataType.DT_Int) {Value = m.Version.VersionInt}},
								{ "MD5", new NodeAttribute(NodeAttribute.DataType.DT_LSString) {Value = m.MD5}},
								{ "Folder", new NodeAttribute(NodeAttribute.DataType.DT_LSWString) {Value = m.Folder}},
							};
							var modNode = new Node()
							{
								Name = "ModuleShortDesc",
								Parent = dependenciesNode,
								Attributes = attributes,
								Children = new Dictionary<string, List<Node>>()
							};
							dependenciesNode.AppendChild(modNode);
							//nodeList.Add(modNode);
						}
						ResourceUtils.SaveResource(MetaResource, FilePath);
						return true;
					}
				}
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"Error saving GM Campaign meta.lsf:\n{ex}");
			}
			return false;
		}

		public DivinityGameMasterCampaign() : base()
		{
			
		}
	}
}
