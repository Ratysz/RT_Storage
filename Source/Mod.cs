using System.Reflection;
using Harmony;
using Verse;

namespace RT_Storage
{
	class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			var harmony = HarmonyInstance.Create("io.github.ratysz.rt_storage");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}