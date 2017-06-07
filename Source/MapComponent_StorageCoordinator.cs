using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RT_Storage
{
	public class MapComponent_StorageCoordinator : MapComponent
	{
		private Dictionary<Comp_StorageInput, SlotGroup> inputs = new Dictionary<Comp_StorageInput, SlotGroup>();
		private Dictionary<Comp_StorageOutput, SlotGroup> outputs = new Dictionary<Comp_StorageOutput, SlotGroup>();
		private List<Comp_StorageAbstract> storage = new List<Comp_StorageAbstract>();
		private Dictionary<SlotGroup, List<IntVec3>> extraCells = new Dictionary<SlotGroup, List<IntVec3>>();
		private Dictionary<IntVec3, List<Comp_CoordinatedAbstract>> compMap = new Dictionary<IntVec3, List<Comp_CoordinatedAbstract>>();

		private int CUI_inputs = 0;
		private int CUI_outputs = 0;

		public MapComponent_StorageCoordinator(Map map) : base(map)
		{

		}

		public void RegisterComponent(ThingComp component)
		{
			Type type = component.GetType();
			if (typeof(Comp_StorageInput).IsAssignableFrom(type))
			{
				inputs.Add((Comp_StorageInput)component, component.parent.GetSlotGroup());
				map.slotGroupManager.Notify_GroupChangedPriority();
			}
			else if (typeof(Comp_StorageOutput).IsAssignableFrom(type))
			{
				outputs.Add((Comp_StorageOutput)component, component.parent.GetSlotGroup());
				map.slotGroupManager.Notify_GroupChangedPriority();
			}
			else if (typeof(Comp_StorageAbstract).IsAssignableFrom(type))
			{
				storage.Add((Comp_StorageAbstract)component);
			}
			IntVec3 cell = component.parent.InteractionCell;
			List<Comp_CoordinatedAbstract> compsInCell;
			if (!compMap.TryGetValue(cell, out compsInCell))
			{
				compsInCell = new List<Comp_CoordinatedAbstract>();
				compMap.Add(cell, compsInCell);
			}
			if (!compsInCell.Contains((Comp_CoordinatedAbstract)component))
			{
				compsInCell.Add((Comp_CoordinatedAbstract)component);
			}
		}

		public void DeregisterComponent(ThingComp component)
		{
			Type type = component.GetType();
			if (typeof(Comp_StorageInput).IsAssignableFrom(type))
			{
				SlotGroup slotGroup = ((Comp_StorageInput)component).GetSlotGroup();
				List<IntVec3> cells;
				if (slotGroup != null && extraCells.TryGetValue(slotGroup, out cells))
				{
					foreach (var parentCell in component.parent.OccupiedRect())
					{
						cells.Remove(parentCell);
					}
				}
				inputs.Remove((Comp_StorageInput)component);
				if (CUI_inputs > 0)
				{
					CUI_inputs--;
				}
				map.slotGroupManager.Notify_GroupChangedPriority();
			}
			else if (typeof(Comp_StorageOutput).IsAssignableFrom(type))
			{
				outputs.Remove((Comp_StorageOutput)component);
				if (CUI_outputs > 0)
				{
					CUI_outputs--;
				}
				map.slotGroupManager.Notify_GroupChangedPriority();
			}
			else if (typeof(Comp_StorageAbstract).IsAssignableFrom(type))
			{
				storage.Remove((Comp_StorageAbstract)component);
			}
			IntVec3 cell = component.parent.InteractionCell;
			List<Comp_CoordinatedAbstract> compsInCell;
			compMap.TryGetValue(cell, out compsInCell);
			compsInCell.Remove((Comp_CoordinatedAbstract)component);
		}

		public void Notify_SlotGroupDestroyed(SlotGroup slotGroup)
		{
			Utility.Debug($"Slot group {slotGroup} destroyed.");
			extraCells.Remove(slotGroup);
			foreach (var kvp in inputs.ToList())
			{
				if (kvp.Value == slotGroup)
				{
					inputs[kvp.Key] = null;
				}
			}
			foreach (var kvp in outputs.ToList())
			{
				if (kvp.Value == slotGroup)
				{
					outputs[kvp.Key] = null;
				}
			}
		}

		public bool HasStorageInputs(SlotGroup slotGroup)
		{
			return inputs.ContainsValue(slotGroup);
		}

		public List<IntVec3> GetExtraCells(SlotGroup slotGroup)
		{
			List<IntVec3> values;
			if (extraCells.TryGetValue(slotGroup, out values))
			{
				return values;
			}
			return null;
		}

		public List<Comp_CoordinatedAbstract> GetCompsInCell(IntVec3 cell)
		{
			List<Comp_CoordinatedAbstract> values;
			if (compMap.TryGetValue(cell, out values))
			{
				return values;
			}
			return null;
		}

		public override void MapComponentTick()
		{
			base.MapComponentTick();
			if (inputs.Count > 0)
			{
				if (CUI_inputs == inputs.Count)
				{
					CUI_inputs = 0;
				}
				Comp_StorageInput input = inputs.ToArray()[CUI_inputs].Key;
				SlotGroup slotGroup = input.GetSlotGroup();
				inputs[input] = slotGroup;
				if (slotGroup != null)
				{
					List<IntVec3> cells;
					if (!extraCells.TryGetValue(slotGroup, out cells))
					{
						cells = new List<IntVec3>();
						extraCells.Add(slotGroup, cells);
					}
					foreach (var cell in input.parent.OccupiedRect())
					{
						if (!cells.Contains(cell))
						{
							cells.Add(cell);
						}
					}
				}
				CUI_inputs++;
			}
			if (outputs.Count > 0)
			{
				if (CUI_outputs == outputs.Count)
				{
					CUI_outputs = 0;
				}
				Comp_StorageOutput output = outputs.ToArray()[CUI_outputs].Key;
				SlotGroup slotGroup = output.GetSlotGroup();
				outputs[output] = slotGroup;
				CUI_outputs++;
			}
			//DebugDump();
		}

		public Comp_StorageAbstract DebugGetAnyStorage()
		{
			return storage.RandomElement();
		}

		private IntVec3 cachedCell = IntVec3.Invalid;
		private IntVec3 previousCell = IntVec3.Invalid;
		public IntVec3 FindClosestOutput(Thing thing, IntVec3 rootCell)
		{
			if (rootCell != previousCell)
			{
				previousCell = rootCell;
				var closestThing = GenClosest.ClosestThingReachable(
					rootCell,
					map,
					ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
					Verse.AI.PathEndMode.InteractionCell,
					TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false),
					9999.0f,
					t => {
						List<Comp_CoordinatedAbstract> compsInCell;
						if (compMap.TryGetValue(thing.Position, out compsInCell))
						{
							foreach (var comp in compsInCell)
							{
								var storageComp = comp as Comp_StorageAbstract;
								if (storageComp != null)
								{
									return storageComp.linkedOutputParents.Contains(t);
								}
							}
						}
						return false;
					});
				if (closestThing != null)
				{
					cachedCell = closestThing.InteractionCell;
				}
				else
				{
					cachedCell = IntVec3.Invalid;
				}
			}
			return cachedCell;
		}

		private int dumpTimer = 0;
		private void DebugDump()
		{
			if (dumpTimer == 0)
			{
				Utility.Debug("||||||||||||||");
				Utility.Debug("dumping inputs");
				foreach (var kvp in inputs)
				{
					Utility.Debug(kvp.ToString());
				}
				Utility.Debug("dumping outputs");
				foreach (var kvp in outputs)
				{
					Utility.Debug(kvp.ToString());
				}
				Utility.Debug("dumping storage");
				foreach (var kvp in storage)
				{
					Utility.Debug(kvp.ToString());
				}
				Utility.Debug("dumping extraCells");
				foreach (var kvp in extraCells)
				{
					Utility.Debug(kvp.ToString());
					foreach (var iter2 in kvp.Value)
					{
						Utility.Debug(iter2.ToString());
					}
				}
				Utility.Debug("dumping compMap");
				foreach (var kvp in compMap)
				{
					Utility.Debug(kvp.ToString());
					foreach (var iter2 in kvp.Value)
					{
						Utility.Debug(iter2.ToString());
					}
				}
				Utility.Debug("||||||||||||||");
				dumpTimer = 300;
			}
			Utility.Debug("TICK");
			dumpTimer--;
		}
	}
}