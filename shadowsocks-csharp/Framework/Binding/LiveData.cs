using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Shadowsocks.Framework.Binding
{
	public class LiveData<T>
	{
		private T _data;
		private int _ver = 0;

		public T Data
		{
			get => _data;
			set
			{
				_ver++;
				_data = value;
				OnChange?.Invoke(value);
			}
		}

		public event Action<T> OnChange;


		public LiveData(T data)
		{
			_data = data;
		}
	}




}
