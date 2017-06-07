using Harmony;
using RimWorld;

namespace RT_Storage
{
	[HarmonyPatch(typeof(SlotGroupManager))]
	[HarmonyPatch("CompareSlotGroupPrioritiesDescending")]
	static class Patch_CompareSlotGroupPrioritiesDescending
	{
		static bool Prefix(ref int __result, SlotGroup a, SlotGroup b)
		{
			__result = ((float)(b.Settings.Priority) + (b.HasStorageInputs() ? 0.5f : 0.0f))
				.CompareTo((float)(a.Settings.Priority) + (a.HasStorageInputs() ? 0.5f : 0.0f));
			return false;
		}
	}

	[HarmonyPatch(typeof(SlotGroup))]
	[HarmonyPatch("Notify_ParentDestroying")]
	static class Patch_Notify_ParentDestroying
	{
		static void Postfix(SlotGroup __instance)
		{
			__instance.parent.Map.GetStorageCoordinator().Notify_SlotGroupDestroyed(__instance);
		}
	}
}
