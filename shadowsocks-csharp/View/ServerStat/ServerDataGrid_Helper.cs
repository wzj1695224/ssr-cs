using System.Drawing;
using System.Windows.Forms;


namespace Shadowsocks.View
{
	public partial class ServerLogForm
	{
		private bool _tableChanged = false;


		private bool SetCellBackColor(DataGridViewCell cell, Color newColor)
		{
			if (cell.Style.BackColor == newColor) return false;

			cell.Style.BackColor = newColor;

			_tableChanged = true;
			return true;
		}


		private bool SetCellToolTipText(DataGridViewCell cell, string newString)
		{
			if (cell.ToolTipText == newString) return false;

			cell.ToolTipText = newString;

			_tableChanged = true;
			return true;
		}


		private bool SetCellText(DataGridViewCell cell, string value)
		{
			if ((string)cell.Value == value) return false;

			cell.Value = value;

			_tableChanged = true;
			return true;
		}


		private bool SetCellText(DataGridViewCell cell, long value)
		{
			if ((string)cell.Value == value.ToString()) return false;

			cell.Value = value.ToString();

			_tableChanged = true;
			return true;
		}
	}
}