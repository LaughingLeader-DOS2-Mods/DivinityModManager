using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DivinityModManager.Models
{
	public interface ISelectable
	{
		bool IsSelected { get; set; }
		Visibility Visibility { get; set; }
		bool CanDrag { get; }
	}
}
