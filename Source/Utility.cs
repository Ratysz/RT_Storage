using Verse;
using Harmony;
using System.Text;
using System.Reflection.Emit;

namespace RT_Storage
{
	public static class Utility
	{
		public static void Debug(string message)
		{
#if DEBUG
			Log.Message("[RT Storage]: " + message);
#endif
		}

		public class CodeInstruction : Harmony.CodeInstruction
		{
			public CodeInstruction(OpCode opcode, object operand = null) : base(opcode, operand)
			{
				PrintInstruction(this);
			}

			public CodeInstruction(CodeInstruction instruction) : base(instruction)
			{
				PrintInstruction(this);
			}
		}

		public static void PrintInstruction(CodeInstruction instr)
		{
			PrintInstruction((Harmony.CodeInstruction)instr);
		}

		public static void PrintInstruction(Harmony.CodeInstruction instr)
		{
			if (instr.labels.Count > 0)
			{
				var labels = instr.labels.ToArray();
				StringBuilder builder = new StringBuilder();
				builder.Append($"LABELS : : : > {labels[0].GetHashCode()}");
				int index = 1;
				while (index < instr.labels.Count)
				{
					builder.Append($", {labels[index].GetHashCode()}");
					index++;
				}
				Debug(builder.ToString());
			}
			Debug($"INSTR : {instr.opcode,-10}\t : "
				+ ((instr.operand != null && instr.operand.GetType() == typeof(Label))
					? $": : > {instr.operand.GetHashCode(),-100}" : $"{instr.operand,-100}"));
		}
	}
}
