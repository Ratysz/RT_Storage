using RimWorld;
using Verse;

namespace RT_Storage
{
	public class StorageReservation
	{
		public Comp_StorageAbstract storage;
		public Thing thing;
		public Pawn pawn;

		public StorageReservation(Comp_StorageAbstract storage, Thing thing, Pawn pawn)
		{
			this.storage = storage;
			this.thing = thing;
			this.pawn = pawn;
		}

		public override string ToString()
		{
			return $"(reservation {GetHashCode()}: {thing} in {storage} by {pawn})";
		}
	}
}
