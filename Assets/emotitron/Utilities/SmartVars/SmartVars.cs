﻿//Copyright 2018, Davin Carten, All rights reserved

using System.Runtime.InteropServices;
using System;

namespace emotitron.Utilities.SmartVars
{
	public enum SmartVarTypeCode
	{
		None, Int, Uint, Bool, Float, Byte, Short, UShort, String
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SmartVar
	{

		[FieldOffset(0)]
		public SmartVarTypeCode TypeCode;

		[FieldOffset(4)]
		public Int32 Int;

		[FieldOffset(4)]
		public UInt32 UInt;

		[FieldOffset(4)]
		public Boolean Bool;

		[FieldOffset(4)]
		public Single Float;

		[FieldOffset(4)]
		public Byte Byte8;

		[FieldOffset(4)]
		public Int16 Short;

		[FieldOffset(4)]
		public UInt16 UShort;

		[FieldOffset(8)]
		public String Str;

		public readonly static SmartVar None = new SmartVar() { TypeCode = SmartVarTypeCode.None };

		public static implicit operator SmartVar(Int32 v)
		{
			return new SmartVar { Int = v, TypeCode = SmartVarTypeCode.Int };
		}

		public static implicit operator SmartVar(UInt32 v)
		{
			return new SmartVar { UInt = v, TypeCode = SmartVarTypeCode.Uint };
		}

		public static implicit operator SmartVar(Single v)
		{
			return new SmartVar { Float = v, TypeCode = SmartVarTypeCode.Float };
		}

		public static implicit operator SmartVar(Boolean v)
		{
			return new SmartVar { Bool = v, TypeCode = SmartVarTypeCode.Bool };
		}

		public static implicit operator SmartVar(Byte v)
		{
			return new SmartVar { Byte8 = v, TypeCode = SmartVarTypeCode.Byte };
		}

		public static implicit operator SmartVar(Int16 v)
		{
			return new SmartVar { Short = v, TypeCode = SmartVarTypeCode.Short };
		}

		public static implicit operator SmartVar(UInt16 v)
		{
			return new SmartVar { UShort = v, TypeCode = SmartVarTypeCode.UShort };
		}

		public static implicit operator SmartVar(String v)
		{
			return new SmartVar { Str = v, TypeCode = SmartVarTypeCode.String };
		}


		public static implicit operator Int32(SmartVar v)
		{
			if (v.TypeCode == SmartVarTypeCode.Int)
			{
				return v.Int;
			}

			throw new InvalidCastException();
		}

		public static implicit operator UInt32(SmartVar v)
		{
			if (v.TypeCode == SmartVarTypeCode.Uint)
			{
				return v.UInt;
			}

			throw new InvalidCastException();
		}

		public static implicit operator Single(SmartVar v)
		{
			if (v.TypeCode == SmartVarTypeCode.Float)
			{
				return v.Float;
			}

			throw new InvalidCastException();
		}

		public static implicit operator Boolean(SmartVar v)
		{
			if (v.TypeCode == SmartVarTypeCode.Bool)
			{
				return v.Bool;
			}

			throw new InvalidCastException();
		}

		public static implicit operator Byte(SmartVar v)
		{
			if (v.TypeCode == SmartVarTypeCode.Byte)
			{
				return v.Byte8;
			}

			throw new InvalidCastException();
		}

		public static implicit operator Int16(SmartVar v)
		{
			if (v.TypeCode == SmartVarTypeCode.Short)
			{
				return v.Short;
			}

			throw new InvalidCastException();
		}

		public static implicit operator UInt16(SmartVar v)
		{
			if (v.TypeCode == SmartVarTypeCode.UShort)
			{
				return v.UShort;
			}

			throw new InvalidCastException();
		}

		public static implicit operator String(SmartVar v)
		{
			if (v.TypeCode == SmartVarTypeCode.String)
			{
				return v.Str;
			}

			throw new InvalidCastException();
		}

		public SmartVar Copy()
		{
			return new SmartVar() { TypeCode = this.TypeCode, Int = this.Int };
		}

		public override string ToString()
		{
			string str = TypeCode.ToString() + " ";
			if (TypeCode == SmartVarTypeCode.None)
				return str;
			else if (TypeCode == SmartVarTypeCode.Bool)
				return str + this.Bool;
			else if (TypeCode == SmartVarTypeCode.Int)
				return str + this.Int;
			else if (TypeCode == SmartVarTypeCode.Uint)
				return str + this.UInt;
			else if (TypeCode == SmartVarTypeCode.Float)
				return str + this.Float;
			else if (TypeCode == SmartVarTypeCode.Short)
				return str + this.Short;
			else if (TypeCode == SmartVarTypeCode.UShort)
				return str + this.UShort;
			else if (TypeCode == SmartVarTypeCode.Byte)
				return str + this.Byte8;
			else if (TypeCode == SmartVarTypeCode.String)
				return str + this.Str;

			return str;
		}
	}
}

