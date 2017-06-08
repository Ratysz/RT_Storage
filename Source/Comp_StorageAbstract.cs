using System;
using System.Collections.Generic;
using Verse;

namespace RT_Storage
{
	public class CompProperties_StorageAbstract : CompProperties_CoordinatedAbstract
	{

	}

	public abstract class Comp_StorageAbstract : Comp_CoordinatedAbstract
	{
		new public CompProperties_StorageAbstract properties
		{
			get
			{
				return (CompProperties_StorageAbstract)props;
			}
		}
		protected List<Comp_StorageOutput> linkedOutputs = new List<Comp_StorageOutput>();
		protected List<Thing> linkedOutputParents = new List<Thing>();
		protected List<Comp_StorageInput> linkedInputs = new List<Comp_StorageInput>();
		protected List<Thing> linkedInputParents = new List<Thing>();

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			foreach (var output in linkedOutputs)
			{
				output.Notify_StorageRemoved();
			}
			linkedOutputs.Clear();
			linkedOutputParents.Clear();
			foreach (var input in linkedInputs)
			{
				input.Notify_StorageRemoved();
			}
			linkedInputs.Clear();
			linkedInputParents.Clear();
		}

		public void Notify_IOAdded(Comp_StorageIOAbstract io)
		{
			if (io.GetType().IsAssignableFrom(typeof(Comp_StorageInput)))
			{
				linkedInputs.Add((Comp_StorageInput)io);
				linkedInputParents.Add(io.parent);
			}
			else if (io.GetType().IsAssignableFrom(typeof(Comp_StorageOutput)))
			{
				linkedOutputs.Add((Comp_StorageOutput)io);
				linkedOutputParents.Add(io.parent);
				previousRootCell = IntVec3.Invalid;
			}
		}

		public void Notify_IORemoved(Comp_StorageIOAbstract io)
		{
			if (io.GetType().IsAssignableFrom(typeof(Comp_StorageInput)))
			{
				linkedInputs.Remove((Comp_StorageInput)io);
				linkedInputParents.Remove(io.parent);
			}
			else if (io.GetType().IsAssignableFrom(typeof(Comp_StorageOutput)))
			{
				linkedOutputs.Remove((Comp_StorageOutput)io);
				linkedOutputParents.Remove(io.parent);
				previousRootCell = IntVec3.Invalid;
			}
		}

		virtual public int CanAccept(Thing thing)
		{
			return 0;
		}

		virtual public IntVec3 ObtainCell(Thing thing)
		{
			return IntVec3.Invalid;
		}

		virtual public bool Store(Thing thing, IntVec3 cell, out Thing resultingThing, Action<Thing, int> placedAction = null)
		{
			resultingThing = null;
			return false;
		}

		protected Thing cachedOutputParent = null;
		protected IntVec3 previousRootCell = IntVec3.Invalid;
		virtual public Thing FindClosestOutputParent(IntVec3 rootCell)
		{
			if (rootCell != previousRootCell)
			{
				previousRootCell = rootCell;
				Thing closestThing = GenClosest.ClosestThingReachable(
					rootCell,
					parent.Map,
					ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
					Verse.AI.PathEndMode.OnCell,
					TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false),
					9999.0f,
					null,
					linkedOutputParents);
				cachedOutputParent = closestThing;
			}
			return cachedOutputParent;
		}
	}
}
