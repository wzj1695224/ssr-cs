using System;
using System.Collections.Generic;


namespace Shadowsocks.Core
{

	internal class ServiceStore
	{
		internal static Dictionary<Type, object> Objects = new Dictionary<Type, object>();


		internal static T Get<T>(Type type)
		{
			return (T)(Objects.TryGetValue(type, out var value) ? value : null);
		}
	}




	internal class ServiceManager
	{
		public static void Register(Type type, object obj)
		{
			// check type
			if ( !type.IsInstanceOfType(obj) )
				throw new Exception($"{obj} is not instance of {type}");

			// only one instance
			var old = ServiceStore.Get<object>(type);
			if ( old != null )
				throw new Exception($"{type} has already registered by {old}");

			ServiceStore.Objects.Add(type, obj);
		}


		public IMenuClickService MenuClickService => ServiceStore.Get<IMenuClickService>(typeof(IMenuClickService));
	}

}
