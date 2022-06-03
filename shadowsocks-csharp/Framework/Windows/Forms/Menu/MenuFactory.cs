using Shadowsocks.Controller;
using System;
using System.Windows.Forms;


namespace Shadowsocks.Framework.Windows.Forms.Menu
{
	public static class MenuFactory
	{
        public static MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MenuItem(I18N.GetString(text), click);
        }

        public static MenuItem CreateMenuItem(string text, Action click)
        {
            return CreateMenuItem(text, (sender, e) => click());
        }

        public static MenuItem CreateMenuItem<T>(string text, Action<T> action, T param)
        {
            return CreateMenuItem(text, (sender, e) => action(param));
        }


        public static MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MenuItem(I18N.GetString(text), items);
        }
	}
}
