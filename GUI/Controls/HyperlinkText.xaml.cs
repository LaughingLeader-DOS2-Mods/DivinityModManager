using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DivinityModManager.Controls
{
	/// <summary>
	/// Interaction logic for HyperlinkText.xaml
	/// </summary>
	public partial class HyperlinkText : TextBlock
	{
		public string URL
		{
			get { return (string)GetValue(URLProperty); }
			set { SetValue(URLProperty, value); }
		}

		// Using a DependencyProperty as the backing store for URL.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty URLProperty =
			DependencyProperty.Register("URL", typeof(string), typeof(HyperlinkText), new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnURLChanged)));

		private static void OnURLChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			string url = (string)e.NewValue;
			Uri uri = null;
			if(Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
			{
				HyperlinkText x = (HyperlinkText)d;
				x.Hyperlink.NavigateUri = uri;
				x.ToolTip = url;
				if(String.IsNullOrEmpty(x.DisplayText) || x.UseUrlForDisplayText)
				{
					x.DisplayText = url;
				}
			}
		}

		public string DisplayText
		{
			get { return (string)GetValue(DisplayTextProperty); }
			set { SetValue(DisplayTextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DisplayText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DisplayTextProperty =
			DependencyProperty.Register("DisplayText", typeof(string), typeof(HyperlinkText), new PropertyMetadata(""));

		public bool UseUrlForDisplayText
		{
			get { return (bool)GetValue(UseUrlForDisplayTextProperty); }
			set { SetValue(UseUrlForDisplayTextProperty, value); }
		}

		public static readonly DependencyProperty UseUrlForDisplayTextProperty =
			DependencyProperty.Register("UseUrlForDisplayText", typeof(bool), typeof(HyperlinkText), new PropertyMetadata(false));

		public HyperlinkText()
		{
			InitializeComponent();

			Hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
	}
}
