﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.OCS
{
    [YAXSerializeAs("OCS")]
   public class OCS_File
    {
        [YAXAttributeForClass]
        public ushort Version { get; set; } //0x6: 16 = pre 1.13, 20 = 1.13 or later

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Partner")]
        public List<OCS_TableEntry> TableEntries { get; set; }


        public static OCS_File Load(byte[] bytes)
        {
            return new Parser(bytes).GetOcsFile();
        }

        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public int CalculateDataOffset()
        {
            int offset = (int)(TableEntries.Count * 16);

            foreach(var tableEntry in TableEntries)
            {
                foreach(var secondEntry in tableEntry.SubEntries)
                {
                    offset += 16;
                }
            }

            return offset;
        }
    }

    [YAXSerializeAs("Partner")]
    public class OCS_TableEntry : IInstallable
    {
        [YAXDontSerialize]
        public int SortID { get { return int.Parse(Index); } }
        [YAXDontSerialize]
        public string Index { get { return PartnerID.ToString(); } set { PartnerID = int.Parse(value); } }

        [YAXAttributeForClass]
        [YAXSerializeAs("Partner_ID")]
        public int PartnerID { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "SkillList")]
        [BindingSubList]
        public List<OCS_SubTableEntry> SubEntries { get; set; }
        

    }

    [YAXSerializeAs("SkillList")]
    public class OCS_SubTableEntry
    {
        public enum SkillType
        {
            Super = 0,
            Ultimate = 1,
            Evasive = 2,
            Awoken = 3
        }

        [YAXAttributeForClass]
        [YAXSerializeAs("Type")]
        public SkillType Skill_Type { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Skill")]
        [BindingSubList]
        public List<OCS_SubEntry> SubEntries { get; set; }


    }

    [YAXSerializeAs("Skill")]
    public class OCS_SubEntry
    {

        #region WrapperProps
        [YAXDontSerialize]
        public int ID2 { get { return ushort.Parse(I_20); } set { I_20 = value.ToString(); } }



        #endregion

        [YAXAttributeForClass]
        [YAXSerializeAs("I_04")]
        public int I_04 { get; set; } //0x4
        [YAXAttributeForClass]
        [YAXSerializeAs("TP_Cost_Toggle")]
        public int I_08 { get; set; } // uint32
        [YAXAttributeForClass]
        [YAXSerializeAs("TP_Cost")]
        public int I_12 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("ID2")]
        public string I_20 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("DLC_Flag")]
        public int I_24 { get; set; }

    }
}
