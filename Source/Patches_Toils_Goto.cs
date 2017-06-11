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
	static class Patch_GotoThing_InitAction
	{
		static MethodBase TargetMethod()
		{
			return typeof(Toils_Goto)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("4F4"))
				.GetMethods(AccessTools.all)
				.FirstOrDefault(method => method.ReturnType == typeof(void));
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo markerField = AccessTools.Field(typeof(Pawn), nameof(Pawn.pather));
			MethodInfo markerMethod = AccessTools.Method(typeof(Job), nameof(Job.GetTarget));
			MethodInfo sneakyMethod = AccessTools.Method(
				typeof(Patch_GotoThing_InitAction),
				nameof(Patch_GotoThing_InitAction.ClosestOutputOrPosition));
			FieldInfo toilField = typeof(Toils_Goto)
				 .GetNestedTypes(AccessTools.all)
				 .FirstOrDefault(type => type.FullName.Contains("4F4"))
				 .GetFields(AccessTools.all)
				 .FirstOrDefault(field => field.FieldType == typeof(Toil));
			FieldInfo targetIndexField = typeof(Toils_Goto)
				 .GetNestedTypes(AccessTools.all)
				 .FirstOrDefault(type => type.FullName.Contains("4F4"))
				 .GetFields(AccessTools.all)
				 .FirstOrDefault(field => field.FieldType == typeof(TargetIndex));
			int patchState = 0;
			foreach (var instr in instructions)
			{
				switch (patchState)
				{
					case 0:
						yield return instr;
						if (instr.opcode == OpCodes.Ldfld && instr.operand == markerField)
						{
							yield return new CodeInstruction(OpCodes.Ldarg_0);
							yield return new CodeInstruction(OpCodes.Ldfld, toilField);
							yield return new CodeInstruction(OpCodes.Ldarg_0);
							yield return new CodeInstruction(OpCodes.Ldfld, targetIndexField);
							yield return new CodeInstruction(OpCodes.Call, sneakyMethod);
							patchState++;
						}
						break;
					case 1:
						if (instr.opcode == OpCodes.Callvirt && instr.operand == markerMethod)
						{
							patchState++;
						}
						break;
					case 2:
						yield return instr;
						break;
				}
			}
		}

		static LocalTargetInfo ClosestOutputOrPosition(Toil toil, TargetIndex targetIndex)
		{
			LocalTargetInfo targetInfo = toil.actor.jobs.curJob.GetTarget(targetIndex);
			Thing thing = targetInfo.Thing;
			if (thing != null)
			{
				Comp_StorageAbstract comp = thing.Position.GetStorageComponent<Comp_StorageAbstract>(thing.Map);
				if (comp != null)
				{
					Thing output = comp.FindClosestOutputParent(toil.actor.Position);
					if (output != null)
					{
						return new LocalTargetInfo(output);
					}
				}
			}
			return toil.actor.CurJob.GetTarget(targetIndex);
		}
	}
}
