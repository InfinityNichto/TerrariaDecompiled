using System.IO;
using Terraria.DataStructures;
using Terraria.Net;

namespace Terraria.GameContent.NetModules;

public class NetTeleportPylonModule : NetModule
{
	public enum SubPacketType : byte
	{
		PylonWasAdded,
		PylonWasRemoved,
		PlayerRequestsTeleport
	}

	public static NetPacket SerializePylonWasAddedOrRemoved(TeleportPylonInfo info, SubPacketType packetType)
	{
		NetPacket result = NetModule.CreatePacket<NetTeleportPylonModule>(6);
		result.Writer.Write((byte)packetType);
		result.Writer.Write(info.PositionInTiles.X);
		result.Writer.Write(info.PositionInTiles.Y);
		result.Writer.Write((byte)info.TypeOfPylon);
		return result;
	}

	public static NetPacket SerializeUseRequest(TeleportPylonInfo info)
	{
		NetPacket result = NetModule.CreatePacket<NetTeleportPylonModule>(6);
		result.Writer.Write((byte)2);
		result.Writer.Write(info.PositionInTiles.X);
		result.Writer.Write(info.PositionInTiles.Y);
		result.Writer.Write((byte)info.TypeOfPylon);
		return result;
	}

	public override bool Deserialize(BinaryReader reader, int userId)
	{
		switch ((SubPacketType)reader.ReadByte())
		{
		case SubPacketType.PylonWasAdded:
		{
			if (Main.dedServ)
			{
				return false;
			}
			TeleportPylonInfo info3 = default(TeleportPylonInfo);
			info3.PositionInTiles = new Point16(reader.ReadInt16(), reader.ReadInt16());
			info3.TypeOfPylon = (TeleportPylonType)reader.ReadByte();
			Main.PylonSystem.AddForClient(info3);
			break;
		}
		case SubPacketType.PylonWasRemoved:
		{
			if (Main.dedServ)
			{
				return false;
			}
			TeleportPylonInfo info2 = default(TeleportPylonInfo);
			info2.PositionInTiles = new Point16(reader.ReadInt16(), reader.ReadInt16());
			info2.TypeOfPylon = (TeleportPylonType)reader.ReadByte();
			Main.PylonSystem.RemoveForClient(info2);
			break;
		}
		case SubPacketType.PlayerRequestsTeleport:
		{
			TeleportPylonInfo info = default(TeleportPylonInfo);
			info.PositionInTiles = new Point16(reader.ReadInt16(), reader.ReadInt16());
			info.TypeOfPylon = (TeleportPylonType)reader.ReadByte();
			Main.PylonSystem.HandleTeleportRequest(info, userId);
			break;
		}
		}
		return true;
	}
}
