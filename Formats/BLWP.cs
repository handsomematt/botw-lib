// Based on code by https://github.com/LordNed

using BotWLib.Common;
using BotWLib.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BotWLib
{
    public class BLWP
    {
        public List<BLWPMesh> Meshes;
        
        public void LoadFromStream(Stream input)
        {
            using (var reader = new EndianBinaryReader(input, Encoding.ASCII, true, Endian.Big))
            {
                if (reader.ReadChars(4).ToString() != "PrOD")
                    throw new InvalidDataException("Mismatching header!");

                reader.ReadBytes(12); // Unknown bytes
                var fileSize = reader.ReadInt32();
                var numberOfMeshes = reader.ReadInt32();
                var stringTableOffset = reader.ReadInt32();
                reader.ReadBytes(4); // Null padding.

                Meshes = new List<BLWPMesh>(numberOfMeshes);
                for (int i = 0; i < numberOfMeshes; i++)
                {
                    var size = reader.ReadInt32();
                    var instanceCount = reader.ReadInt32();
                    var stringOffset = reader.ReadInt32();
                    Trace.Assert(reader.ReadInt32() == 0);

                    // Read the string name for these instances
                    long streamPos = reader.BaseStream.Position;
                    reader.BaseStream.Position = stringTableOffset + stringOffset;
                    string instanceName = reader.ReadStringUntil('\0');

                    BLWPMesh instanceHdr = new BLWPMesh();
                    instanceHdr.MeshName = instanceName;
                    Meshes.Add(instanceHdr);

                    // Jump back to where we were in our stream and read instanceCount many instances of data.
                    reader.BaseStream.Position = streamPos;
                    for (int j = 0; j < instanceCount; j++)
                    {
                        BLWPMeshInstance inst = new BLWPMeshInstance();
                        inst.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        inst.Rotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        inst.UniformScale = reader.ReadSingle();
                        instanceHdr.MeshInstances.Add(inst);

                        reader.ReadUInt32();
                    }
                }
            }
        }
    }

    public class BLWPMesh
    {
        public string MeshName;
        public List<BLWPMeshInstance> MeshInstances;
        
        public BLWPMesh()
        {
            MeshInstances = new List<BLWPMeshInstance>();
        }
    }

    /// <summary>
    /// Immediately following the <see cref="BLWPMesh"/> is <see cref="BLWPMesh.InstanceCount"/> many instances of that actor.
    /// Each instance is padded up to 0x32 bytes.
    /// </summary>
    public class BLWPMeshInstance
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public float UniformScale;

        public BLWPMeshInstance()
        {
            UniformScale = 1f;
        }
    }
}
