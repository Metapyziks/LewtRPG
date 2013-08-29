using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace Lewt.Shared.Networking
{
    public static class NetworkConstants
    {
        public const ushort ProtocolVersion = 0x0004;
    }

    public enum PacketID : byte
    {
        CheckActive         = 0x00,
        Handshake           = 0x01,
        Authenticate        = 0x02,
        ResourceRequest     = 0x03,
        Resource            = 0x04,
        WorldRequest        = 0x05,
        PostWorld           = 0x06,
        PlayerJoin          = 0x07,
        PlayerLeave         = 0x08,
        PlayerEnterMap      = 0x09,
        PlayerLeaveMap      = 0x0A,
        MapRequest          = 0x0B,
        InteriorChunk       = 0x0C,
        ExteriorChunk       = 0x0D,
        DiscardChunk        = 0x0E,
        PostMap             = 0x0F,
        SyncTime            = 0x10,
        ChatMessage         = 0x11,
        CharacterMove       = 0x12,
        CharacterStop       = 0x13,
        SpellCast           = 0x14,
        EntityAdded         = 0x15,
        EntityRemoved       = 0x16,
        EntityUpdated       = 0x17,
        CharPointRequest    = 0x18,
        CharacterCreate     = 0x19,
        Resurrect           = 0x1A,
        UseEntity           = 0x1B,
        ViewInventory       = 0x1C,
        ModifyInventory     = 0x1D,
        UseItem             = 0x1E,

        Disconnect = 0xFF
    }
    

    public enum DisconnectReason : byte
    {
        ServerStopping          = 0x00,
        ServerFull              = 0x01,
        ProtocolVersionMismatch = 0x02,
        Kicked                  = 0x03,
        Timeout                 = 0x04,
        ClientDisconnect        = 0x05,
        BadPassword             = 0x06,
        ResourceNotFound        = 0x07
    }

    public class RemoteNetworkedObject
    {
        public virtual void CheckForPackets()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnReceivePacket( PacketID id, BinaryReader reader )
        {
            throw new NotImplementedException();
        }

        public virtual BinaryWriter GetWriter()
        {
            throw new NotImplementedException();
        }

        public virtual void SendPacket()
        {
            throw new NotImplementedException();
        }

        public virtual bool PacketPending()
        {
            throw new NotImplementedException();
        }

        public virtual IPAddress IPAddress
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public String SanitisedIPAddress
        {
            get
            {
                return IPAddress.ToString().Replace( '.', '_' );
            }
        }
    }
}