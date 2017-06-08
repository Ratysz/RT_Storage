using Verse;

namespace RT_Storage
{
	public class CompProperties_StorageOutput : CompProperties_StorageIOAbstract
	{
		public CompProperties_StorageOutput()
		{
			compClass = typeof(Comp_StorageOutput);
		}
	}

	public class Comp_StorageOutput : Comp_StorageIOAbstract
	{
		new public CompProperties_StorageOutput properties
		{
			get
			{
				return (CompProperties_StorageOutput)props;
			}
		}
	}
}
