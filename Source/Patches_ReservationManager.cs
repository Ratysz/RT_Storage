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
	[HarmonyPatch("CanReserve")]
	static class Patch_CanReserve
	{
		static bool Prefix(ref bool __result, Pawn claimant, LocalTargetInfo target)
		{
			var cell = target.Cell;
			if (cell != null)
			{
				Comp_StorageInput comp = cell.GetStorageComponent<Comp_StorageInput>(claimant.Map);
				if (comp != null)
				{
					__result = true;
					return false;
				}
			}
			return true;
		}
	}

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
					__result = true;
					return false;
				}
			}
			return true;
		}
	}
}
