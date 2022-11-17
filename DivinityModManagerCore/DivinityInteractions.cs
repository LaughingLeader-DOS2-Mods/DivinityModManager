using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DivinityModManager
{
	public struct DeleteFilesViewConfirmationData
	{
		public int Total;
		public bool PermanentlyDelete;
		public CancellationToken Token;
	}

	public static class DivinityInteractions
	{
		public static readonly Interaction<DeleteFilesViewConfirmationData, bool> ConfirmModDeletion = new Interaction<DeleteFilesViewConfirmationData, bool>();
	}
}
