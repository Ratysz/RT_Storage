namespace RT_Storage
{
	class Garbage
	{
		/*static FieldInfo targetIndexField = typeof(Toils_Goto)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("4F4"))
				.GetFields(AccessTools.all)
				.FirstOrDefault(field => field.FieldType == typeof(TargetIndex));

		static FieldInfo toilField = typeof(Toils_Goto)
				.GetNestedTypes(AccessTools.all)
				.FirstOrDefault(type => type.FullName.Contains("4F4"))
				.GetFields(AccessTools.all)
				.FirstOrDefault(field => field.FieldType == typeof(Toil));

		static bool Prefix(object __instance, out object[] __state)
		{
			__state = null;
			Toil toil = (Toil)toilField.GetValue(__instance);
			TargetIndex targetIndex = (TargetIndex)targetIndexField.GetValue(__instance);
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
						Job job = toil.actor.jobs.curJob;
						__state = new object[] { job, targetIndex, targetInfo };
						Utility.Debug($"prefix {__state[0]} | {__state[1]} | {__state[2]}");
						job.SetTarget(targetIndex, new LocalTargetInfo(output));
					}
				}
			}
			return true;
		}

		static void Postfix(object __instance, object[] __state)
		{
			if (__state != null)
			{
				Utility.Debug($"postfix {__state[0]} | {__state[1]} | {__state[2]}");
				((Job)__state[0]).SetTarget((TargetIndex)__state[1], (LocalTargetInfo)__state[2]);
			}
			__state = null;
		}*/
	}

	/*[HarmonyPatch(typeof(ListerThings))]
	[HarmonyPatch("ThingsMatching")]
	static class Patch_ThingsMatching
	{
		static void Postfix(ref List<Thing> __result)
		{
			var fakeThings = new List<Thing>();
			bool addedFakeThing = false;
			foreach (Thing thing in new List<Thing>(__result))
			{
				if (thing.def.EverHaulable)
				{
					Comp_StorageAbstract comp = thing.Position.GetStorageComponent<Comp_StorageAbstract>(thing.Map);
					if (comp != null)
					{
						foreach (var output in comp.linkedOutputs)
						{
							addedFakeThing = true;
							FakeThing fakeThing = FakeThing.MakeFakeThing(thing, output.parent.Position);
							fakeThings.Add(fakeThing);
							Utility.Debug($"  ++  adding fake thing: {fakeThing} : {thing}");
						}
					}
				}
			}
			if (addedFakeThing)
			{
				foreach (var thing in fakeThings)
				{
					__result.Insert(0, thing);
				}
			}
		}
	}

	public class FakeThing : ThingWithComps
	{
		public static List<FakeThing> fakeThings = new List<FakeThing>();

		public static FakeThing MakeFakeThing(Thing original, IntVec3 position)
		{
			ThingDef def = DefDatabase<ThingDef>.GetNamed(original.def.defName);
			Type backupType = def.thingClass;
			def.thingClass = typeof(FakeThing);
			FakeThing fakeThing = (FakeThing)ThingMaker.MakeThing(def, original.Stuff);
			def.thingClass = backupType;
			fakeThing.referencedThing = original;
			fakeThing.stackCount = original.stackCount;
			fakeThing.Position = position;
			fakeThing.SpawnSetup(original.Map, false);
			fakeThings.Add(fakeThing);
			return fakeThing;
		}

		public Thing referencedThing;
	}

	[HarmonyPatch(typeof(GenClosest))]
	[HarmonyPatch("ClosestThing_Regionwise_ReachablePrioritized")]
	static class Patch_ClosestThing_Regionwise_ReachablePrioritized
	{
		static void Postfix()
		{
			foreach (var thing in FakeThing.fakeThings)
			{
				thing.Destroy();
			}
			FakeThing.fakeThings.Clear();
		}
	}*/

	/*[HarmonyPatch(typeof(Thing))]
	[HarmonyPatch("Position", PropertyMethod.Getter)]
	public static class Patch_Position
	{
		public static IntVec3 cachedCell = IntVec3.Invalid;
		static bool Prefix(ref IntVec3 __result)
		{
			if (cachedCell != IntVec3.Invalid)
			{
				__result = IntVec3.Invalid;
				return false;
			}
			return true;
		}
	}*/

	/*[HarmonyPatch(typeof(StoreUtility))]
	[HarmonyPatch("IsGoodStoreCell")]
	static class Patch_IsGoodStoreCell
	{
		static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo markerMethod = AccessTools.Method(typeof(StoreUtility), "NoStorageBlockersIn");
			MethodInfo sneakyMethod1 = AccessTools.Method(typeof(Patch_IsGoodStoreCell), "FetchInput");
			MethodInfo sneakyMethod2 = AccessTools.Method(typeof(Patch_IsGoodStoreCell), "CheckIfAccepts");
			LocalBuilder sneakyVar = generator.DeclareLocal(typeof(Comp_StorageInput));
			Label sneakyLabel = generator.DefineLabel();
			bool patched = false;
			var instrList = instructions.ToList();
			for (int i = 0; i < instrList.Count; i++)
			{
				yield return instrList[i];
				if (!patched
					&& instrList[i + 3].opcode == OpCodes.Call
					&& instrList[i + 3].operand == markerMethod)
				{
					patched = true;
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, sneakyMethod1);
					yield return new CodeInstruction(OpCodes.Stloc, sneakyVar);
					yield return new CodeInstruction(OpCodes.Ldloc, sneakyVar);
					yield return new CodeInstruction(OpCodes.Brfalse, sneakyLabel);
					yield return new CodeInstruction(OpCodes.Ldloc, sneakyVar);
					yield return new CodeInstruction(OpCodes.Ldarg_2);
					yield return new CodeInstruction(OpCodes.Call, sneakyMethod2);
					yield return new CodeInstruction(OpCodes.Brtrue, instrList[i + 4].operand);
					var lastInstr = new CodeInstruction(OpCodes.Ldarg_0);
					lastInstr.labels.Add(sneakyLabel);
					yield return lastInstr;
				}
			}
		}

		static Comp_StorageInput FetchInput(IntVec3 cell, Map map)
		{
			Comp_StorageInput comp = cell.GetStorageComponent<Comp_StorageInput>(map);
			if (comp != null && comp.linkedStorage != null)
			{
				return comp;
			}
			return null;
		}

		static bool CheckIfAccepts(Comp_StorageInput comp, Thing thing)
		{
			return comp.CanAccept(thing) > 0;
		}
	}*/

	/*[HarmonyPatch(typeof(StoreUtility))]
	[HarmonyPatch("NoStorageBlockersIn")]
	static class Patch_NoStorageBlockersIn
	{
		static bool Prefix(ref bool __result, IntVec3 c, Map map, Thing thing)
		{
			Comp_StorageInput comp = c.GetStorageComponent<Comp_StorageInput>(map);
			if (comp != null)
			{
				__result = comp.CanAccept(thing) > 0;
				return false;
			}
			return true;
		}
	}*/
}
