using Verse;
using Harmony;
using System.Reflection.Emit;

namespace RT_Storage
{
	public static class Utility
	{
		/*public class CodeInstruction : Harmony.CodeInstruction
		{
			public CodeInstruction(OpCode opcode, object operand = null) : base(opcode, operand)
			{
				PrintInstruction(this);
			}

			public CodeInstruction(CodeInstruction instruction) : base(instruction)
			{
				PrintInstruction(this);
			}
		}*/

		public static void Debug(string message)
		{
#if DEBUG
			Log.Message("[RT Storage]: " + message);
#endif
		}

		public static void PrintInstruction(CodeInstruction instr)
		{
			Debug($"INSTR : {instr.opcode,-10}\t : {instr.operand,-100}");
			foreach (var label in instr.labels)
			{
				Debug($"LABEL : {instr.opcode,-10}\t : : : {label,-100}");
			}
		}

		/*public static void PrintInstruction(Harmony.CodeInstruction instr)
		{
			Debug($"INSTR : {instr.opcode,-10}\t : {instr.operand,-100}");
			foreach (var label in instr.labels)
			{
				Debug($"LABEL : {instr.opcode,-10}\t : : : {label,-100}");
			}
		}*/
	}
}
