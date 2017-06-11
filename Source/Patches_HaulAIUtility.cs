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
	[HarmonyPatch(typeof(HaulAIUtility))]
	[HarmonyPatch("HaulMaxNumToCellJob")]
	static class Patch_HaulMaxNumToCellJob
	{
		static void Postfix(ref Job __result, Pawn p, Thing t, IntVec3 storeCell)
		{
			Comp_StorageInput comp = storeCell.GetStorageComponent<Comp_StorageInput>(p.Map);
			if (comp != null)
			{
				__result.count = comp.CanAccept(t);
			}
		}
	}
}
