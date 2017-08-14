namespace ScanMaster.Database.Converters
{
	public enum StateOperator : byte
	{
        UNKNOWN = 0,
		EQUAL = 0x3D,
		NOT_EQUAL = 0x21,
		GREATER = 0x3E,
		LESS = 0x3C,
		MASK_ZERO = 0x30,
		MASK_NOT_ZERO = 0x39
	}
}
