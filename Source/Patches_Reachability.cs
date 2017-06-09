using Harmony;
using RimWorld;
using Verse;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using System.Linq;
using Verse.AI;
using UnityEngine;

namespace RT_Storage
{
	[HarmonyPatch]
	static class Patch_TFBBI_BaseValidator
	{
		static MethodBase TargetMethod()
		{
			return typeof(WorkGiver_DoBill)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("TryFindBestBillIngredients"))
				.GetMethods(AccessTools.all)
				.FirstOrDefault(
					method => method.ReturnType == typeof(bool)
					&& method.GetParameters()
						.FirstOrDefault(parameter => parameter.ParameterType == typeof(Thing)) != null);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo markerMethod = AccessTools.Property(typeof(Thing), nameof(Thing.Position)).GetGetMethod();
			MethodInfo sneakyMethod = AccessTools.Method(
				typeof(Patch_TFBBI_BaseValidator),
				nameof(Patch_TFBBI_BaseValidator.ClosestOutputOrPosition));
			bool patched = false;
			var instrList = instructions.ToList();
			for (int i = 0; i < instrList.Count; i++)
			{
				if (!patched && instrList[i].opcode == OpCodes.Callvirt && instrList[i].operand == markerMethod)
				{
					patched = true;
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, instrList[i + 2].operand);
					yield return new CodeInstruction(OpCodes.Call, sneakyMethod);
				}
				else
				{
					yield return instrList[i];
				}
			}
		}

		static IntVec3 ClosestOutputOrPosition(Thing thing, Thing otherThing)
		{
			return Patch_TFBBI_RegProc_Comparison.ClosestOutputOrPosition(thing, otherThing.InteractionCell);
		}
	}

	[HarmonyPatch]
	static class Patch_TFBBI_RegProc_Comparison
	{
		static MethodBase TargetMethod()
		{
			return typeof(WorkGiver_DoBill)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("TryFindBestBillIngredients"))
				.GetMethods(AccessTools.all)
				.FirstOrDefault(method => method.ReturnType == typeof(int));
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo markerMethod = AccessTools.Property(typeof(Thing), nameof(Thing.Position)).GetGetMethod();
			MethodInfo sneakyMethod = AccessTools.Method(
				typeof(Patch_TFBBI_RegProc_Comparison),
				nameof(Patch_TFBBI_RegProc_Comparison.ClosestOutputOrPosition));
			var instrList = instructions.ToList();
			for (int i = 0; i < instrList.Count; i++)
			{
				if (instrList[i].opcode == OpCodes.Callvirt && instrList[i].operand == markerMethod)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, instrList[i + 2].operand);
					yield return new CodeInstruction(OpCodes.Call, sneakyMethod);
				}
				else
				{
					yield return instrList[i];
				}
			}
		}

		public static IntVec3 ClosestOutputOrPosition(Thing thing, IntVec3 rootCell)
		{
			var cell = thing.Map.GetStorageCoordinator().FindClosestOutput(thing, rootCell);
			if (cell != IntVec3.Invalid)
			{
				int distanceToOutput = (cell - rootCell).LengthHorizontalSquared;
				int distanceToThing = (thing.Position - rootCell).LengthHorizontalSquared;
				if (distanceToOutput < distanceToThing)
				{
					return cell;
				}
			}
			return thing.Position;
		}
	}

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
			Utility.Debug($"{toil.actor} | {toil.actor.jobs.curJob} | {targetInfo}");
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