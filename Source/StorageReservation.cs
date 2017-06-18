using RimWorld;
using Verse;

namespace RT_Storage
{
	public class StorageReservation
	{
		public Comp_StorageAbstract storage;
		public ThingDef def;
		public int stackCount;
		public ThingDef stuff;
		public Pawn pawn;

		public StorageReservation(Comp_StorageAbstract storage, Pawn pawn, Thing thing, int stackCount = 0)
		{
			this.storage = storage;
			def = thing.def;
			this.stackCount = (stackCount != 0 ? stackCount : thing.stackCount);
			stuff = thing.Stuff;
			this.pawn = pawn;
		}

		public override string ToString()
		{
			return $"(reservation {GetHashCode()}: {stackCount} of {def} in {storage} by {pawn})";
		}
	}
}
