using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;
using Verse.AI;

namespace RT_Storage
{
	[HarmonyPatch(typeof(SlotGroupManager))]
	[HarmonyPatch("CompareSlotGroupPrioritiesDescending")]
	static class Patch_CompareSlotGroupPrioritiesDescending
	{
		static bool Prefix(ref int __result, SlotGroup a, SlotGroup b)
		{
			__result = ((float)(b.Settings.Priority) + (b.HasStorageInputCells() ? 0.5f : 0.0f))
				.CompareTo((float)(a.Settings.Priority) + (a.HasStorageInputCells() ? 0.5f : 0.0f));
			return false;
		}
	}

	[HarmonyPatch(typeof(SlotGroupManager))]
	[HarmonyPatch("AddGroup")]
	static class Patch_AddGroup
	{
		static void Postfix(SlotGroup newGroup)
		{
			newGroup.parent.Map.GetStorageCoordinator().Notify_SlotGroupAdded(newGroup);
		}
	}

	[HarmonyPatch(typeof(SlotGroupManager))]
	[HarmonyPatch("RemoveGroup")]
	static class Patch_RemoveGroup
	{
		static void Postfix(SlotGroup oldGroup)
		{
			oldGroup.parent.Map.GetStorageCoordinator().Notify_SlotGroupRemoved(oldGroup);
		}
	}

	[HarmonyPatch(typeof(SlotGroupManager))]
	[HarmonyPatch("SetCellFor")]
	static class Patch_SetCellFor
	{
		static void Postfix(IntVec3 c, SlotGroup group)
		{

		}
	}

	[HarmonyPatch(typeof(SlotGroupManager))]
	[HarmonyPatch("ClearCellFor")]
	static class Patch_ClearCellFor
	{
		static void Postfix(IntVec3 c, SlotGroup group)
		{

		}
	}
}
