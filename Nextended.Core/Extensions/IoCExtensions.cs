namespace Nextended.Core.Extensions
{
	/// <summary>
	/// IoC Erweiterungen
	/// </summary>
	public static class IoCExtensions
	{
		///// <summary>
		///// Aktion als LazyPattern registrieren. 
		///// </summary>
		//public static IUnityContainer RegisterLazy<T>(this IUnityContainer unityContainer, Func<T> injector)
		//{
		//	return unityContainer.RegisterType<T>(new InjectionFactory(uc => injector()));
		//}

		///// <summary>
		///// Aktion als LazyPattern registrieren. 
		///// </summary>
		//public static IUnityContainer RegisterLazy<T>(this IUnityContainer unityContainer, string registrationName, Func<T> injector)
		//{
		//	Check.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(registrationName));
		//	return unityContainer.RegisterType<T>(registrationName, new InjectionFactory(uc => injector()));
		//}
	}
}