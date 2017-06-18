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
	[HarmonyPatch(typeof(ReservationManager))]
	[HarmonyPatch("Reserve")]
	static class Patch_Reserve
	{
		static bool Prefix(ref bool __result, Pawn claimant, LocalTargetInfo target)
		{
			var cell = target.Cell;
			if (cell != null)
			{
				Comp_StorageInput comp = cell.GetStorageComponent<Comp_StorageInput>(claimant.Map);
				if (comp != null)
				{
					Thing thing;
					if (claimant.CurJob.targetA == target)
					{
						thing = claimant.CurJob.targetB.Thing;
					}
					else
					{
						thing = claimant.CurJob.targetA.Thing;
					}
					if (thing != null && comp.Reserve(claimant, thing))
					{
						__result = true;
						return false;
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ReservationManager))]
	[HarmonyPatch("ReleaseAllClaimedBy")]
	static class Patches_ReleaseAllClaimedBy
	{
		static void Postfix(Pawn claimant)
		{
			claimant?.Map?.GetStorageCoordinator().Notify_ReservationsCleared(claimant);
		}
	}

	/*[HarmonyPatch(typeof(ReservationManager))]
	[HarmonyPatch("ReleaseAllForTarget")]
	static class Patches_ReleaseAllForTarget
	{
		static void Postfix(Thing t)
		{
			t?.Map?.GetStorageCoordinator().Notify_ReservationsCleared(t);
		}
	}*/

	/*[HarmonyPatch(typeof(ReservationManager))]
	[HarmonyPatch("Release")]
	static class Patches_Release
	{
		static void Postfix(LocalTargetInfo target, Pawn claimant)
		{
			Thing thing = target.Thing;
			if (thing != null)
			{
				thing?.Map?.GetStorageCoordinator().Notify_ReservationsCleared(thing);
			}
		}
	}*/
}
