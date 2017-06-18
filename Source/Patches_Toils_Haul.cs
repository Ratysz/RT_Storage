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
	[HarmonyPatch]
	static class Patch_CheckForGetOpportunityDuplicate_initAction
	{
		static Type type = typeof(Toils_Haul).GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("CheckForGetOpportunityDuplicate"));
		static FieldInfo[] captureFields = type.GetFields(AccessTools.all);
		static MethodBase TargetMethod()
		{
			return type.GetMethods(AccessTools.all)
				.FirstOrDefault(method => method.ReturnType == typeof(void));
		}
		static FieldInfo toilField = captureFields
			 .FirstOrDefault(field => field.FieldType == typeof(Toil));
		static FieldInfo haulableIndField = captureFields
			 .FirstOrDefault(field => field.Name.Contains("haulableInd"));

		static bool Prefix(object __instance, out object __state)
		{
			Toil toil = __instance.Get<Toil>(toilField);
			TargetIndex haulableInd = __instance.Get<TargetIndex>(haulableIndField);
			__state = new object[] { toil, haulableInd, toil.actor.CurJob.GetTarget(haulableInd).Thing };
			return true;
		}

		static void Postfix(object __instance, object __state)
		{
			object[] args = (object[])__state;
			Toil toil = (Toil)args[0];
			TargetIndex haulableInd = (TargetIndex)args[1];
			Thing haulable = (Thing)args[2];
			if (toil != null && haulableInd != TargetIndex.None && haulable != null)
			{
				Thing toilThing = toil.actor.CurJob.GetTarget(haulableInd).Thing;
				if (toilThing != haulable)
				{
					toil.actor.Map.GetStorageCoordinator().Notify_OpportunityHaul(toil.actor, toilThing.stackCount);
				}
			}
		}
	}
}
