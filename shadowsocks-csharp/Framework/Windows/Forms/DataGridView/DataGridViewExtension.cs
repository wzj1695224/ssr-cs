using System.Windows.Forms;


namespace Shadowsocks.Framework.Windows.Forms
{
	public static class DataGridViewExtension
	{
		public static DataGridView UseDoubleBuffer(this DataGridView self)
		{
			DataGridViewHelper.UseDoubleBuffer(self);
			return self;
		}
	}
}
