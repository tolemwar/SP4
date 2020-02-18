using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class NetworkReader
{

    private BinaryReader reader;
    public Peer peer;

    public NetworkReader(Peer peer)
    {
        this.peer = peer;
        reader = new BinaryReader(new MemoryStream());
    }

    public bool StartReading()
    {
        if (peer == null)
        {
            return false;
        }

        Position = 0;
        peer.ReadBytes((MemoryStream)reader.BaseStream, peer.incomingBytesUnread);
        peer.TOTAL_RECEIVED_BYTES += Length;
        return true;

    }

    // "int" - лучший тип для .Позиция. "short" слишком мал, если мы отправим > 32kb, что приведет к отрицательному результату .Позиция
    // - >преобразование long в int нормально до 2GB данных (MAX_INT), поэтому нам не нужно беспокоиться о переполнениях
    public int Position { get { return (int)reader.BaseStream.Position; } set { reader.BaseStream.Position = value; } }
    public int Length => (int)reader.BaseStream.Length;

    public byte ReadByte() => reader.ReadByte();
    public sbyte ReadSByte() => reader.ReadSByte();
    public char ReadChar() => reader.ReadChar();
    public bool ReadBoolean() => reader.ReadBoolean();
    public short ReadInt16() => reader.ReadInt16();
    public ushort ReadUInt16() => reader.ReadUInt16();
    public int ReadInt32() => reader.ReadInt32();
    public uint ReadUInt32() => reader.ReadUInt32();
    public long ReadInt64() => reader.ReadInt64();
    public ulong ReadUInt64() => reader.ReadUInt64();
    public decimal ReadDecimal() => reader.ReadDecimal();
    public float ReadFloat() => reader.ReadSingle();
    public double ReadDouble() => reader.ReadDouble();

    public string ReadString()
    {
        return reader.ReadBoolean() ? reader.ReadString() : null; // null support, see NetworkWriter
    }

    public byte[] ReadBytes(int count) => reader.ReadBytes(count);

    public byte[] ReadBytesAndSize()
    {
        // notNull? (see NetworkWriter)
        bool notNull = reader.ReadBoolean();
        if (notNull)
        {
            uint size = ReadPackedUInt32();
            return reader.ReadBytes((int)size);
        }
        return null;
    }
  
    // http://sqlite.org/src4/doc/trunk/www/varint.wiki
    // NOTE: big endian.
    public uint ReadPackedUInt32()
    {
        ulong value = ReadPackedUInt64();
        if (value > uint.MaxValue)
        {
            throw new IndexOutOfRangeException("ReadPackedUInt32() failure, value too large");
        }
        return (uint)value;
    }

    public ulong ReadPackedUInt64()
    {
        byte a0 = ReadByte();
        if (a0 < 241)
        {
            return a0;
        }

        byte a1 = ReadByte();
        if (a0 >= 241 && a0 <= 248)
        {
            return 240 + 256 * (a0 - ((ulong)241)) + a1;
        }

        byte a2 = ReadByte();
        if (a0 == 249)
        {
            return 2288 + (((ulong)256) * a1) + a2;
        }

        byte a3 = ReadByte();
        if (a0 == 250)
        {
            return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16);
        }

        byte a4 = ReadByte();
        if (a0 == 251)
        {
            return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24);
        }

        byte a5 = ReadByte();
        if (a0 == 252)
        {
            return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32);
        }

        byte a6 = ReadByte();
        if (a0 == 253)
        {
            return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40);
        }

        byte a7 = ReadByte();
        if (a0 == 254)
        {
            return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48);
        }

        byte a8 = ReadByte();
        if (a0 == 255)
        {
            return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48) + (((ulong)a8) << 56);
        }

        throw new IndexOutOfRangeException("ReadPackedUInt64() failure: " + a0);
    }

    public Vector2 ReadVector2()
    {
        return new Vector2(ReadFloat(), ReadFloat());
    }

    public Vector3 ReadVector3()
    {
        return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
    }

    public Vector3 ReadVector3(bool compressed)
    {
        if (compressed)
        {
            return new Vector3(peer.ReadCompressedFloat, peer.ReadCompressedFloat, peer.ReadCompressedFloat);
        }
        else
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }
    }

    public Vector4 ReadVector4()
    {
        return new Vector4(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
    }

    public Color ReadColor()
    {
        return new Color(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
    }

    public Color32 ReadColor32()
    {
        return new Color32(ReadByte(), ReadByte(), ReadByte(), ReadByte());
    }

    public Quaternion ReadQuaternion()
    {
        return new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
    }

    public Rect ReadRect()
    {
        return new Rect(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
    }

    public Plane ReadPlane()
    {
        return new Plane(ReadVector3(), ReadFloat());
    }

    public Ray ReadRay()
    {
        return new Ray(ReadVector3(), ReadVector3());
    }

    public Matrix4x4 ReadMatrix4x4()
    {
        Matrix4x4 m = new Matrix4x4
        {
            m00 = ReadFloat(),
            m01 = ReadFloat(),
            m02 = ReadFloat(),
            m03 = ReadFloat(),
            m10 = ReadFloat(),
            m11 = ReadFloat(),
            m12 = ReadFloat(),
            m13 = ReadFloat(),
            m20 = ReadFloat(),
            m21 = ReadFloat(),
            m22 = ReadFloat(),
            m23 = ReadFloat(),
            m30 = ReadFloat(),
            m31 = ReadFloat(),
            m32 = ReadFloat(),
            m33 = ReadFloat()
        };
        return m;
    }
}