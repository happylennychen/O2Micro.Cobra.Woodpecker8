﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using O2Micro.Cobra.Common;

namespace O2Micro.Cobra.Woodpecker8
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    internal class ElementDefine
    {
        #region Chip Constant
        internal const UInt16 EF_MEMORY_SIZE = 0x10;
        internal const UInt16 EF_MEMORY_OFFSET = 0x10;
        internal const UInt16 EF_ATE_OFFSET = 0x10;
        internal const UInt16 EF_ATE_TOP = 0x17;
        internal const UInt16 ATE_CRC_OFFSET = 0x17;

        internal const UInt16 EF_CFG = 0x16;
        internal const UInt16 EF_USR_OFFSET = 0x18;
        internal const UInt16 EF_USR_TOP = 0x1f;

        internal const UInt16 EF_USR_BANK1_OFFSET = 0x18;
        internal const UInt16 EF_USR_BANK1_TOP = 0x1b;
        internal const UInt16 EF_USR_BANK2_OFFSET = 0x1c;
        internal const UInt16 EF_USR_BANK2_TOP = 0x1f;

        internal const UInt16 OP_MEMORY_SIZE = 0xFF;
        internal const Byte PARAM_HEX_ERROR = 0xFF;
        internal const Double PARAM_PHYSICAL_ERROR = -999999;
		
        internal const int RETRY_COUNTER = 15;
		internal const byte WORKMODE_OFFSET = 0x40;
        internal const byte MAPPINGDISABLE_OFFSET = 0x40;
        internal const UInt32 SectionMask = 0xFFFF0000;





        #region 温度参数GUID
        internal const UInt32 TemperatureElement = 0x00010000;
        internal const UInt32 TpETRx = TemperatureElement + 0x00;
        #endregion

        #region Efuse参数GUID
        //internal const UInt32 EFUSEElement = 0x00020000;    //0x10~0x1f
        internal const UInt32 E_BAT_TYPE = 0x00031a07;
        internal const UInt32 E_OVP_TH = 0x00031900;
        internal const UInt32 E_DOT_TH = 0x00031802;
        internal const UInt32 E_OVR_HYS = 0x00031b04;
        internal const UInt32 E_UVR_HYS = 0x00031a04;
        internal const UInt32 E_UVP_TH = 0x00031a00;
        #endregion

        #region Operation参数GUID
        internal const UInt32 OperationElement = 0x00030000;    //0x30~0xff

        internal const UInt32 O_BAT_TYPE = 0x00032a07;
        internal const UInt32 O_OVP_TH = 0x00032900;
        internal const UInt32 O_DOT_TH = 0x00032802;
        internal const UInt32 O_OVR_HYS = 0x00032b04;
        internal const UInt32 O_UVR_HYS = 0x00032a04;
        internal const UInt32 O_UVP_TH = 0x00032a00;

        #endregion

        #region Virtual parameters
        internal const UInt32 VirtualElement = 0x000c0000;

        internal const UInt32 E_DOT_E = 0x000c0001; //Efuse T Enable
        internal const UInt32 O_DOT_E = 0x000c0002; //OP T Enable
        #endregion

        #region EFUSE操作常量定义
        internal const byte EFUSE_DATA_OFFSET = 0x10;
        internal const byte EFUSE_MAP_OFFSET = 0x20;
        internal const byte OPERATION_OFFSET = 0x30;

        // EFUSE operation code

        // EFUSE control registers' addresses
        internal const byte EFUSE_WORKMODE_REG = 0x40;
        //internal const byte EFUSE_TESTCTR_REG = 0x41;
        //internal const byte EFUSE_ATE_FROZEN_REG = 0x04;
        //internal const byte EFUSE_USER_FROZEN_REG = 0x07;

        // EFUSE Control Flags
        internal const byte ALLOW_WR_FLAG = 0x80;
        internal const byte EFUSE_FROZEN_FLAG = 0x80;
        internal const UInt16 EF_TOTAL_PARAMS = 20;
        #endregion


        #endregion

        internal enum SUBTYPE : ushort
        {
            DEFAULT = 0,
            DOT_TH = 1,
            OVP = 2,
            
            EXT_TEMP_TABLE = 40,
            INT_TEMP_REFER = 41
        }

        #region Local ErrorCode
        internal const UInt32 IDS_ERR_DEM_POWERON_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0001;
        internal const UInt32 IDS_ERR_DEM_POWEROFF_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0002;
        internal const UInt32 IDS_ERR_DEM_POWERCHECK_FAILED = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0003;
        internal const UInt32 IDS_ERR_DEM_FROZEN = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0004;
        internal const UInt32 IDS_ERR_DEM_FROZEN_OP = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0005;
        internal const UInt32 IDS_ERR_DEM_BLOCK = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0006;
        internal const UInt32 IDS_ERR_DEM_ONE_PARAM_DISABLE = LibErrorCode.IDS_ERR_SECTION_DYNAMIC_DEM + 0x0007;
        #endregion

        internal enum WORK_MODE : ushort
        {
            NORMAL = 0,
            WRITE_MAP_CTRL = 0x01,
            PROGRAM = 0x02,
        }

        internal enum COMMAND : ushort
        {
            FROZEN_BIT_CHECK_PC = 9,
            FROZEN_BIT_CHECK = 10,
            DIRTY_CHIP_CHECK_PC = 11,
            DIRTY_CHIP_CHECK = 12,
            DOWNLOAD_PC = 13,
            DOWNLOAD = 14,
            READ_BACK_CHECK_PC = 15,
            READ_BACK_CHECK = 16,
            GET_EFUSE_HEX_DATA = 17
        }

    }
}
