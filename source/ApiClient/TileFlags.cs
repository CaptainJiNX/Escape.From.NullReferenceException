﻿using System;

namespace ApiClient
{
	//NOTHING, BLOCKED and PERIMETER can’t be walked on, all else can (if no entity is standing there)
	[Flags]
	public enum TileFlags : uint
	{
		NOTHING = 0x00000000,
		BLOCKED = 0x00000001,
		ROOM = 0x00000002,
		CORRIDOR = 0x00000004,
		PERIMETER = 0x00000010,
		ENTRANCE = 0x00000020,
		ROOM_ID = 0x0000FFC0,
		ARCH = 0x00010000,
		DOOR1 = 0x00020000,
		DOOR2 = 0x00040000,
		DOOR3 = 0x00080000,
		DOOR4 = 0x00100000,
		PORTCULLIS = 0x00200000,
		STAIR_DOWN = 0x00400000,
		STAIR_UP = 0x00800000,
		LABEL = 0xFF000000,
		UNKNOWN = 0xFFFFFFFF
	}
}